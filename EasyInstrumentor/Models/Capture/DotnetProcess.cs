using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInstrumentor.Models
{
    public class DotnetProcess
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string ImageFileName { get; set; }
        public bool IsDotnet { get; set; }
        public string CommandLine { get; set; }
        public bool IsInstrumented { get; set; }
        public string Executable { get; set; }
    }
}
