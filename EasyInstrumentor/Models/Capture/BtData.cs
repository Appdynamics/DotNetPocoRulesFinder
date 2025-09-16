using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EasyInstrumentor.Models
{
    internal class BtData
    {
        public string ProcessName { get; set; }
        public int ProcessID { get; set; }
        public int ThreadID { get; set; }
        public int ExecutionCount { get; set; } = 1;
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string CallStack { get; set; }=string.Empty;


        public override bool Equals(object obj)
        {
            return obj is BtData other &&
                   ProcessID == other.ProcessID &&
                   MethodName == other.MethodName &&
                   ClassName == other.ClassName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProcessID, MethodName, ClassName);
        }
    }
}
