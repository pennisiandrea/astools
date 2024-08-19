using System.Xml;
using System.Xml.Serialization;

namespace ASTools.Core.Tools.Templates
{
    [XmlRoot("Template")]
    public class TemplateConfigClass
    {
        [XmlArray("Keywords")]
        [XmlArrayItem("Keyword")]
        public List<KeywordClass>? Keywords {get; set;}

        [XmlArray("Instructions")]
        [XmlArrayItem("Instruction")]
        public required List<InstructionClass> Instructions {get; set;}

        // Submembers
        public class KeywordClass
        {
            [XmlAttribute("ID")]
            public required string ID { get; set; }
            
            [XmlText]
            public string? Value { get; set; }

            public override string ToString() => $"ID:{ID}\tValue:{Value}";
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
        public class TemplateListItem
        {
            public int ID;
            public string Path;
            public string Name;

            public TemplateListItem(int id, string path, string name)
            {
                ID = id;
                Path = path;
                Name = name;
            }            
        }
        private List<TemplateListItem> _templatesList = new();
        public List<TemplateListItem> TemplatesList { get => _templatesList;}

        public Templates()
        {            

        }
        
        public List<TemplateListItem> UpdateTemplatesList(string[] repositories)
        {
            _templatesList.Clear();
            int id = 0;
            foreach (var path in repositories)
            {
                if (!Directory.Exists(path)) throw new Exception($"Templates path {path} invalid");
                DirectoryInfo dir = new(path);

                foreach (var template in dir.GetDirectories())
                    _templatesList.Add(new TemplateListItem(id++,template.FullName,template.Name));                
            }
                        
            return TemplatesList;
        }
    
    }

    public class Template
    {   
        // Fields
        private Dictionary<string,string> _configConstants = new ()
        {
            {"$CONFIG_FILE_NAME",string.Empty},
            {"$TEMPLATE_PATH",string.Empty},
            {"$USER_PATH",string.Empty},
            {"$PROJECT_PATH",string.Empty},
            {"$ACTIVE_CONFIGURATION_PATH",string.Empty}
        };
        public Dictionary<string,string> ConfigConstants { get => _configConstants;}
        private TemplateConfigClass _config;
        public TemplateConfigClass Config { get => _config;}
        public bool KeywordsReady {get => _config.Keywords?.All(_ => _.Value != null) ?? true;}
        public bool Ready { get => KeywordsReady; }
        private string _templatePath;
        public string TemplatePath { get => _templatePath;}

        private const string DefaultTemplateConfigFile = "template_config.xml";
        // Constructor
        public Template(string templatePath)
        {         
            Init(templatePath);  
            if (_config == null) throw new Exception($"Invalid configuration"); 
            if (_templatePath == null) throw new Exception($"Invalid template"); 
        }

        // Methods
        public void Init (string templatePath)
        {
            _config = GetConfig(templatePath);
            _templatePath = templatePath;
        }
        public void Execute(string userPath)
        {
            CalculateConfigConstants(_templatePath, userPath);

            // Replace constants in config structure:            
            foreach (var item in _config.Instructions)
            {
                if(item.Destination != null) item.Destination.Path = ReplaceConstants(item.Destination.Path);
                if(item.Source != null) item.Source.Path = ReplaceConstants(item.Source.Path);
                if(item.XmlElements2Add != null) item.XmlElements2Add.Path = ReplaceConstants(item.XmlElements2Add.Path);                        
            }            
            
            // Execute instructions
            if (!Ready) throw new Exception($"Template not ready yet. Try set keywords.");
            foreach (var instruction in _config.Instructions)
            {
                switch (instruction.Type)
                {
                    case "Copy":
                        if (instruction.Source != null && instruction.Destination != null)
                        {                                
                            string newDestPath = Copy(instruction.Destination.Path,instruction.Source.Path);
                            if (_config.Keywords != null)
                                newDestPath = ReplaceKeywords(newDestPath,_config.Keywords);                                

                            if (instruction.Source.Type != null) // A type was specified -> Add xml element to descriptive file     
                            {                      
                                AddDescriptiveXmlElement(newDestPath,instruction.Source.Type);

                                if (_config.Keywords != null)
                                {
                                    string? descriptivePath = Path.GetDirectoryName(newDestPath);
                                    if (descriptivePath != null)
                                        ReplaceKeywords(GetASDescriptiveFile(descriptivePath).FullName,_config.Keywords);
                                }
                            }
                        }                            
                        break;

                    case "Append":
                        if (instruction.Source != null && instruction.Destination != null)
                        {     
                            //Since destination file should already exists I have to replace keywords in it before proceed with append command
                            if (_config.Keywords != null)
                                foreach (var keyword in _config.Keywords)
                                    instruction.Destination.Path = instruction.Destination.Path.Replace(keyword.ID,keyword.Value);                    
                            
                            Append(instruction.Destination.Path,instruction.Source.Path);
                            if (_config.Keywords != null)
                                ReplaceKeywords(instruction.Destination.Path,_config.Keywords);
                        }                            
                        break;
                        
                    case "AddXmlElement":
                        if (instruction.Destination != null && instruction.XmlElements2Add != null)
                            AddXmlElementsToFile(instruction.XmlElements2Add.XmlElements,instruction.XmlElements2Add.Path,instruction.Destination.Path);
                        break;
                }
            }
            
        }
        public void SetKeywordValue(TemplateConfigClass.KeywordClass keyword)
        {
            if (_config.Keywords != null && keyword.ID != string.Empty)
                _config.Keywords.First(_ => _.ID == keyword.ID).Value = keyword.Value; 
            else throw new Exception($"Wrong keyword {keyword} provided!");
        }
        private void CalculateConfigConstants(string templatePath, string userPath)
        {
            // These constants are fixed
            _configConstants["$CONFIG_FILE_NAME"] = DefaultTemplateConfigFile;

            _configConstants["$TEMPLATE_PATH"] = templatePath;

            _configConstants["$USER_PATH"] = userPath;

            // These constants are calculated only if required by template's config file
            string configFileFullName = Path.Combine(_configConstants["$TEMPLATE_PATH"],_configConstants["$CONFIG_FILE_NAME"]);
            if (!File.Exists(configFileFullName)) throw new Exception($"{configFileFullName} file not found!");
            string configFileContent = File.ReadAllText(configFileFullName);

            if (configFileContent.Contains("$PROJECT_PATH") || configFileContent.Contains("$ACTIVE_CONFIGURATION_PATH"))
            {
                string projectPath = GetASProjectPath(userPath);
                _configConstants["$PROJECT_PATH"] = projectPath;
                
                string activeConfig = GetASActiveConfigurationName(projectPath);
                _configConstants["$ACTIVE_CONFIGURATION_PATH"] = GetASActiveConfigurationPath(projectPath,activeConfig);
            }
            else
            {
                _configConstants["$PROJECT_PATH"] = string.Empty;
                _configConstants["$ACTIVE_CONFIGURATION_PATH"] = string.Empty;
            }
             
        }
        private TemplateConfigClass GetConfig(string templatePath)
        {
            string configFileFullName = Path.Combine(templatePath,DefaultTemplateConfigFile);
            if (!File.Exists(configFileFullName)) throw new Exception($"{configFileFullName} file not found!");
            
            XmlSerializer serializer = new XmlSerializer(typeof(TemplateConfigClass));

            using (FileStream fileStream = new FileStream(configFileFullName, FileMode.Open))
            {
                var tempConfig = (TemplateConfigClass?)serializer.Deserialize(fileStream);

                if (tempConfig == null) throw new Exception($"Failed to deserialize {configFileFullName}");

                return tempConfig;
            }

        }
        private string ReplaceConstants(string str)
        {
            foreach (var constant in _configConstants) str = str.Replace(constant.Key,constant.Value);
            
            return str;
        }
        private static string ReplaceKeywords(string destPath, List<TemplateConfigClass.KeywordClass> replacementsList)
        {
            string replacedPath = string.Empty;

            if (File.Exists(destPath)) // Provided path is just a file
            {
                FileInfo destFile = new FileInfo(destPath);
                string newfileName = destFile.FullName;
                
                // Replace keywords in the file name
                foreach (var replacement in replacementsList) newfileName = newfileName.Replace(replacement.ID,replacement.Value);
                if (destPath != newfileName)
                    File.Move(destPath,newfileName);
                
                replacedPath = newfileName;

                // Replace keywords in the content
                string content = File.ReadAllText(newfileName);
                foreach (var replacement in replacementsList) content = content.Replace(replacement.ID,replacement.Value);
                File.WriteAllText(newfileName,content);
            }
            else if(Directory.Exists(destPath)) // Provided path is a folder
            {
                DirectoryInfo destDir = new DirectoryInfo(destPath);

                // Check if destDir name contains a keyword
                if (replacementsList.Any(_ => _.ID == destDir.Name))
                {
                    foreach (var replacement in replacementsList) destPath = destPath.Replace(replacement.ID,replacement.Value);
                    destDir.MoveTo(destPath);
                }
                replacedPath = destPath;

                // Rename files and contents
                FileInfo[] destFiles = destDir.GetFiles();
                foreach (FileInfo file in destFiles)
                {
                    string fileName = file.FullName;
                    foreach (var replacement in replacementsList) fileName = fileName.Replace(replacement.ID,replacement.Value);
                    file.MoveTo(fileName);

                    string content = File.ReadAllText(file.FullName);
                    foreach (var replacement in replacementsList) content = content.Replace(replacement.ID,replacement.Value);
                    File.WriteAllText(file.FullName,content);
                }

                // Rename folders and navigates in they
                DirectoryInfo[] directories = destDir.GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    string directoryName = directory.FullName;
                    foreach (var replacement in replacementsList) directoryName = directoryName.Replace(replacement.ID,replacement.Value);
                    directory.MoveTo(directoryName);
                    ReplaceKeywords(directoryName, replacementsList);
                }
            }
            else throw new Exception($"Invalid path to replace keywords: {destPath}");

            return replacedPath;
        }
        private static string Copy(string destPath, string sourcePath)
        {
            string copiedPath = string.Empty;

            if (File.Exists(sourcePath)) // Copying a single file
            {
                FileInfo file = new FileInfo(sourcePath);
                copiedPath = Path.Combine(destPath,file.Name);
                file.CopyTo(copiedPath);
            }
            else if (Directory.Exists(sourcePath)) // Copying a folder
            {                
                DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
                DirectoryInfo destDir = new DirectoryInfo(destPath);

                // Check if in destPath exists a directory like sourcePath. This sould be true only the first time
                if (sourceDir.Name != destDir.Name)
                {
                    destPath = Path.Combine(destPath,sourceDir.Name);
                    Directory.CreateDirectory(destPath);
                }
                copiedPath = destPath;

                // Copy all files
                FileInfo[] sourceFiles = sourceDir.GetFiles();
                foreach (FileInfo file in sourceFiles) 
                    file.CopyTo(Path.Combine(destPath,file.Name));
                
                // Copy all folders
                DirectoryInfo[] directories = sourceDir.GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    string newDestPath = Path.Combine(destPath,directory.Name);
                    Directory.CreateDirectory(newDestPath);                
                    Copy(newDestPath, directory.FullName);
                }             
            }
            return copiedPath;
        }
        private static void Append(string destPath, string sourcePath)
        {
            // Append source content to dest content

            if (!File.Exists(destPath) || !File.Exists(sourcePath)) throw new Exception($"Failed to append {sourcePath} to {destPath}");
            
            string sourceContent = File.ReadAllText(sourcePath);
            string destContent = File.ReadAllText(destPath);

            File.WriteAllText(destPath,destContent + "\n" + sourceContent);
        }
        private static string GetASProjectPath(string path)
        {
            // Check if .apj file is in this directory
            DirectoryInfo actDir = new(path);
            FileInfo[] files = actDir.GetFiles();            
            if (files.Any(file => file.Extension == ".apj")) return actDir.FullName;
            else 
            {
                if (actDir.Parent != null) return GetASProjectPath(actDir.Parent.FullName);
                else throw new Exception($"Automation Studio project not found.");
            }
        }
        private static string GetASActiveConfigurationName(string projectPath)
        {
            string lastUserFile = Path.Combine(projectPath,"LastUser.set");
            if (!File.Exists(lastUserFile)) throw new Exception($"Cannot find file {lastUserFile}");

            // Carica il documento XML dal file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(lastUserFile);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            if (xmlDoc.DocumentElement != null)
            {
                string namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
                nsmgr.AddNamespace("ns", namespaceUri);
            } 
        
            // Trova l'elemento specificato dal percorso 
            XmlNode? node = xmlDoc.SelectSingleNode("/ns:ProjectSettings/ns:ConfigurationManager",nsmgr);

            if (node is XmlElement element)
            {
                var attribute = element.Attributes["ActiveConfigurationName"];
                if (attribute != null) return attribute.Value;  
                else throw new Exception($"{lastUserFile} ActiveConfigurationName attribute not found");     
            }            
            else throw new Exception($"{lastUserFile} file structure not supported");
        }
        private static string GetASActiveConfigurationPath(string projectPath, string activeConfigName)
        {
            // Carica il documento XML dal file
            string activeConfigFile = Path.Combine(projectPath,"Physical",activeConfigName,"Config.pkg");
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(activeConfigFile);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            if (xmlDoc.DocumentElement != null)
            {
                string namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
                nsmgr.AddNamespace("ns", namespaceUri);
            } 
        
            // Searching for CPU name
            string? cpuName = null; 
            XmlNode? parentNode = xmlDoc.SelectSingleNode("/ns:Configuration/ns:Objects",nsmgr);

            if (parentNode != null)
            {
                foreach (XmlNode childNode in parentNode.ChildNodes)
                {
                    if (childNode is XmlElement element)
                    {                       
                        var attribute = element.Attributes["Type"];
                        if (attribute != null && attribute.Value == "Cpu")
                        {
                            cpuName = element.InnerText;
                            break;
                        }
                    }    
                }
            }            
            else throw new Exception($"{activeConfigFile} file structure not supported");
            
            if (cpuName == null) throw new Exception($"{activeConfigFile} cannot find Cpu");

            return Path.Combine(activeConfigFile,cpuName);
        }
        private static FileInfo GetASDescriptiveFile(string path)
        {
            DirectoryInfo dirInfo = new(path);
            return dirInfo.GetFiles().First(file => file.Extension == ".pkg" || file.Extension == ".lby");    
        }
        private static void AddXmlElementsToFile(XmlElement[] elements, string xmlPath, string filePath)
        {
            // Carica il documento XML dal file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            string namespaceUri = "";
            if (xmlDoc.DocumentElement != null)
            {
                namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
                nsmgr.AddNamespace("ns", namespaceUri);
            } 
        
            // Trova l'elemento specificato dal percorso XPath
            XmlNode? parentNode = xmlDoc.SelectSingleNode(xmlPath,nsmgr);

            if (parentNode != null)
            {
                // Aggiunge l'elemento come figlio del nodo trovato
                foreach (var element in elements)
                {
                    XmlElement newElement = xmlDoc.CreateElement(element.LocalName,namespaceUri);
                    newElement.InnerText = element.InnerText;
                    foreach (XmlAttribute attribute in element.Attributes)
                        newElement.SetAttribute(attribute.Name, attribute.Value);
        
                    XmlNode importedNode = xmlDoc.ImportNode(newElement, true);
                    parentNode.AppendChild(importedNode);  
                }

                // Salva il documento XML aggiornato
                xmlDoc.Save(filePath);

            }
            else throw new Exception($"Cannot find path {xmlPath} in file {filePath}. Failed to add xml elements");
        } 
        private static void AddDescriptiveXmlElement(string addedItemPath, string type)
        {      
            // Get DescriptiveFile
            string? descriptiveDirName = Path.GetDirectoryName(addedItemPath);
            if (descriptiveDirName == null) throw new Exception($"Cannot find {addedItemPath} folder");
            FileInfo descriptiveFile = GetASDescriptiveFile(descriptiveDirName);    

            // Get ItemName
            string itemName = Path.GetFileName(addedItemPath);  
               
            // Get Xml element
            XmlElement[] newElement = [GetDescriptiveXmlElement(type,itemName)];

            // Get xmlPath
            string? xmlPath = null;
            switch (descriptiveFile.Extension)
            {
                case ".pkg":
                    xmlPath = "/ns:Package/ns:Objects";
                    break;

                case ".lby":      
                    xmlPath = "/ns:Library/ns:Objects";
                    break;                    
            }
            if (xmlPath == null) throw new Exception($"Descriptive file {descriptiveFile.FullName} not supported.");

            // Add element to xml
            AddXmlElementsToFile(newElement,xmlPath,descriptiveFile.FullName);
        }
        private static XmlElement GetDescriptiveXmlElement(string type, string name)
        {
            XmlDocument xmlDoc = new XmlDocument();

            string? xmlString = null;

            switch (type.ToLower())
            {
                case "package":   
                    xmlString = $"<Object Type=\"Package\">{name}</Object>";
                    break;

                case "file":   
                    xmlString = $"<Object Type=\"File\">{name}</Object>";
                    break;

                case "library_binary":   
                    xmlString = $"<Object Type=\"Library\" Language=\"binary\">{name}</Object>";
                    break;

                case "library_iec":   
                    xmlString = $"<Object Type=\"Library\" Language=\"IEC\">{name}</Object>";
                    break;

                case "program_iec":   
                    xmlString = $"<Object Type=\"Program\" Language=\"IEC\">{name}</Object>";
                    break;
            }

            if (xmlString == null) throw new Exception($"Specified type {type} of element {name} not supported.");
            xmlDoc.LoadXml(xmlString);
            
            if (xmlDoc.DocumentElement == null) throw new Exception($"Something went wrong generating Xml descriptive object of type {type} and name {name}");
            return xmlDoc.DocumentElement;
        }
    }

}