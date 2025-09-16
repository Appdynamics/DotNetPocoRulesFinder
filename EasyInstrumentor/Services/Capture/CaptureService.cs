using EasyInstrumentor.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Newtonsoft.Json;
using EasyInstrumentor.Models.Capture;
using Microsoft.Diagnostics.Symbols;
using System.Collections.Concurrent;
using EasyInstrumentor.Services.Config;
using EasyInstrumentor.Models.Config;
using System.Management;
using System.Diagnostics.SymbolStore;
using Microsoft.Extensions.Logging;


namespace EasyInstrumentor.Services.Capture
{
    public class CaptureService
    {
        TraceEventSession userSession;
        TraceEventSession activeUserSession;
        TraceLogEventSource traceLogSource;
        // Shared data structure for grouping process IDs and call stacks
        static ConcurrentDictionary<int, HashSet<string>> callStacks = new ConcurrentDictionary<int, HashSet<string>>();
        ConfigService configService = new ConfigService();
        static ConfigModel config;
        private static SymbolReader m_symbolReader;


        private readonly ILogger<CaptureService> _logger;
        private readonly CaptureHelperService _captureHelperService;

        public CaptureService(ILogger<CaptureService> logger, CaptureHelperService captureHelperService)
        {
            _logger = logger; 
            _captureHelperService = captureHelperService;
        }

        public async void StartCapture(string commandLine, int oldPid)
        {
            await Task.Run(() => CaptureTraceData(commandLine, oldPid));
        }

        public void CaptureTraceData(string commandLine, int oldPid)
        {
            string symbolPath = SymbolPath.SymbolPathFromEnvironment;

            // If _NT_SYMBOL_PATH isn't set, force it to default to the one mentioned in the README of the project.
            //if (string.IsNullOrWhiteSpace(symbolPath))
            //{
            //    symbolPath = @";SRV*C:\Symbols*https://msdl.microsoft.com/download/symbols;SRV*C:\Symbols*https://nuget.smbsrc.net;SRV*C:\Symbols*https://referencesource.microsoft.com/symbols";
            //}

            //m_symbolReader = new SymbolReader(TextWriter.Null, symbolPath);
            //m_symbolReader.SecurityCheck = path => true;

            List<string> activityid = new List<string>();
            userSession = new TraceEventSession("SimpleMontitorSession", "MyEventsFile.etl");
            activeUserSession = new TraceEventSession("ActiveUserSession");


            activeUserSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process,
                stackCapture: KernelTraceEventParser.Keywords.Thread
                );



            userSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process
                | KernelTraceEventParser.Keywords.ImageLoad
                | KernelTraceEventParser.Keywords.Thread,
                stackCapture: KernelTraceEventParser.Keywords.Thread
                );
            userSession.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose,
                (ulong)(ClrTraceEventParser.Keywords.Loader
                | ClrTraceEventParser.Keywords.Stack
                | ClrTraceEventParser.Keywords.Threading
                | ClrTraceEventParser.Keywords.Jit
                ),
                options: new TraceEventProviderOptions { StacksEnabled = true }
                );
            // Needed for JIT Compile code that was already compiled. 
            userSession.EnableProvider(ClrRundownTraceEventParser.ProviderGuid, TraceEventLevel.Verbose,
                (ulong)(ClrTraceEventParser.Keywords.Jit |
                ClrTraceEventParser.Keywords.Loader |
                ClrTraceEventParser.Keywords.Stack |
                ClrTraceEventParser.Keywords.Threading),
                options: new TraceEventProviderOptions { StacksEnabled = true }
                );


            traceLogSource = TraceLog.CreateFromTraceEventSession(activeUserSession);

            traceLogSource.Kernel.ProcessStart += delegate (ProcessTraceData data)
                {

                    if (_captureHelperService.IsValidProcess(data, commandLine, oldPid))
                    {
                        CaptureHelperService.lstProcess.Add(new DotnetProcess()
                        {
                            ProcessId = data.ProcessID,
                            ProcessName = data.ProcessName,
                            ImageFileName = data.ImageFileName,
                            IsDotnet = true,
                        });
                    }

                };

            traceLogSource.Kernel.ProcessDCStart += delegate (ProcessTraceData data)
            {
                if (_captureHelperService.IsValidProcess(data, commandLine, oldPid))
                {
                    CaptureHelperService.lstProcess.Add(new DotnetProcess()
                    {
                        ProcessId = data.ProcessID,
                        ProcessName = data.ProcessName,
                        ImageFileName = data.ImageFileName,
                        IsDotnet = true,
                    });
                }
            };

            traceLogSource.Process();

        }
        internal async Task<bool> StopCapture(bool terminate)
        {

            if (userSession != null) { userSession.Stop(); }
            //GetEligibleProcess();

            if(terminate)
            {
                CaptureHelperService.lstProcess.Clear();
                return true;
            }
            else
                return await Task.Run(() => ProcessData());

        }

        private void CaptureStack(TraceEvent data)
        {
            // There are a lot of data collection start on entry that I don't want to see (but often they are quite handy
            if (data.Opcode == TraceEventOpcode.DataCollectionStart)
            {
                return;
            }
            // V3.5 runtimes don't log the stack and in fact don't event log the exception name (it shows up as an empty string)
            // Just ignore these as they are not that interesting. 
            if (data is ExceptionTraceData && ((ExceptionTraceData)data).ExceptionType.Length == 0)
            {
                return;
            }

            try
            {
                var callStack = data.CallStack();
                string stack = GetCallStack(data);

                if (!string.IsNullOrEmpty(stack.Trim()))
                {
                    AddCallStack(data.ProcessID, stack);
                }
            }
            catch (Exception e)
            {

            }

        }

        internal bool ProcessData()
        {
            using (var source = Microsoft.Diagnostics.Tracing.Etlx.TraceLog.OpenOrConvert("MyEventsFile.etl").Events.GetSource())
            {
                // Get the stream of starts.
                IObservable<MethodJittingStartedTraceData> jitStartStream = source.Clr.Observe<MethodJittingStartedTraceData>("Method/JittingStarted");

                // And the stream of ends.
                IObservable<MethodLoadUnloadVerboseTraceData> jitEndStream = source.Clr.Observe<MethodLoadUnloadVerboseTraceData>("Method/LoadVerbose");

                int nProcessID = Process.GetCurrentProcess().Id;
                var jitTimes =
                    from start in jitStartStream //.Where(e => e.MethodID == e.MethodID && e.ProcessID == e.ProcessID && e.ProcessID != nProcessID && CaptureHelperService.lstProcess.Where(x => x.ProcessId == e.ProcessID).Count() > 0).Take(1)
                    from end in jitEndStream.Where(e => start.MethodID == e.MethodID && start.ProcessID == e.ProcessID
                    && start.ProcessID != nProcessID //&& start.ProcessID == 44840
                    && CaptureHelperService.lstProcess.Where(x => x.ProcessId == start.ProcessID).Count() > 0).Take(1)
                    select new JitData
                    {
                        ProcessID = start.ProcessID,
                        ThreadID = start.ThreadID,
                        //StartData = PrintData(start),
                        //EndData = PrintData(end),
                        ClassName = start.MethodNamespace,
                        MethodName = start.MethodName,
                        ProcessName = CaptureHelperService.lstProcess.Where(x => x.ProcessId == start.ProcessID).First().ProcessName
                    };

                //Print every time you compile a method
                jitTimes.Subscribe(onNext: jitData => _captureHelperService.IsValidMethod(jitData));

                source.Kernel.StackWalkStack += data =>
                {
                    if (_captureHelperService.IsMonitored(data.ProcessID))
                    {
                        
                        string stack = GetCallStack(data);

                        //Console.WriteLine(stack);
                    }
                };

                source.Kernel.PerfInfoSample += data =>
                {
                    if (_captureHelperService.IsMonitored(data.ProcessID))
                    {

                        
                        //ResolveSymbols(callStack);
                        string stack = GetCallStack(data);

                        if (!string.IsNullOrEmpty(stack.Trim()))
                        {
                            AddCallStack(data.ProcessID, stack);
                        }
                    }

                };

                source.Kernel.ThreadStart += data =>
                {
                    if (_captureHelperService.IsMonitored(data.ProcessID))
                    {
                        var callStack = data.CallStack();
                        string stack = GetCallStack(data);
                        if (!string.IsNullOrEmpty(stack.Trim()))
                        {
                            AddCallStack(data.ProcessID, stack);
                        }
                    }
                };
                source.Process();
            }

            MapMethodAndCallStack();

            return CaptureHelperService.lstProcess.Count > 0;
        }

        internal static void ResolveSymbols(TraceCallStack callStack)
        {
            while (callStack != null)
            {
                var codeAddress = callStack.CodeAddress;
                if (codeAddress.Method == null)
                {
                    var moduleFile = codeAddress.ModuleFile;
                    if (moduleFile != null)
                    {
                        codeAddress.CodeAddresses.LookupSymbolsForModule(m_symbolReader, moduleFile);
                    }
                }

                callStack = callStack.Caller;
            }
        }


        public List<BTDetails> FillBtData(int processId)
        {
            List<BTDetails> btDetails = new List<BTDetails>();
            List<string> callstack = new List<string>();

            foreach (var x in CaptureHelperService.lstBtData.Where(x => x.ProcessID == processId))
            {
                var item = new BTDetails()
                {
                    ClassName = x.ClassName,
                    MethodName = x.MethodName,
                    ProcessName = x.ProcessName,
                    ProcessId = x.ProcessID.ToString(),
                    CallStack = x.CallStack,
                    ExecutionCount = x.ExecutionCount
                };
                btDetails.Add(item);
            }


            return btDetails;
        }



        private string GetCallStack(TraceEvent data)
        {
            TraceCallStack callStack = data.CallStack();
            StringBuilder sb = new StringBuilder();
            int indentLevel = 1;
            string indent = new string(' ', indentLevel);
            var stackFrames = new List<string>();

            while (callStack != null)
            {
                // Check if the frame is from a system module
                if (!_captureHelperService.IsSystemModule(callStack.CodeAddress.ModuleName) && _captureHelperService.IsValidMethod(callStack.CodeAddress.FullMethodName))
                {
                    stackFrames.Add($"{indent}{callStack.CodeAddress.ModuleName.Trim()}!{callStack.CodeAddress.FullMethodName.Trim()}\n");
                }
                callStack = callStack.Caller;
            }

            // Reverse the collected stack frames
            stackFrames.Reverse();

            foreach (var frame in stackFrames)
            {
                sb.AppendLine($"{indent}{frame}");
                indentLevel++;
                indent = new string(' ', indentLevel);
            }

            return sb.ToString();

        }

        static void AddCallStack(int processId, string callStack)
        {
            // Add call stack to the dictionary in a thread-safe manner
            callStacks.AddOrUpdate(
                processId,
                new HashSet<string> { callStack },
                (key, existingSet) =>
                {
                    lock (existingSet) // Ensure thread safety when updating HashSet
                    {
                        existingSet.Add(callStack);
                    }
                    return existingSet;
                });
        }

        static void MapMethodAndCallStack()
        {

            // Logic to process and update btData
            foreach (var processEntry in callStacks)
            {
                int processId = processEntry.Key;
                var callStackstemp = processEntry.Value;

                // Filter btData for current processId
                

                foreach (var callStack in callStackstemp)
                {

                    var processMethods = CaptureHelperService.lstBtData.Where(b => b.ProcessID == processId ).ToList();
                    // Find all matching methods for this call stack
                    var matchingMethods = processMethods
                        .Where(b => callStack.Contains(b.ClassName+"."+ b.MethodName))
                        .ToList();

                    if (matchingMethods.Any())
                    {
                        // Assign the call stack to the first matching method
                        var firstMethod = matchingMethods.First();

                        if(string.IsNullOrEmpty(firstMethod.CallStack))
                        {
                            firstMethod.CallStack = callStack;
                        }
                        

                        // Remove all other matching methods from btData
                        foreach (var method in matchingMethods.Skip(1))
                        {
                            CaptureHelperService.lstBtData.Remove(method);
                        }
                    }
                }
            }


            #region Methods without callstack

            //if there are any method without callstack check other method with same thread id and processid
            //Then update first thread id and processid method with the subsequent methods
            if (CaptureHelperService.lstBtData.Where(x => string.IsNullOrEmpty(x.CallStack)
            //&& x.ProcessID == 44840
            ).Count() > 0)
            {
                // Dictionary to keep track of the first occurrence for each (Pid, Tid)
                var firstOccurrenceMap = new Dictionary<(int Pid, int Tid), BtData>();

                // Temporary set to store items to be removed
                var itemsToRemove = new HashSet<BtData>();

                for (int i = 0; i < CaptureHelperService.lstBtData.Where(x => string.IsNullOrEmpty(x.CallStack)
                //&& x.ProcessID == 44840
                ).Count(); i++)
                {
                    var current = CaptureHelperService.lstBtData.Where(x => string.IsNullOrEmpty(x.CallStack)
                    //&& x.ProcessID == 44840
                    ).ElementAt(i);
                    var key = (current.ProcessID, current.ThreadID);

                    if (firstOccurrenceMap.ContainsKey(key))
                    {
                        /*
                        // Append the current method to the callstack of the first occurrence
                        var firstOccurrence = firstOccurrenceMap[key];
                        var indentLevel = firstOccurrence.CallStack == null ? 1 : firstOccurrence.CallStack.Split('\n').Length + 1;
                        string indent = new string(' ', indentLevel);

                        if (string.IsNullOrEmpty(firstOccurrence.CallStack))
                        {
                            // Include the method of the first occurrence in its own callstack
                            firstOccurrence.CallStack += ($"{firstOccurrence.ClassName.Trim()}!{firstOccurrence.MethodName.Trim()}");
                            itemsToRemove.Remove(firstOccurrence);
                        }
                        firstOccurrence.CallStack += ($"{indent}{firstOccurrence.ClassName.Trim()}!{firstOccurrence.MethodName.Trim()}");
                       
                        */

                        // Mark the current object for removal
                        itemsToRemove.Add(current);
                    }
                    else
                    {
                        // Mark the current object as the first occurrence
                        firstOccurrenceMap[key] = current;
                    }
                }

                // Remove marked items
                foreach (var item in itemsToRemove)
                {
                    CaptureHelperService.lstBtData.Remove(item);
                }


            }

            #endregion


        }


        public bool IsInstrumented(string commandLine)
        {
            bool isInstrumented = false;


            if (String.IsNullOrEmpty(commandLine))
            {
                if (config is null)
                {
                    config = configService.GetConfigDetails(false).Result;
                }

                if (config?.appagents?.standaloneapplications?.Count > 0)
                {

                    string processArgs;
                    // Search for matching exePath or exeName in the list
                    var matchingEntry = GetMatchingEntryFromConfig(commandLine, out processArgs);

                    if (matchingEntry != null)
                    {
                        string configCommandLine = matchingEntry.commandLine ?? string.Empty;
                        isInstrumented = string.Equals(configCommandLine, processArgs, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return isInstrumented;
        }

        static Standaloneapplication? GetMatchingEntryFromConfig(string commandLine, out string processArgs)
        {
            string processExePath = GetExecutablePathFromCommand(commandLine);
            string processExeName = Path.GetFileName(processExePath);
            Standaloneapplication? selectedApplication = config.appagents.standaloneapplications.FirstOrDefault(entry =>
                        string.Equals(Path.GetFullPath(entry.executable), processExePath, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetFileName(entry.executable), processExeName, StringComparison.OrdinalIgnoreCase));

            if (selectedApplication != null)
            {
                processArgs = GetArgumentsFromCommand(commandLine, processExePath)?.Trim() ?? string.Empty;
            }
            else
            {
                processArgs = string.Empty;
            }

            return selectedApplication;
        }

        static string GetExecutablePathFromCommand(string commandLine)
        {
            if (commandLine.StartsWith("\"")) // Handle quoted paths
            {
                int endQuoteIndex = commandLine.IndexOf("\"", 1);
                return endQuoteIndex > 0 ? commandLine.Substring(1, endQuoteIndex - 1) : commandLine;
            }
            else // Unquoted paths
            {
                int firstSpaceIndex = commandLine.IndexOf(' ');
                return firstSpaceIndex > 0 ? commandLine.Substring(0, firstSpaceIndex) : commandLine;
            }
        }

        static string GetArgumentsFromCommand(string commandLine, string exePath)
        {
            if (commandLine.StartsWith("\""))
            {
                int endQuoteIndex = commandLine.IndexOf("\"", 1);
                return endQuoteIndex > 0 && commandLine.Length > endQuoteIndex + 1
                    ? commandLine.Substring(endQuoteIndex + 2)
                    : string.Empty;
            }
            else
            {
                int firstSpaceIndex = commandLine.IndexOf(' ');
                return firstSpaceIndex > 0 && commandLine.Length > firstSpaceIndex + 1
                    ? commandLine.Substring(firstSpaceIndex + 1)
                    : string.Empty;
            }
        }

        public void RefreshData(int processId)
        {
            config = configService.GetConfigDetails(true).Result;
            CaptureHelperService.lstProcess.Where(x => x.ProcessId == processId).FirstOrDefault().IsInstrumented = true;

        }


        static string GetCommandLine(int processId)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["CommandLine"]?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving command line: {ex.Message}");
            }

            return string.Empty;
        }

        public async Task<DotnetProcess> GetNewProcessData()
        {

            return CaptureHelperService.lstProcess.FirstOrDefault();
        }
    }
}
