using EasyInstrumentor.Models;
using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInstrumentor.Services.Capture
{
    public class CaptureHelperService
    {


        HashSet<string> ignoredWindowsServiceExecutables;
        HashSet<string> notallowedClass;
        HashSet<string> systemModules;
        HashSet<string> excludedMethods;
        private FileSystemWatcher _watcher;
        private readonly IConfiguration _configuration;

        public CaptureHelperService(IConfiguration config)
        {

            _configuration = config;

            LoadConfig();
            // Setup FileSystemWatcher to watch appsettings.json
            _watcher = new FileSystemWatcher(AppContext.BaseDirectory, "appsettings.json");
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Changed += OnConfigChanged;
            _watcher.EnableRaisingEvents = true;
        }


        private void LoadConfig()
        {
            ignoredWindowsServiceExecutables = LoadSet(_configuration, "ExcludedProcess");
            notallowedClass = LoadSet(_configuration, "ExcludedClasses");
            systemModules = LoadSet(_configuration, "SystemModules");
            excludedMethods = LoadSet(_configuration, "ExcludedMethods");
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            // Delay a bit to allow file write to complete
            Task.Delay(500).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadConfig();
                });
            });
        }


        private HashSet<string> LoadSet(IConfiguration config, string key)
        {
            var items = config.GetSection(key).Get<List<string>>() ?? new();
            return new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
        }


        static int CurrentProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;
        internal static HashSet<BtData> lstBtData = new HashSet<BtData>();
        internal static HashSet<DotnetProcess> lstProcess = new HashSet<DotnetProcess>();
        internal static HashSet<CallStackData> lstStack = new HashSet<CallStackData>();



        //// Define methods to exclude
        //internal static readonly HashSet<string> ExcludedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        //{
        //    "initializecomponent()", ".ctor()", "dispose(bool)"
        //};
        internal bool IsValidProcess(ProcessTraceData data, string commandLine, int oldPid)
        {
            if (data.ProcessID == CurrentProcessID || data.ProcessID == oldPid)
            {
                return false;
            }

            if (string.Equals(commandLine, data.CommandLine, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;

        }

        /// <summary>
        /// Checks that method is valid for considering for BT or not.
        /// </summary>
        /// <param name="jitData"></param>
        internal void IsValidMethod(JitData jitData)
        {
            if (!jitData.MethodName.Contains(".ctor") &&
                !notallowedClass.Where(x => jitData.ClassName.Contains(x)).Any() &&
                !excludedMethods.Contains(jitData.MethodName))
            {

                BtData btData = new BtData() {
                    ClassName = jitData.ClassName,
                    MethodName = jitData.MethodName,
                    ProcessID = jitData.ProcessID,
                    ProcessName = jitData.ProcessName,
                    CallStack = jitData.CallStack,
                    ThreadID = jitData.ThreadID
                };

                if (lstBtData.TryGetValue(btData, out var existing))
                {
                    existing.ExecutionCount++;  // update counter
                }
                else
                {
                    lstBtData.Add(btData);          // new entry
                }
            }
        }

        /// <summary>
        /// Verifies whether method passed to this can be considered or not for building the call stack.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        internal bool IsValidMethod(string moduleName)
        {
            bool isValid = false;
            if (!string.IsNullOrEmpty(moduleName) && !excludedMethods.Any(value => moduleName.Contains(value, StringComparison.OrdinalIgnoreCase)))
            {
                isValid = true;
            }
            // Check if the module name is in the system modules list
            return isValid;

            //return false;
        }

        internal bool IsMonitored(int processId)
        {
            if (processId == CurrentProcessID)
            {
                return false;
            }


            if (lstProcess.Where(x => x.ProcessId == processId).Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool IsSystemModule(string moduleName)
        {
            // Check if the module name is in the system modules list
            return systemModules.Contains(moduleName);

            //return false;
        }

        public bool IgnoreService(string imageName)
        {

            //if(imageName.ToLower().Contains("smsvchost"))
            //{
            //    Console.WriteLine("");
            //}
            // Normalize the image name to lower case for comparison
           // string normalizedImageName = imageName.ToLowerInvariant();

            // Check if the image name is in the list of known Windows service executables
            if (ignoredWindowsServiceExecutables.Contains(imageName))
                return true;

            return false;
        }
    }
}
