using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInstrumentor.Models.Config
{
    internal class ConfigFormModel
    {
        public string ControllerHost { get; set; }
        public int ControllerPort { get; set; }
        public string DefaultApplicationName { get; set; }
        public string StandaloneApplicationName { get; set; }

        [Required(ErrorMessage = "Please enter standalone application executable")]
        public string StandaloneExecutable { get; set; }
        public string StandaloneCommandline { get; set; }
        [Required(ErrorMessage ="Please enter standalone application tier name")]
        public string StandaloneTierName { get; set; }
        public bool HasMultiControllerApplication { get; set; }
    }
}
