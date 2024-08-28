using System.Xml;
using System.Xml.Serialization;
using ConsoleTables;

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
   
    public class TemplateInfo
    {
        private readonly RepositoryInfo? _repository;
        public RepositoryInfo? Repository {get => _repository;}
        private readonly string _name;
        public string Name {get => _name;}
        private readonly string? _path;
        public string Path 
        {
            get 
            {
                if (_repository != null) return System.IO.Path.Combine(_repository.Path,_name);
                else return _path??"";
            }
        }

        public TemplateInfo(string path, string name)
        {
            _path = path;
            _name = name;
        }
        public TemplateInfo(RepositoryInfo repository, string name)
        {
            _repository = repository;
            _name = name;
        }
     
    }
    public class RepositoryInfo
    {
        private readonly string _name;
        public string Name {get => _name;}
        private readonly string _path;
        public string Path {get => _path;}
        private readonly List<TemplateInfo> _templates = [];
        public List<TemplateInfo> Templates {get => _templates;}
        public bool IsValid {get => Directory.Exists(Path);}

        public RepositoryInfo(string nameAndPath)
        {
            var splittedMembers = nameAndPath.Split('|');

            _name = splittedMembers[0];
            _path = splittedMembers[1];

            UpdateTemplatesInfo();
        }

        public RepositoryInfo(string name, string path)
        {
            _name = name;
            _path = path;

            UpdateTemplatesInfo();
        }

        public void UpdateTemplatesInfo()
        {
            _templates.Clear();

            if (IsValid)
            {
                DirectoryInfo dir = new(_path);
                
                foreach (var folder in dir.GetDirectories())
                {
                    if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                        _templates.Add(new TemplateInfo(this,folder.Name));    
                }
            }  
            else throw new Exception($"Repository {_name} is not longer valid");     
            
        } 

    }
    
    public class Logic
    {
        private readonly List<RepositoryInfo> _repositoriesInfo = [];
        private Template? _template;
        private readonly string _configFilePath = "";
        
        public Logic(string configFilePath)
        {
            _configFilePath = configFilePath;

            InitRepositoriesInfo();
        }

        private void InitRepositoriesInfo()
        {
            // Clear repositories list
            _repositoriesInfo.Clear();

            // Open config.ini file 
            var iniFile = new IniFile(_configFilePath);

            // Read all parameters under [Templates] section named like Repository1, Repository2...
            for (int i = 1; ; i++)
            {
                string repositoryString = iniFile.Read("Templates", $"Repository{i}");
                if (string.IsNullOrEmpty(repositoryString))
                    break;
                
                var splittedRepository = repositoryString.Split('|');

                RepositoryInfo repository = new(splittedRepository[0],splittedRepository[1]);

                if (!_repositoriesInfo.Any(_ => _.Name == repository.Name || _.Path == repository.Path) && repository.IsValid) _repositoriesInfo.Add(repository);
            }
        }
        public void Execute(Command.Templates commands, bool executionByUI)
        {            
            CheckCommand(commands);
            
            if(executionByUI) UICommands(commands);
            else ConsoleCommands(commands);
        }
        private void CheckCommand(Command.Templates commands)
        {
            // ^ == XOR

            if (commands.RepositoryAdd == null ^ commands.RepositoryAddPath == null) throw new Exception($"Incomplete command");
 
            if (commands.LoadTemplate == null ^ commands.LoadTemplateRepo == null) throw new Exception($"Incomplete command");

            if (commands.Exec && commands.ExecWorkingDir == null) throw new Exception($"Incomplete command");

            if (commands.KeywordInsertName == null ^ commands.KeywordInsertValue == null) throw new Exception($"Incomplete command");

        }
        private void CommandRepositoryAdd(string name, string path)
        {
            if (_repositoriesInfo.Any(_ => _.Name == name || _.Path == path)) throw new Exception($"The repository already exists");  

            RepositoryInfo newRepository = new(name, path);

            if (newRepository.IsValid) // Add repository to config file
            {
                var iniFile = new IniFile(_configFilePath);

                for (int i = 1; ; i++)
                {
                    string tmpRepositoryString = iniFile.Read("Templates", $"Repository{i}");
                    if (string.IsNullOrEmpty(tmpRepositoryString))
                    {
                        iniFile.Write("Templates", $"Repository{i}",$"{newRepository.Name}|{newRepository.Path}");
                        break;
                    }
                }
            }

            _repositoriesInfo.Add(newRepository);
        }
        private void CommandRepositoryRemove(string name)
        {
            if (!_repositoriesInfo.Any(_ => _.Name == name)) throw new Exception($"{name} is not a listed repository!");
            
            var iniFile = new IniFile(_configFilePath);
            var shift = false;
            for (int i = 1; ; i++)
            {
                string actRepositoryString = iniFile.Read("Templates", $"Repository{i}");

                if (shift)
                {    
                    if (string.IsNullOrEmpty(actRepositoryString)) {
                        iniFile.DeleteKey("Templates", $"Repository{i-1}");
                        break;    
                    }   
                    else iniFile.Write("Templates", $"Repository{i-1}",actRepositoryString);                    
                }

                if (string.IsNullOrEmpty(actRepositoryString)) break;
                else if (actRepositoryString.Split('|')[0] == name) shift = true;    
            }
            
            _repositoriesInfo.Remove(_repositoriesInfo.First(_ => _.Name == name));
        }
        private void CommandLoadTemplate(string repositoryName, string templateName)
        {            
            // Create a complete new instance of _template.
            try
            {
                _template = new(_repositoriesInfo.First(_ => _.Name == repositoryName).Templates.First(_ => _.Name == templateName));
            }
            catch (Exception)
            {
                _template = null; // In case of error delete previously created _template instances
                throw;
            }
        }
        private void CommandUnloadTemplate()
        {
            _template = null;
        }
        private void CommandKeywordInsert(string name, string? value)
        {
            if (_template == null) throw new Exception($"No template loaded"); 
            if (name == null) throw new Exception($"No keyword name provided");
            if (value == null) throw new Exception($"No keyword value provided");
                                                       
            _template.SetKeywordValue(new TemplateConfigClass.KeywordClass(){ID = name,Value = value});               
        }
        private void CommandKeywordClean()
        {
            if (_template == null) throw new Exception($"No template loaded");  
            
            _template.ResetKeywordsValues();            
        }
        
        // Templates - Console
        private void ConsoleCommands(Command.Templates commands)
        {
            //Commands typ 1
            if (commands.LoadTemplateRepo != null && commands.LoadTemplate != null) CommandLoadTemplate(commands.LoadTemplateRepo,commands.LoadTemplate);

            //Commands typ 2
            if (commands.TemplatesList) ConsoleWriteTemplatesList();    
            else if (commands.LoadedTemplate) ConsoleCommandLoadedTemplate();   
            else if (commands.UnloadTemplate) CommandUnloadTemplate();          
            else if (commands.Exec && commands.ExecWorkingDir != null) ConsoleCommandExec(commands.ExecWorkingDir);          
            else if (commands.KeywordsList) ConsoleCommandKeywordsList();  
            else if (commands.KeywordInsertName != null) CommandKeywordInsert(commands.KeywordInsertName, commands.KeywordInsertValue); 
            else if (commands.KeywordsClean) CommandKeywordClean(); 
            else if (commands.RepositoriesList) ConsoleCommandRepositoriesList(); 
            else if (commands.RepositoryAdd != null && commands.RepositoryAddPath != null) CommandRepositoryAdd(commands.RepositoryAdd,commands.RepositoryAddPath); 
            else if (commands.RepositoryRemove != null) CommandRepositoryRemove(commands.RepositoryRemove); 
                  
        }
        private void ConsoleWriteTemplatesList()
        {
            var table = new ConsoleTable("ID", "Repository" ,"Name", "Path");
            
            var id = 0;
            foreach (var repository in _repositoriesInfo)
            {
                foreach (var template in repository.Templates)
                {
                    id++;
                    table.AddRow(id,repository.Name,template.Name,template.Path);  
                }
            }
                
            table.Write(Format.Minimal); 
        }
        private void ConsoleCommandRepositoriesList()
        {    
            var table = new ConsoleTable("Name", "Path");
            
            foreach (var repository in _repositoriesInfo)
                table.AddRow(repository.Name,repository.Path);                  
                            
            table.Write(Format.Minimal);           
        }
        private void ConsoleCommandExec(string execWorkingDir)
        {   
            if (_template == null) throw new Exception($"No template loaded");

            if(!_template.KeywordsReady && _template.Config.Keywords != null) // Ask user to input keywords
            {
                foreach (var keyword in _template.Config.Keywords)
                {
                    do
                    {
                        Console.Write($"Please, insert the keyword value of {keyword.ID}:");
                        keyword.Value = Console.ReadLine();    
                    } while(keyword.Value == null);                    
                    _template.SetKeywordValue(keyword);
                }
            }

            _template.Execute(execWorkingDir);  
            _template.ResetKeywordsValues(); // This prevents undesired repetitions          
        }
        private void ConsoleCommandKeywordsList()
        {
            if (_template == null) throw new Exception($"No template loaded");  
            
            var table = new ConsoleTable("ID", "Value");
            
            if (_template.Config.Keywords != null)
                foreach (var keyword in _template.Config.Keywords)
                    table.AddRow(keyword.ID,keyword.Value);                     

            table.Write(Format.Minimal);              
        }
        private void ConsoleCommandLoadedTemplate()
        {
            if (_template == null) throw new Exception($"No template loaded");  

            var table = new ConsoleTable("Repository" ,"Name", "Path");

            table.AddRow(_template.TemplateInfo.Repository?.Name,_template.TemplateInfo.Name,_template.TemplateInfo.Path); 
            
            table.Write(Format.Minimal); 
        }

        // Templates - UI
        private void UICommands(Command.Templates commands)
        {
            //Commands typ 1
            if (commands.LoadTemplateRepo != null && commands.LoadTemplate != null) CommandLoadTemplate(commands.LoadTemplateRepo,commands.LoadTemplate);    
            
            //Commands typ 2
            if (commands.TemplatesList) UICommandTemplatesList();   
            else if (commands.LoadedTemplate) UICommandLoadedTemplate();   
            else if (commands.UnloadTemplate) CommandUnloadTemplate();   
            else if (commands.Exec && commands.ExecWorkingDir != null) UICommandExec(commands.ExecWorkingDir);    
            else if (commands.KeywordsList) UICommandKeywordsList(); 
            else if (commands.KeywordInsertName != null) CommandKeywordInsert(commands.KeywordInsertName, commands.KeywordInsertValue);    
            else if (commands.KeywordsClean) CommandKeywordClean(); 
            else if (commands.RepositoriesList) UICommandRepositoriesList();    
            else if (commands.RepositoryAdd != null && commands.RepositoryAddPath != null) CommandRepositoryAdd(commands.RepositoryAdd,commands.RepositoryAddPath); 
            else if (commands.RepositoryRemove != null) CommandRepositoryRemove(commands.RepositoryRemove); 
                      
        }
        private void UICommandTemplatesList()
        {           
            foreach (var repository in _repositoriesInfo)
            {
                foreach (var template in repository.Templates)
                    Console.WriteLine($"{repository.Name}|{template.Name}|{template.Path}");
            }    
        }
        private void UICommandRepositoriesList()
        {             
            foreach (var repository in _repositoriesInfo)
                Console.WriteLine($"{repository.Name}|{repository.Path}"); 
        }
        private void UICommandExec(string execWorkingDir)
        {
            if (_template == null) throw new Exception($"No template loaded");  
            if (!_template.Ready) throw new Exception($"Template not ready to be installed");

            _template.Execute(execWorkingDir);  
            _template.ResetKeywordsValues(); // This prevents undesired repetitions                 
        }
        private void UICommandKeywordsList()
        {
            if (_template == null) throw new Exception($"No template loaded"); 
              
            if (_template.Config.Keywords != null)
                foreach (var keyword in _template.Config.Keywords)
                    Console.WriteLine($"{keyword.ID}|{keyword.Value}");                        
        }
        private void UICommandLoadedTemplate()
        {
            if (_template == null) throw new Exception($"No template loaded");  

            Console.WriteLine($"{_template.TemplateInfo.Repository?.Name}|{_template.TemplateInfo.Name}|{_template.TemplateInfo.Path}");    

        }

    }

    public class Template
    {   
        // Constants
        private const string DefaultTemplateConfigFile = "template_config.xml";

        // Fields & properties
        private readonly Dictionary<string,string> _configConstants = new ()
        {
            {"$CONFIG_FILE_NAME",string.Empty},
            {"$TEMPLATE_PATH",string.Empty},
            {"$USER_PATH",string.Empty},
            {"$PROJECT_PATH",string.Empty},
            {"$ACTIVE_CONFIGURATION_PATH",string.Empty}
        };
        public Dictionary<string,string> ConfigConstants { get => _configConstants;}
        private readonly TemplateConfigClass _config;
        public TemplateConfigClass Config { get => _config;}
        public bool KeywordsReady {get => _config.Keywords?.All(_ => !string.IsNullOrEmpty(_.Value)) ?? true;}
        public bool Ready { get => KeywordsReady; }
        private readonly TemplateInfo _templateInfo;
        public TemplateInfo TemplateInfo { get => _templateInfo;}
        
        // Constructor
        public Template(TemplateInfo templateInfo)
        {        
            if (!Directory.Exists(templateInfo.Path)) throw new Exception($"Template {templateInfo.Path} is not valid");

            _config = GetConfig(templateInfo.Path);
            _templateInfo = templateInfo; 
        }

        // Methods
        public void Execute(string userPath)
        {
            if (!Directory.Exists(userPath)) throw new Exception($"Path {userPath} is not valid");

            CalculateConfigConstants(_templateInfo.Path, userPath);

            // Replace constants in config structure:            
            foreach (var item in _config.Instructions)
            {
                if(item.Destination != null) item.Destination.Path = ReplaceConstants(item.Destination.Path);
                if(item.Source != null) item.Source.Path = ReplaceConstants(item.Source.Path);
                if(item.XmlElements2Add != null) item.XmlElements2Add.Path = ReplaceConstants(item.XmlElements2Add.Path);                        
            }            
            
            // Execute instructions
            if (!Ready) throw new Exception($"Template not ready yet. Try set keywords before execute.");
            foreach (var instruction in _config.Instructions)
            {
                switch (instruction.Type)
                {
                    case "Copy":
                        if (instruction.Source == null || instruction.Destination == null) throw new Exception($"Copy instruction failed! Invalid paths.");
                                                        
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
                                                   
                        break;

                    case "Append":
                        if (instruction.Source == null || instruction.Destination == null) throw new Exception($"Append instruction failed! Invalid paths.");
                             
                        //Since destination file should already exists I have to replace keywords in it before proceed with append command
                        if (_config.Keywords != null)
                            foreach (var keyword in _config.Keywords)
                                instruction.Destination.Path = instruction.Destination.Path.Replace(keyword.ID,keyword.Value);                    
                        
                        Append(instruction.Destination.Path,instruction.Source.Path);
                        
                        if (_config.Keywords != null)
                            ReplaceKeywords(instruction.Destination.Path,_config.Keywords);
                                                    
                        break;
                        
                    case "AddXmlElement":
                        if (instruction.Destination == null || instruction.XmlElements2Add == null) throw new Exception($"AddXmlElement instruction failed! Invalid path or Xml elements");
                        
                        AddXmlElementsToFile(instruction.XmlElements2Add.XmlElements,instruction.XmlElements2Add.Path,instruction.Destination.Path);
                        break;
                
                    default:
                        throw new Exception($"Instruction type not supported.");
                }
            }
            
        }
        public void SetKeywordValue(TemplateConfigClass.KeywordClass keyword)
        {
            if (_config.Keywords != null && keyword.ID != string.Empty)
                _config.Keywords.First(_ => _.ID == keyword.ID).Value = keyword.Value; 
            else throw new Exception($"Wrong keyword {keyword} provided!");
        }
        public void ResetKeywordsValues()
        {
            _config.Keywords?.ForEach(_ => _.Value = string.Empty); 
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
        private static TemplateConfigClass GetConfig(string templatePath)
        {
            string configFileFullName = Path.Combine(templatePath,DefaultTemplateConfigFile);
            if (!File.Exists(configFileFullName)) throw new Exception($"{configFileFullName} file not found!");
            
            XmlSerializer serializer = new(typeof(TemplateConfigClass));

            using FileStream fileStream = new(configFileFullName, FileMode.Open);
            var tempConfig = (TemplateConfigClass?)serializer.Deserialize(fileStream) ?? throw new Exception($"Failed to deserialize {configFileFullName}");
            
            return tempConfig;
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
                FileInfo destFile = new(destPath);
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
                DirectoryInfo destDir = new(destPath);

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
            string copiedPath;

            if (File.Exists(sourcePath)) // Copying a single file
            {
                FileInfo file = new(sourcePath);
                copiedPath = Path.Combine(destPath,file.Name);
                file.CopyTo(copiedPath);
            }
            else if (Directory.Exists(sourcePath)) // Copying a folder
            {                
                DirectoryInfo sourceDir = new(sourcePath);
                DirectoryInfo destDir = new(destPath);

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
            else throw new Exception($"Invalid path to copy: {sourcePath}");

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
            XmlDocument xmlDoc = new();
            xmlDoc.Load(lastUserFile);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new(xmlDoc.NameTable);
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
            XmlDocument xmlDoc = new();
            xmlDoc.Load(activeConfigFile);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new(xmlDoc.NameTable);
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
            try
            {
                DirectoryInfo dirInfo = new(path);
                return dirInfo.GetFiles().First(file => file.Extension == ".pkg" || file.Extension == ".lby");   
            }
            catch (System.Exception)
            {
                throw new Exception($"Cannot find descriptive file (.pkg or .lby) in {path}");
            }   
        }
        private static void AddXmlElementsToFile(XmlElement[] elements, string xmlPath, string filePath)
        {
            if (!File.Exists(filePath)) throw new Exception($"Cannot find xml file {filePath}.");

            // Carica il documento XML dal file
            XmlDocument xmlDoc = new();
            xmlDoc.Load(filePath);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new(xmlDoc.NameTable);
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
            string? descriptiveDirName = Path.GetDirectoryName(addedItemPath) ?? throw new Exception($"Cannot find {addedItemPath} folder");
            FileInfo descriptiveFile = GetASDescriptiveFile(descriptiveDirName);    

            // Get ItemName
            string itemName = Path.GetFileName(addedItemPath);  
               
            // Get Xml element
            XmlElement[] newElement = [GetDescriptiveXmlElement(type,itemName)];

            // Get xmlPath
            string xmlPath = descriptiveFile.Extension switch
            {
                ".pkg" => "/ns:Package/ns:Objects",
                ".lby" => "/ns:Library/ns:Objects",
                _ => throw new Exception($"Descriptive file extension {descriptiveFile.Extension} not supported."),
            };

            // Add element to xml
            AddXmlElementsToFile(newElement,xmlPath,descriptiveFile.FullName);
        }
        private static XmlElement GetDescriptiveXmlElement(string type, string name)
        {
            XmlDocument xmlDoc = new();

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