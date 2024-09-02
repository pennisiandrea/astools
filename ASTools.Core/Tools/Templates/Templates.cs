using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using ASTools.Library;
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
        private string _name;
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

        public void Rename(string newName)
        {
            _name = newName;
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
        private static void CheckCommand(Command.Templates commands)
        {
            // ^ == XOR

            if (commands.RepositoryAdd == null ^ commands.RepositoryAddPath == null) throw new Exception($"Incomplete command");
 
            if (commands.LoadTemplate == null ^ commands.LoadTemplateRepo == null) throw new Exception($"Incomplete command");

            if (commands.Exec && commands.ExecWorkingDir == null) throw new Exception($"Incomplete command");

            if (commands.DeleteTemplateName == null ^ commands.DeleteTemplateRepo == null) throw new Exception($"Incomplete command");

            if (commands.RenameRepoNewName == null ^ commands.RenameRepoActName == null) throw new Exception($"Incomplete command");

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
                                                       
            _template.SetKeywordValue(new TemplateConfigClass.KeywordClass(){ID = name,Value = value});               
        }
        private void CommandKeywordClean()
        {
            if (_template == null) throw new Exception($"No template loaded");  
            
            _template.ResetKeywordsValues();            
        }
        private void CommandDeleteTemplate(string name, string repository)
        {         
            // Unload if loaded            
            if (_template != null && _template.TemplateInfo.Repository?.Name == repository && _template.TemplateInfo.Name == name)
                CommandUnloadTemplate();
            
            // Delete folder
            Directory.Delete(_repositoriesInfo.First(_ => _.Name == repository).Templates.First(_ => _.Name == name).Path,true); 

            // Update repository
            if (repository != null)
                _repositoriesInfo.First(_ => _.Name == repository).UpdateTemplatesInfo();

        }
        private void CommandUpdateAll()
        {
            InitRepositoriesInfo();
        }
        private void CommandRenameTemplate(string newName, string? actName, string? actRepo)
        {
            // Retrieve missing information from loaded template
            if (actName == null || actRepo == null)
            {
                if (_template == null) throw new Exception($"No template loaded");
                else
                {
                    actName = _template.TemplateInfo.Name;
                    actRepo = _template.TemplateInfo.Repository?.Name;
                }
            }
            if (actName == null || actRepo == null) throw new Exception($"Failed to rename template - 1");

            // Retrieve path of actual and future template
            var actTemplatePath = _repositoriesInfo.First(_ => _.Name == actRepo).Templates.First(_ => _.Name == actName).Path;
            DirectoryInfo actTemplate = new(actTemplatePath);            
            if (actTemplate.Parent == null) throw new Exception($"Failed to rename template - 2");            
            var newTemplatePath = Path.Combine(actTemplate.Parent.FullName,newName);
            Directory.CreateDirectory(newTemplatePath);
            foreach (var directory in actTemplate.GetDirectories())
                Utilities.Copy(newTemplatePath,directory.FullName);
            foreach (var file in actTemplate.GetFiles())
                Utilities.Copy(newTemplatePath,file.FullName);     
             
            // Delete the old template
            Directory.Delete(actTemplatePath,true);            

            // Restore repository templates list
            _repositoriesInfo.First(_ => _.Name == actRepo).UpdateTemplatesInfo();
            
            // Rename the loaded template if available
            if (_template != null && _template.TemplateInfo.Name == actName && _template.TemplateInfo.Repository?.Name == actRepo)
                _template.Rename(new(_repositoriesInfo.First(_ => _.Name == actRepo),newName));
        }
        private void CommandRenameRepository(string newName, string actName)
        {
            if (_repositoriesInfo.Any(_ => _.Name == newName)) throw new Exception($"Repository name already used");

            var iniFile = new IniFile(_configFilePath);

            for (int i = 1; ; i++)
            {
                string tmpRepositoryString = iniFile.Read("Templates", $"Repository{i}");
                if (tmpRepositoryString.Split('|')[0] == actName )
                {
                    iniFile.DeleteKey("Templates",$"Repository{i}");                    
                    iniFile.Write("Templates", $"Repository{i}",$"{newName}|{tmpRepositoryString.Split('|')[1]}");
                    break;
                }
            }
            _repositoriesInfo.First(_ => _.Name == actName).Rename(newName);
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
            else if (commands.DeleteTemplateName != null && commands.DeleteTemplateRepo != null) CommandDeleteTemplate(commands.DeleteTemplateName,commands.DeleteTemplateRepo);         
            else if (commands.UpdateAll) CommandUpdateAll();  
            else if (commands.Exec && commands.ExecWorkingDir != null) ConsoleCommandExec(commands.ExecWorkingDir);          
            else if (commands.KeywordsList) ConsoleCommandKeywordsList();  
            else if (commands.KeywordInsertName != null) CommandKeywordInsert(commands.KeywordInsertName, commands.KeywordInsertValue); 
            else if (commands.KeywordsClean) CommandKeywordClean(); 
            else if (commands.RepositoriesList) ConsoleCommandRepositoriesList(); 
            else if (commands.RepositoryAdd != null && commands.RepositoryAddPath != null) CommandRepositoryAdd(commands.RepositoryAdd,commands.RepositoryAddPath); 
            else if (commands.RepositoryRemove != null) CommandRepositoryRemove(commands.RepositoryRemove); 
            else if (commands.RenameTemplateNewName != null) CommandRenameTemplate(commands.RenameTemplateNewName,commands.RenameTemplateActName,commands.RenameTemplateActRepo); 
            else if (commands.RenameRepoNewName != null && commands.RenameRepoActName != null) CommandRenameRepository(commands.RenameRepoNewName,commands.RenameRepoActName); 
           
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
            else if (commands.DeleteTemplateName != null && commands.DeleteTemplateRepo != null) CommandDeleteTemplate(commands.DeleteTemplateName,commands.DeleteTemplateRepo);         
            else if (commands.UpdateAll) CommandUpdateAll();  
            else if (commands.Exec && commands.ExecWorkingDir != null) UICommandExec(commands.ExecWorkingDir);    
            else if (commands.KeywordsList) UICommandKeywordsList(); 
            else if (commands.KeywordInsertName != null) CommandKeywordInsert(commands.KeywordInsertName, commands.KeywordInsertValue);    
            else if (commands.KeywordsClean) CommandKeywordClean(); 
            else if (commands.RepositoriesList) UICommandRepositoriesList();    
            else if (commands.RepositoryAdd != null && commands.RepositoryAddPath != null) CommandRepositoryAdd(commands.RepositoryAdd,commands.RepositoryAddPath); 
            else if (commands.RepositoryRemove != null) CommandRepositoryRemove(commands.RepositoryRemove); 
            else if (commands.RenameTemplateNewName != null) CommandRenameTemplate(commands.RenameTemplateNewName,commands.RenameTemplateActName,commands.RenameTemplateActRepo); 
            else if (commands.RenameRepoNewName != null && commands.RenameRepoActName != null) CommandRenameRepository(commands.RenameRepoNewName,commands.RenameRepoActName); 
                
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
        private TemplateInfo _templateInfo;
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
                                                        
                        string newDestPath = Utilities.Copy(instruction.Destination.Path,instruction.Source.Path);
                        if (_config.Keywords != null)
                            newDestPath = ReplaceKeywords(newDestPath,_config.Keywords);                                

                        if (instruction.Source.Type != null) // A type was specified -> Add xml element to descriptive file     
                        {                      
                            Utilities.AddDescriptiveXmlElement(newDestPath,instruction.Source.Type);

                            if (_config.Keywords != null)
                            {
                                string? descriptivePath = Path.GetDirectoryName(newDestPath);
                                if (descriptivePath != null)
                                    ReplaceKeywords(Utilities.GetASDescriptiveFile(descriptivePath).FullName,_config.Keywords);
                            }
                        }
                                                   
                        break;

                    case "Append":
                        if (instruction.Source == null || instruction.Destination == null) throw new Exception($"Append instruction failed! Invalid paths.");
                             
                        //Since destination file should already exists I have to replace keywords in it before proceed with append command
                        if (_config.Keywords != null)
                            foreach (var keyword in _config.Keywords)
                                instruction.Destination.Path = instruction.Destination.Path.Replace(keyword.ID,keyword.Value);                    
                        
                        Utilities.Append(instruction.Destination.Path,instruction.Source.Path);
                        
                        if (_config.Keywords != null)
                            ReplaceKeywords(instruction.Destination.Path,_config.Keywords);
                                                    
                        break;
                        
                    case "AddXmlElement":
                        if (instruction.Destination == null || instruction.XmlElements2Add == null) throw new Exception($"AddXmlElement instruction failed! Invalid path or Xml elements");
                        
                        Utilities.AddXmlElementsToFile(instruction.XmlElements2Add.XmlElements,instruction.XmlElements2Add.Path,instruction.Destination.Path);
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
                string projectPath = Utilities.GetASProjectPath(userPath);
                _configConstants["$PROJECT_PATH"] = projectPath;
                
                string activeConfig = Utilities.GetASActiveConfigurationName(projectPath);
                _configConstants["$ACTIVE_CONFIGURATION_PATH"] = Utilities.GetASActiveConfigurationPath(projectPath,activeConfig);
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
        public void Rename(TemplateInfo templateInfo)
        {
            _templateInfo = templateInfo; 
        }
    }

}