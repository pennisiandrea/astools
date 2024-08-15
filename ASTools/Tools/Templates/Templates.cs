using System.Xml;
using System.Xml.Serialization;

namespace ASTools.Tools.Templates
{
    [XmlRoot("Template")]
    public class TemplateConfigClass
    {
        [XmlArray("Keywords")]
        [XmlArrayItem("Keyword")]
        public List<KeywordClass>? Keywords {get; set;}

        [XmlArray("Instructions")]
        [XmlArrayItem("Instruction")]
        public List<InstructionClass>? Instructions {get; set;}

        // Submembers
        public class KeywordClass
        {
            [XmlAttribute("ID")]
            public required string ID { get; set; }
            
            [XmlText]
            public string? Value { get; set; }
        }
        public class InstructionClass
        {
            [XmlAttribute("Type")]
            public required string Type { get; set; }

            [XmlElement("Source")]
            public SourceClass? Source { get; set; }

            [XmlElement("Destination")]
            public DestinationClass? Destination { get; set; }

            [XmlElement("XmlElements2Add")]
            public XmlElements2AddClass? XmlElements2Add { get; set; }
        }
        public class SourceClass
        {
            [XmlAttribute("Path")]
            public required string Path { get; set; }

            [XmlAttribute("Type")]
            public string? Type { get; set; }
        }
        public class DestinationClass
        {
            [XmlAttribute("Path")]
            public required string Path { get; set; }
        }
        public class XmlElements2AddClass
        {
            [XmlAttribute("Path")]
            public required string Path { get; set; }

            [XmlAnyElement]
            public required XmlElement[] XmlElements { get; set; }
        }

        // Others
        public override string ToString()
        {
            string returnValue = "";

            // Keywords
            if (Keywords != null)
            {
                returnValue += "\nKeywords:\n";
                foreach (var _ in Keywords) returnValue += $"\tID:{_.ID}\n";
            }

            // Instructions
            if (Instructions != null)
            {
                returnValue += "\nInstructions:\n";
                foreach (var _ in Instructions) 
                {
                    switch (_.Type)
                    {
                        case "Copy":
                            if (_.Source != null && _.Destination != null)
                            {
                                returnValue += $"\tType:{_.Type}\n";
                                if (_.Source.Type != null)
                                    returnValue += $"\tSource:{_.Source.Path}\tType:{_.Source.Type}\n";
                                else                                
                                    returnValue += $"\tSource:{_.Source.Path}\n";
                                returnValue += $"\tDestination:{_.Destination.Path}\n";
                            }
                            break;
                            
                        case "AddXmlElement":
                            if (_.Destination != null && _.XmlElements2Add != null)
                            {
                                returnValue += $"\tType:{_.Type}\n";
                                returnValue += $"\tDestination:{_.Destination.Path}\n";
                                returnValue += $"\tPath:{_.XmlElements2Add.Path}\n";
                                foreach (var item in _.XmlElements2Add.XmlElements)
                                {
                                    returnValue += $"\tXmlElement:{item.OuterXml}\n";
                                }
                            }
                            break;
                    }
                    returnValue += "\n";
                }
            }

            return returnValue;
        }
    }

    public class Templates
    {     
    }

    public class Template
    {   
        private Dictionary<string,string> _configConstants = new ()
        {
            {"$CONFIG_FILE_NAME",""},
            {"$TEMPLATE_PATH",""},
            {"$USER_PATH",""},
            {"$PROJECT_PATH",""},
            {"$ACTIVE_CONFIGURATION_PATH",""}
        };
        public Dictionary<string,string> ConfigConstants { get => _configConstants;}

        private TemplateConfigClass? _config;
        public TemplateConfigClass? Config { get => _config;}

        public Template(string templatePath, string userPath)
        {           
            LoadConfig(templatePath, userPath);    
            SetKeywords();        
        }

        private void CalculateConstants(string templatePath, string userPath)
        {
            // These constants are fixed
            _configConstants["$CONFIG_FILE_NAME"] = "template_config.xml";

            _configConstants["$TEMPLATE_PATH"] = templatePath;

            _configConstants["$USER_PATH"] = userPath;

            // These constants are calculated only if required by template's config file
            string configFileFullName = Path.Combine(_configConstants["$TEMPLATE_PATH"],_configConstants["$CONFIG_FILE_NAME"]);
            if (!File.Exists(configFileFullName)) throw new Exception($"{configFileFullName} file not found!");
            string ConfigFileContent = File.ReadAllText(configFileFullName);

            if (ConfigFileContent.Contains("$PROJECT_PATH") || ConfigFileContent.Contains("$ACTIVE_CONFIGURATION_PATH"))
            {
                string projectPath = Utilities.GetASProjectPath(userPath);
                _configConstants["$PROJECT_PATH"] = projectPath;
                
                string activeConfig = Utilities.GetASActiveConfiguration(projectPath);
                _configConstants["$ACTIVE_CONFIGURATION_PATH"] = Utilities.GetASActiveConfigurationPath(projectPath,activeConfig);
            }
             
        }

        private void SetKeywords()
        {
            if (_config != null && _config.Keywords != null)
            {
                //_config.Keywords[0].Value = "FB3";
                //_config.Keywords[1].Value = "Library";

                _config.Keywords[0].Value = "prgTest";
            }
        }

        public void LoadConfig(string templatePath, string userPath)
        {
            CalculateConstants(templatePath, userPath);

            string configFileFullName = Path.Combine(_configConstants["$TEMPLATE_PATH"],_configConstants["$CONFIG_FILE_NAME"]);
            if (!File.Exists(configFileFullName)) throw new Exception($"{configFileFullName} file not found!");
            
            XmlSerializer serializer = new XmlSerializer(typeof(TemplateConfigClass));

            using (FileStream fileStream = new FileStream(configFileFullName, FileMode.Open))
            {
                _config = (TemplateConfigClass?)serializer.Deserialize(fileStream);

                if (_config == null) throw new Exception($"Failed to deserialize {configFileFullName}");

                // Convert CONSTANTS to real values:
                if (_config.Instructions != null)
                {
                    foreach (var item in _config.Instructions)
                    {
                        if(item.Destination != null) item.Destination.Path = ReplaceConstants(item.Destination.Path);
                        if(item.Source != null) item.Source.Path = ReplaceConstants(item.Source.Path);
                        if(item.XmlElements2Add != null) item.XmlElements2Add.Path = ReplaceConstants(item.XmlElements2Add.Path);                        
                    }
                }
            }
        }
        public void ExecuteInstructions()
        {
            if (_config != null && _config.Instructions != null)
            {
                foreach (var _ in _config.Instructions)
                {
                    switch (_.Type)
                    {
                        case "Copy":
                            Console.WriteLine("Copy instruction");
                            if (_.Source != null && _.Destination != null)
                            {                                
                                Console.WriteLine($"\tSrc:{_.Source.Path}\n\tDest:{_.Destination.Path}");
                                string newDestPath = Utilities.Copy(_.Destination.Path,_.Source.Path);
                                if (_config.Keywords != null)
                                    newDestPath = ReplaceKeywords(newDestPath,_config.Keywords);                                

                                if (_.Source.Type != null) // A type was specified -> Add xml element to descriptive file     
                                {                      
                                    Utilities.AddDescriptiveXmlElement(newDestPath,_.Source.Type);

                                    if (_config.Keywords != null)
                                    {
                                        string? DescriptivePath = Path.GetDirectoryName(newDestPath);
                                        if (DescriptivePath != null)
                                            ReplaceKeywords(Utilities.GetASDescriptiveFile(DescriptivePath).FullName,_config.Keywords);
                                    }
                                }
                            }                            
                            break;

                        case "Append":
                            Console.WriteLine("Append instruction");
                            if (_.Source != null && _.Destination != null)
                            {     
                                //Since destination file should already exists I have to replace keywords in it before proceed with append command
                                if (_config.Keywords != null)
                                    foreach (var item in _config.Keywords)
                                        _.Destination.Path = _.Destination.Path.Replace(item.ID,item.Value);      

                                Console.WriteLine($"\tSrc:{_.Source.Path}\n\tDest:{_.Destination.Path}");                    
                                
                                Utilities.Append(_.Destination.Path,_.Source.Path);
                                if (_config.Keywords != null)
                                    ReplaceKeywords(_.Destination.Path,_config.Keywords);
                            }                            
                            break;
                            
                        case "AddXmlElement":
                            Console.WriteLine("AddXmlElement instruction\n");
                            if (_.Destination != null && _.XmlElements2Add != null)
                                Utilities.AddXmlElementsToFile(_.XmlElements2Add.XmlElements,_.XmlElements2Add.Path,_.Destination.Path);
                            break;
                    }
                }
            }
        }
        private string ReplaceConstants(string str)
        {
            foreach (var item in _configConstants) str = str.Replace(item.Key,item.Value);
            
            return str;
        }
        private string ReplaceKeywords(string destPath, List<TemplateConfigClass.KeywordClass> replacementsList)
        {
            string ReplacedPath = string.Empty;

            if (File.Exists(destPath)) // Provided path is just a file
            {
                FileInfo destFile = new FileInfo(destPath);
                string NewfileName = destFile.FullName;
                
                // Replace keywords in the file name
                foreach (var item in replacementsList) NewfileName = NewfileName.Replace(item.ID,item.Value);
                if (destPath != NewfileName)
                    File.Move(destPath,NewfileName);
                
                ReplacedPath = NewfileName;

                // Replace keywords in the content
                string content = File.ReadAllText(NewfileName);
                foreach (var item in replacementsList) content = content.Replace(item.ID,item.Value);
                File.WriteAllText(NewfileName,content);
            }
            else if(Directory.Exists(destPath)) // Provided path is a folder
            {
                DirectoryInfo destDir = new DirectoryInfo(destPath);

                // Check if destDir name contains a keyword
                if (replacementsList.Any(_ => _.ID == destDir.Name))
                {
                    foreach (var item in replacementsList) destPath = destPath.Replace(item.ID,item.Value);
                    destDir.MoveTo(destPath);
                }
                ReplacedPath = destPath;

                // Rename files and contents
                FileInfo[] destFiles = destDir.GetFiles();
                foreach (FileInfo file in destFiles)
                {
                    string fileName = file.FullName;
                    foreach (var item in replacementsList) fileName = fileName.Replace(item.ID,item.Value);
                    file.MoveTo(fileName);

                    string content = File.ReadAllText(file.FullName);
                    foreach (var item in replacementsList) content = content.Replace(item.ID,item.Value);
                    File.WriteAllText(file.FullName,content);
                }

                // Rename folders and navigates in they
                DirectoryInfo[] directories = destDir.GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    string directoryName = directory.FullName;
                    foreach (var item in replacementsList) directoryName = directoryName.Replace(item.ID,item.Value);
                    directory.MoveTo(directoryName);
                    ReplaceKeywords(directoryName, replacementsList);
                }
            }
            else throw new Exception($"Invalid path to replace keywords: {destPath}");

            return ReplacedPath;
        }
    
    }

}