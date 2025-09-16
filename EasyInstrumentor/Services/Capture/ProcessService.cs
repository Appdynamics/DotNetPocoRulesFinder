using EasyInstrumentor.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;


namespace EasyInstrumentor.Services.Capture
{
    public class ProcessService
    {


        private readonly ILogger<ProcessService> _logger;
        private readonly CaptureHelperService _captureHelperService;
        public ProcessService(ILogger<ProcessService> logger, CaptureHelperService captureHelperService)
        {
            _logger = logger;
            _captureHelperService = captureHelperService;
        }
        static int CurrentProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;
        
        static List<DotnetProcess> list = new List<DotnetProcess>();

        public async Task<List<DotnetProcess>> GetDotNetProcessesAsync(bool refresh)
        {
            if (refresh)
            {
                list.Clear();
                return await Task.Run(() =>
                {

                    bool isEligible = false;

                    HashSet<int> dotnetCorePids = new(DiagnosticsClient.GetPublishedProcesses());

                    var processes = Process.GetProcesses().Where(p =>
                    {
                        try
                        {


                            return !_captureHelperService.IgnoreService(p.ProcessName);
                        }
                        catch
                        {
                            return false;
                        }
                    }); ;


                    foreach (var proc in processes)
                    {
                        DateTime start = DateTime.Now;
                        
                        isEligible = false;
                        try
                        {
                            string processName = proc.ProcessName;
                            int pid = proc.Id;

                            if (pid == CurrentProcessID //|| !processName.ToUpper().Contains("STANDALONE")
                                )
                            {
                                continue;
                            }

                            // Check if it's a .NET Core process
                            if (dotnetCorePids.Contains(pid) // && !_captureHelperService.IgnoreService(proc.MainModule.ModuleName)
                            )
                            {
                                isEligible = true;
                            }
                            else  //if (!_captureHelperService.IgnoreService(proc.MainModule.ModuleName))
                            {

                                isEligible = proc.Modules.Cast<ProcessModule>()
                                           .Any(m => m.ModuleName.Equals("clr.dll", StringComparison.OrdinalIgnoreCase) ||
                                                     m.ModuleName.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase) ||
                                                     m.ModuleName.StartsWith("mscor", StringComparison.OrdinalIgnoreCase));

                            }

                            if (isEligible)
                            {
                                

                                Process process = Process.GetProcessById(pid);
                                list.Add(new DotnetProcess
                                {
                                    ProcessName = processName,
                                    ProcessId = pid,
                                    CommandLine = GetCommandLine(pid),
                                    IsDotnet = true
                                });
                                DateTime end = DateTime.Now;

                                _logger.LogInformation(proc.ProcessName + " ## " + (end - start).Milliseconds);
                            }
                        }
                        catch
                        {
                            // Skip processes we can't access
                        }

                        
                    }
                    return list;
                });
            }
            else
            {
                return list;
            }
        }

        public Task<DotnetProcess?> GetProcessByIdAsync(int id)
        {
            try
            {
                var p = Process.GetProcessById(id);
                return Task.FromResult<DotnetProcess?>(new DotnetProcess
                {
                    ProcessId = p.Id,
                    ProcessName = p.ProcessName,
                    CommandLine = GetCommandLine(p.Id)
                });
            }
            catch
            {
                return Task.FromResult<DotnetProcess?>(null);
            }
        }



        private string GetCommandLine(int pid)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {pid}");
                using var results = searcher.Get();
                var commandLine = results.Cast<ManagementObject>().FirstOrDefault()?["CommandLine"]?.ToString();
                return commandLine ?? "";
            }
            catch
            {
                return "";
            }
        }



    }
}
