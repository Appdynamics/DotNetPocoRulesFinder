# .NET POCO Rules Finder

A Windows application that helps in **detecting classes, methods, and call stacks** of running .NET applications.  
This tool is especially useful when configuring **POCO (Plain Old CLR Object) transactions** in AppDynamics for Windows services or standalone applications that cannot be auto-instrumented.

---

## 🚀 Why This Tool?

AppDynamics does not automatically instrument **Windows Services** or **standalone .NET applications**.  
To monitor business transactions in these cases, we must **manually configure POCOs**.  

However:
- Identifying the correct class and method is often difficult  
- Third-party applications may not provide source code access  

This tool solves that by **capturing runtime method and class execution using ETW (Event Tracing for Windows)** via the **PerfView TraceEvent library**.

---

## ⚙️ How It Works

1. Download Dotnet POCO Detector zip file from the github.
2. Then extract it to folder.
3. Run Dotnet **POCO Detector.exe** as administrator.
4. On launch, the tool lists all running .NET processes.
5. The user selects the target process.
6. Click **Proceed** → The tool waits for the process to be restarted.
7. Once restarted, the tool begins capturing runtime events.
8. The user performs the activity that triggers the business transaction.
9. Click **Stop** → The tool processes captured data.
10. The tool displays:
   - Classes and methods invoked
   - Call stacks (if available)
   - Rows with call stack = highlighted in green

You can use this output to configure **POCOs in AppDynamics**.

---

## 🧩 Key Features

- ✅ List running .NET processes  
- ✅ Capture classes and methods in real-time  
- ✅ Capture call stacks (if available)  
- ✅ Green highlight for methods with call stack captured  
- ✅ Configurable process and method exclusions via `appsettings.json`  

---

## 🔧 Configuration

The tool supports filtering via **`appsettings.json`**:

```json
{
  "ExcludedProcess": "Process1,Process2",
  "ExcludedMethods": "ToString,Equals,GetHashCode"
}
