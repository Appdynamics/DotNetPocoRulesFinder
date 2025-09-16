using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInstrumentor.Models.Capture
{
    public class BTDetails
    {
        public string ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string CallStack { get; set; }
        public int ExecutionCount { get; set; } 
    }
}
