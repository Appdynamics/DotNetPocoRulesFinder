using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInstrumentor.Models
{
    internal class CallStackData
    {
        public TraceCallStack CallStackDetails { get; set; }
        public int ProcessId { get; set; }
        public string MethodName { get; set; }
        public string ClassName { get; set; }
    }
}
