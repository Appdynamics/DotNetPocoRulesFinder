using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyInstrumentor.Models.Config
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute("appdynamics-agent", Namespace = "", IsNullable = false)]
    public partial class ConfigModel
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("controller")]
        public Controller controller { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("machine-agent")]
        public Machineagent machineagent { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("app-agents")]
        public Appagents appagents { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Controller
    {
        /// <remarks/>
        public ControllerApplication application { get; set; }

        public List<ControllerApplication> applications { get; set; }

        /// <remarks/>
        public ControllerAccount account { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string host { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int port { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool ssl { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool enable_tls12 { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ControllerApplication
    {
        private string nameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        [System.Xml.Serialization.XmlElement("default")]
        public bool isDefault { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ControllerAccount
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string password { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Machineagent
    {

        private List<MachineagentPerfcounter> perfcountersField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute("perf-counters")]
        [System.Xml.Serialization.XmlArrayItemAttribute("perf-counter", IsNullable = false)]
        public MachineagentPerfcounter[] perfcounters { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MachineagentPerfcounter
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cat { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string instance { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Appagents
    {

        /// <remarks/>
        public IISTag IIS { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute("standalone-applications")]
        [System.Xml.Serialization.XmlArrayItemAttribute("standalone-application", IsNullable = false)]
        public List<Standaloneapplication> standaloneapplications { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IISTag
    {

        /// <remarks/>
        public IsAutomatic automatic { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("application", IsNullable = false)]
        public List<IISApplication> applications { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IsAutomatic
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool enabled { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class IISApplication
    {

        /// <remarks/>
        public Tier tier { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string path { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string site { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Tier
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name { get; set; }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Standaloneapplication
    {
        /// <remarks/>
        public Tier tier { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string executable { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("command-line")]
        public string commandLine { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("controller-application")]
        public string controllerApplication { get; set; }

        
    }


}
