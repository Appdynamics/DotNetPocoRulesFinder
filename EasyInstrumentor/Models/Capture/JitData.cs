using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInstrumentor
{
    internal class JitData
    {
        public int ProcessID { get; set; }
        public int ThreadID { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string ProcessName { get; set; }
        public string CallStack { get; set; }

    }


}
