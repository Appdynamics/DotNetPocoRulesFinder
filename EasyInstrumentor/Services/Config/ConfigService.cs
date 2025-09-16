using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using EasyInstrumentor.Models.Config;

namespace EasyInstrumentor.Services.Config
{
    internal class ConfigService
    {
        internal const string DotNetAgentRegistryKey = @"SOFTWARE\AppDynamics\dotNet Agent";
        internal const string InstallationDirRegistryName = "InstallationDir";
        internal const string DotNetAgentFolderRegistryName = "DotNetAgentFolder";

        internal const string DotNetAgentFolderLocation = @"AppDynamics\DotNetAgent";
        internal const string ConfigFileFolder = "config";
        internal const string ConfigFileName = "config.xml";

        public static bool isConfigFileLocated { get; private set; }
        public static string ConfigFilelocation { get; private set; }
        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        public static ConfigModel config = null;
        public static bool isConfigFetched = false;
        public static string AgentConfigFullPath
        {
            get
            {
                //TODO: Need to update to get correct file if config location is changed.

                string defaultPath =
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
                    System.IO.Path.DirectorySeparatorChar +
                    ConfigService.DotNetAgentFolderLocation +
                    System.IO.Path.DirectorySeparatorChar;

                string path = RegistryService.
                    GetorDefault(DotNetAgentRegistryKey, DotNetAgentFolderRegistryName, defaultPath);

                return path +
                    ConfigService.ConfigFileFolder +
                    System.IO.Path.DirectorySeparatorChar +
                    ConfigService.ConfigFileName;
            }
        }


        private static async Task<ConfigModel> ReadAppDynamicsConfigFileAsync()
        {

            GetConfigFileLocation();
            if (isConfigFileLocated)
            {
                var serializer = new XmlSerializer(typeof(ConfigModel));

                using var reader = new StreamReader(ConfigService.ConfigFilelocation);
                config = (ConfigModel)serializer.Deserialize(reader);
            }
            else
            {
                _logger.Info("Could not locate Config file location at-" + ConfigFilelocation);
            }

            return config;
        }

        public async Task<ConfigModel> GetConfigDetails(bool hardRefresh)
        {
            if (config == null || hardRefresh)
            {
                ConfigService.GetConfigFileLocation();
                await ConfigService.ReadAppDynamicsConfigFileAsync();
                isConfigFetched = true;
            }
            return config;
        }

        private static void GetConfigFileLocation()
        {
            isConfigFileLocated = true;

            ConfigFilelocation = "";

            ConfigFilelocation = ConfigService.AgentConfigFullPath;

            isConfigFileLocated = File.Exists(ConfigFilelocation);

            _logger.Info(String.Format("Located-{0} Config file at - {1}", isConfigFileLocated, ConfigFilelocation));

        }


        private static XElement GetChildElementbyName(XElement node, string childNodeName)
        {
            XElement machineAgent = null;
            if (node != null && node.HasElements)
            {
                // TODO: can use sarch using xpath
                foreach (XElement xe in node.Elements())
                {
                    string name = xe.Name.LocalName;
                    if (name.Equals(childNodeName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        machineAgent = xe;
                        break;
                    }
                }
            }
            return machineAgent;
        }

        // Method to serialize class to XML and save to a file
        static void SerializeClassToXmlFile<T>(T obj, string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", ""); // Removes default XML namespaces

            using StreamWriter writer = new StreamWriter(filePath);
            serializer.Serialize(writer, obj, ns);
        }


        public static XElement ToXElement<T>(T obj, string elementName = null)
        {

            if (obj is Standaloneapplication standaloneApp)
            {
                // Handle Standaloneapplication case
                return new XElement("standalone-application",
                    new XAttribute("executable", standaloneApp.executable ?? ""),
                    new XAttribute("command-line", standaloneApp.commandLine ?? ""),
                    !string.IsNullOrEmpty(standaloneApp.controllerApplication)
        ? new XAttribute("controller-application", standaloneApp.controllerApplication)
        : null,
                    standaloneApp.tier != null ? new XElement("tier", new XAttribute("name", standaloneApp.tier.name)) : null
        );
            }
            else
            {
                return null;
            }


        }

        public static bool UpdateStandAloneApplicationToConfig(XElement element, out string message)
        {
            bool isSuccess = false;
            message = "Standalone application configured successfully..!!!";
            try
            {
                string standaloneNode = "standalone-applications";
                string appNode = "appdynamics-agent";
                // Load the existing XML file
                XDocument doc = XDocument.Load(ConfigFilelocation);

                // Find the parent node
                XElement? appElement = doc.Root;

                if (appElement == null)
                {
                    _logger.Info($"Parent node '{appElement}' not found.");
                    message = $"Parent node '{appElement}' not found.";
                    return isSuccess;
                }

                // Find or create the target node inside the parent node

                XElement targetElement = appElement.Descendants(standaloneNode).FirstOrDefault();

                if (targetElement == null)
                {
                    // If the node doesn't exist, create it and add it under the parent node
                    targetElement = new XElement(standaloneNode);
                    appElement.Add(targetElement);
                }

                // Add the new element inside the target node
                targetElement.Add(new XElement(element));

                // Save the updated XML
                doc.Save(ConfigFilelocation);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                message = "Error while processing. Error : " + ex.Message;
            }

            return isSuccess;

        }


    }
}
