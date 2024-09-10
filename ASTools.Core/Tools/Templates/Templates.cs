using CommandLine;
using System.Xml;
using System.Xml.Serialization;
using ASTools.Library;
using ConsoleTables;
using System.Text.RegularExpressions;

namespace ASTools.Core.Tools.Templates
{    
    // Parsing options
    [Verb("templates", HelpText = "Send command to Templates module")]
    public class TemplatesOptions 
    {      
        [Option("load-template", Default = null, HelpText = "Load a very specific template providing the name of the template")]
        public string? LoadTemplate { get; set; }
        [Option("load-template-repo", Default = null, HelpText = "Load a very specific template providing the name of the repository")]
        public string? LoadTemplateRepo { get; set; }
        [Option("loaded", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Print the path of the loaded template")]
        public bool LoadedTemplate { get; set; }
        [Option("unload", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Unload the loaded template")]
        public bool UnloadTemplate { get; set; }

        [Option("update-all", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Update the list of repositories and templates")]
        public bool UpdateAll { get; set; }

        [Option("delete-name", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Delete a template - name")]
        public string? DeleteTemplateName { get; set; }
        [Option("delete-repo", Default = null, HelpText = "Delete a template - repository")]
        public string? DeleteTemplateRepo { get; set; }

        [Option("rename-template-new-name", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Rename a template - new name")]
        public string? RenameTemplateNewName { get; set; }
        [Option("rename-template-act-name", Default = null, HelpText = "Rename a template - actual name")]
        public string? RenameTemplateActName { get; set; }
        [Option("rename-template-act-repo", Default = null, HelpText = "Rename a template - actual repository")]
        public string? RenameTemplateActRepo { get; set; }
        [Option("rename-repo-new-name", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Rename a repository - new name")]
        public string? RenameRepoNewName { get; set; }
        [Option("rename-repo-act-name", Default = null, HelpText = "Rename a repository - actual name")]
        public string? RenameRepoActName { get; set; }
        
        // Templates repositories commands
        [Option("repo-list", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Print the list of templates repositories")]
        public bool RepositoriesList { get; set; }
        [Option("repo-add", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Name of the repository to add")]
        public string? RepositoryAdd { get; set; }
        [Option("repo-add-path", Default = null, HelpText = "Path of the Repository to add")]
        public string? RepositoryAddPath { get; set; }
        [Option("repo-remove", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Remove a repository by name")]
        public string? RepositoryRemove { get; set; }

        // Templates list command
        [Option("list", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Print the list of templates")]
        public bool TemplatesList { get; set; }

        // Execute command
        [Option("exec", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Execute a template")]
        public bool Exec { get; set; }
        [Option("exec-working-dir", Default = null, HelpText = "Working directory where execute the template")]
        public string? ExecWorkingDir { get; set; }

        // Keywords commands
        [Option("k-list", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Print keywords list")]
        public bool KeywordsList { get; set; }
        [Option("k-name", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Name of the keyword to insert")]
        public string? KeywordInsertName { get; set; }
        [Option("k-value", Default = null, HelpText = "Name of the keyword to insert")]
        public string? KeywordInsertValue { get; set; }
        [Option("k-clean", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Clean recorded keywords")]
        public bool KeywordsClean { get; set; }

    }

    // Config template file
    [XmlRoot("Template")]
    public class TemplateConfigClass
    {
        [XmlArray("Keywords")]
        [XmlArrayItem("Keyword")]
        public List<KeywordClass>? Keywords {get; set;}

        [XmlArray("SearchFunctions")]
        [XmlArrayItem("SearchFunction")]
        public List<Utilities.SearchFunctionClass>? SearchFunctions {get; set;}

        [XmlArray("Instructions")]
        [XmlArrayItem("Instruction")]
        public required List<InstructionClass> Instructions {get; set;}

        // Submembers
        public class KeywordClass
        {
            [XmlAttribute("ID")]
            public required string ID { get; set; }
            
            public string? Value { get; set; }
        }     
        public class InstructionClass
        {
            [XmlAttribute("Type")]
            public required string Type { get; set; }
            [XmlAttribute("ErrorAction")]
            public string? ErrorAction { get; set; }
            [XmlAttribute("SkipKeywordReplacement")]
            public bool SkipKeywordReplacement { get; set; }

            [XmlArray("Sources")]
            [XmlArrayItem("Source")]
            public List<SourceClass>? Sources { get; set; }
            [XmlArray("XmlElementsGroups")]
            [XmlArrayItem("XmlElementsGroup")]
            public List<XmlElementsGroupClass>? XmlElementsGroups { get; set; }

            [XmlArray("Destinations")]
            [XmlArrayItem("Destination")]
            public required List<DestinationClass>? Destinations { get; set; }

            public bool ErrorActionContinue => ErrorAction?.ToLower() == "continue";
            public bool ErrorActionIgnore => ErrorAction?.ToLower() == "ignore";
        }
        public class SourceClass
        {
            [XmlAttribute("Type")]
            public string? Type { get; set; }

            [XmlText]
            public required string Path { get; set; }
            
            [XmlAttribute("Overwrite")]
            public bool Overwrite { get; set; }
        }
        public class DestinationClass
        {
            [XmlText]
            public required string Path { get; set; }
        }
        public class XmlElementsGroupClass
        {
            [XmlAttribute("Path")]
            public required string Path { get; set; }

            [XmlAnyElement]
            public required XmlElement[] XmlElements { get; set; }
        }

    }
   
    // Info classes
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
    
    // Commands logic
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
        public void Execute(TemplatesOptions commands, bool executionByUI)
        {            
            CheckCommand(commands);
            
            if(executionByUI) UICommands(commands);
            else ConsoleCommands(commands);
        }
        private static void CheckCommand(TemplatesOptions commands)
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
            if (_repositoriesInfo.First(_ => _.Name == actRepo).Templates.Any(_ => _.Name == newName) ) throw new Exception($"Template name already used");
            if (!Utilities.IsFolderNameValid(newName)) throw new Exception($"This name cannot be used.");

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
            if (!Utilities.IsTextValidForIniValue(newName)) throw new Exception($"This name cannot be used.");

            newName = newName.Replace('|', ' '); // This char is reserved by

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
        private void ConsoleCommands(TemplatesOptions commands)
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
        private void UICommands(TemplatesOptions commands)
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

    // Template object
    public class Template
    {   
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
        public List<string> _compilationErrors = [];
        public List<string> CompilationErrors {get => _compilationErrors;}
        public List<string> _executionErrors = [];
        public List<string> ExecutionErrors {get => _executionErrors;}
        private bool _compilationResult;

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
            _executionErrors.Clear();

            if (!Directory.Exists(userPath)) throw new Exception($"Path {userPath} is not valid");

            // Execute instructions
            if (!Ready) throw new Exception($"Template not ready yet. Try set keywords before execute.");
                                
            // Compiling instructions
            CompileInstructions(userPath);

            // Execute instructions
            #pragma warning disable CS8602 // Null conditions checked in InstructionsCompile()
            foreach (var instruction in _config.Instructions)
            {
                try
                {
                    switch (instruction.Type)
                    {
                        case "Check":
                            // Compilation should have done all work.
                            break;

                        case "Copy":            
                            foreach (var source in instruction.Sources)
                            {
                                if (source.Overwrite)
                                {
                                    // Delete the file/folder if already exists in the destination.
                                    var newName = Path.GetFileName(source.Path);
                                    if (_config.Keywords != null && !instruction.SkipKeywordReplacement)
                                    {
                                        foreach (var keyword in _config.Keywords)
                                            newName = newName.Replace(keyword.ID,keyword.Value);
                                    }
                                    if (File.Exists(source.Path))
                                    {
                                        var dirFiles = Directory.GetFiles(instruction.Destinations[0].Path);
                                        foreach (var file in dirFiles)
                                        {
                                            if (Path.GetFileName(file) == newName || Path.GetFileName(file) == Path.GetFileName(source.Path)) 
                                                File.Delete(file);                                        
                                        }                                    
                                    }
                                    else if (Directory.Exists(source.Path))
                                    {
                                        var dirDirs = Directory.GetDirectories(instruction.Destinations[0].Path);
                                        foreach (var dir in dirDirs)
                                        {
                                            if (Path.GetFileName(dir) == newName || Path.GetFileName(dir) == Path.GetFileName(source.Path)) 
                                                Directory.Delete(dir,true);                                        
                                        }     
                                    }
                                }

                                // Execute copy
                                string newDestPath = Utilities.Copy(instruction.Destinations[0].Path,source.Path);
                                if (_config.Keywords != null && !instruction.SkipKeywordReplacement)
                                    newDestPath = ReplaceKeywords(newDestPath,_config.Keywords);                                

                                if (source.Type != null) // A type was specified -> Add xml element to descriptive file     
                                {                     
                                    Utilities.AddDescriptiveXmlElement(newDestPath,source.Type);                                

                                    if (_config.Keywords != null && !instruction.SkipKeywordReplacement)
                                    {
                                        string? descriptivePath = Path.GetDirectoryName(newDestPath);
                                        if (descriptivePath != null)
                                            ReplaceKeywords(Utilities.GetASDescriptiveFile(descriptivePath).FullName,_config.Keywords);
                                    }
                                }
                            }     
                                                    
                            break;

                        case "Append":     
                            foreach (var source in instruction.Sources)
                            {
                                Utilities.Append(instruction.Destinations[0].Path,source.Path);
                                
                                if (_config.Keywords != null && !instruction.SkipKeywordReplacement)
                                    ReplaceKeywords(instruction.Destinations[0].Path,_config.Keywords);
                            }                     
                            break;
                            
                        case "AddXmlElement":
                            foreach (var xmlElementsGroup in instruction.XmlElementsGroups)
                                Utilities.AddXmlElementsToFile(xmlElementsGroup.XmlElements,xmlElementsGroup.Path,instruction.Destinations[0].Path);
                            break;                
                    }
                }
                catch (System.Exception e)
                {
                    if (!instruction.ErrorActionContinue && !instruction.ErrorActionIgnore) throw;
                    _executionErrors.Add(e.Message);
                }  
                
            }
            #pragma warning restore CS8602

            string exitMessage = string.Empty;
            if (_compilationErrors.Count > 0)
            {
                exitMessage = "Compilation errors:\n";
                foreach (var error in _compilationErrors)
                    exitMessage += error + "\n";             
            }
            if (_executionErrors.Count > 0)
            {
                exitMessage = "Execution errors:\n";
                foreach (var error in _executionErrors)
                    exitMessage += error + "\n"; 
            }
            if (!string.IsNullOrEmpty(exitMessage))  throw new Exception (exitMessage);   
        }
        private void CompileInstructions(string userPath)
        {       
            _compilationResult = true;
            _compilationErrors = [];

            CompileCheckMissingData();

            CompileConstants(userPath);

            CompileKeywords();

            CompileFunctions();

            CompileCheckData();

            if (!_compilationResult)
            {
                var exeptionMessage = "Compilation errors:\n";
                foreach (var error in _compilationErrors)
                    exeptionMessage += error + "\n";      
                throw new Exception (exeptionMessage);          
            }
            
        }
        private void CompileCheckMissingData()
        {
            List<TemplateConfigClass.InstructionClass> instructionsToRemove = []; 

            // Check for errors in instructions            
            foreach (var instruction in _config.Instructions)
            {                     
                switch (instruction.Type)
                {
                    case "Check":
                        if (instruction.Destinations == null || instruction.Destinations.All(_ => string.IsNullOrEmpty(_.Path))) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Check error - Missing all destinations"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        else if (instruction.Destinations.Any(_ => string.IsNullOrEmpty(_.Path))) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Check error - Missing some sources");
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;   
                            instruction.Destinations.RemoveAll(_ => string.IsNullOrEmpty(_.Path)); // Remove only destinations with errors
                        }
                        
                        break;
                    
                    case "Copy":
                        if (instruction.Sources == null || instruction.Sources.All(_ => string.IsNullOrEmpty(_.Path))) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - Missing all sources"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        else if (instruction.Sources.Any(_ => string.IsNullOrEmpty(_.Path))) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - Missing some sources");
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;   
                            instruction.Sources.RemoveAll(_ => string.IsNullOrEmpty(_.Path)); // Remove only sources with errors
                        }
                        
                        if (instruction.Destinations == null || instruction.Destinations.Count != 1 || string.IsNullOrEmpty(instruction.Destinations[0].Path)) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - Missing destination"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        break;

                    case "Append":
                        if (instruction.Sources == null || instruction.Sources.All(_ => string.IsNullOrEmpty(_.Path))) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Append error - Missing all sources"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        else if (instruction.Sources.Any(_ => string.IsNullOrEmpty(_.Path))) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Append error - Missing some sources");
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;   
                            instruction.Sources.RemoveAll(_ => string.IsNullOrEmpty(_.Path)); // Remove only items with errors
                        }

                        if (instruction.Destinations == null || instruction.Destinations.Count != 1 || string.IsNullOrEmpty(instruction.Destinations[0].Path)) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Append error - Missing destination"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        break;

                    case "AddXmlElement":                        
                        if (instruction.Destinations == null || instruction.Destinations.Count != 1 || string.IsNullOrEmpty(instruction.Destinations[0].Path)) 
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} AddXmlElement error - Missing destination"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }

                        if (instruction.XmlElementsGroups == null || instruction.XmlElementsGroups.All(_ => string.IsNullOrEmpty(_.Path)))
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} AddXmlElement error - Missing all xml element groups or all paths");
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        else if (instruction.XmlElementsGroups.Any(_ => string.IsNullOrEmpty(_.Path)) || instruction.XmlElementsGroups.Any(_ => _.XmlElements.Length == 0))
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} AddXmlElement error - Missing some xml element groups or some paths");
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                            instruction.XmlElementsGroups.RemoveAll(_ => string.IsNullOrEmpty(_.Path) || _.XmlElements.Length == 0); // Remove only items with errors                  
                        }
                        break;                                  
                
                    default:                        
                        if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} - Missing or invalid type");
                        _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;
                        instructionsToRemove.Add(instruction);
                        break;
                }                
            }

            // Removing instructions with errors
            foreach (var instruction in instructionsToRemove)
                _config.Instructions.Remove(instruction);

        }
        private void CompileConstants(string userPath)
        {
            // Replace constants in searchfunctions and instructions
            CalculateConfigConstants(_templateInfo.Path, userPath);  
            if (_config.SearchFunctions != null)   
                foreach (var item in _config.SearchFunctions)
                {
                    if(!string.IsNullOrEmpty(item.TargetInnerText)) item.TargetInnerText = ReplaceConstants(item.TargetInnerText);
                    if(!string.IsNullOrEmpty(item.TargetInnerFileInnerText)) item.TargetInnerFileInnerText = ReplaceConstants(item.TargetInnerFileInnerText);
                    if(!string.IsNullOrEmpty(item.TargetInnerFile)) item.TargetInnerFile = ReplaceConstants(item.TargetInnerFile);                        
                }
            foreach (var instruction in _config.Instructions)
            {
                if (instruction.Destinations == null ) throw new Exception("Unexpected condition!"); // Virtually impossible. Should be catched by previous controls.
                 
                foreach (var destination in instruction.Destinations) destination.Path = ReplaceConstants(destination.Path);  
                if(instruction.Sources != null)
                    foreach (var source in instruction.Sources) source.Path = ReplaceConstants(source.Path);
                if(instruction.XmlElementsGroups != null)
                    foreach (var group in instruction.XmlElementsGroups) group.Path = ReplaceConstants(group.Path);                    
            }
        }
        private void CompileKeywords()
        {
            // Replace keywords on searchfunctions and instructions destination side
            if (_config.Keywords == null) return;
             
            if (_config.SearchFunctions != null)   
                foreach (var item in _config.SearchFunctions)
                {
                    foreach (var keyword in _config.Keywords)
                    {
                        if(!string.IsNullOrEmpty(item.TargetInnerText)) item.TargetInnerText = item.TargetInnerText.Replace(keyword.ID,keyword.Value);  
                        if(!string.IsNullOrEmpty(item.TargetInnerFileInnerText)) item.TargetInnerFileInnerText = item.TargetInnerFileInnerText.Replace(keyword.ID,keyword.Value);
                        if(!string.IsNullOrEmpty(item.TargetInnerFile)) item.TargetInnerFile = item.TargetInnerFile.Replace(keyword.ID,keyword.Value);      
                    }                  
                }
            foreach (var instruction in _config.Instructions)
            {
                if (instruction.SkipKeywordReplacement) continue;
                foreach (var keyword in _config.Keywords)
                {  
                    if (instruction.Destinations == null ) throw new Exception("Unexpected condition!"); // Virtually impossible. Should be catched by previous controls.
                 
                    foreach (var destination in instruction.Destinations)
                        destination.Path = destination.Path.Replace(keyword.ID,keyword.Value);  
                }                      
            }
            
        }
        private void CompileFunctions()
        {
            if (_config.SearchFunctions == null) return;
                
            List<TemplateConfigClass.InstructionClass> instructionsToRemove = [];
        
            // Replace search functions only in destinations
            string pattern = @"\$SEARCH_FUNCTION\((.*?)\)";                    
            Regex regex = new(pattern);

            foreach (var instruction in _config.Instructions)
            {      
                if (instruction.Destinations == null ) throw new Exception("Unexpected condition!"); // Virtually impossible. Should be catched by previous controls.
                 
                foreach (var destination in instruction.Destinations)
                {
                    Match match = regex.Match(destination.Path);

                    if (!match.Success) continue;
                    
                    string functionID = match.Groups[1].Value;

                    var searchFunction = _config.SearchFunctions.FirstOrDefault(_ => _?.ID == functionID,null);
                    if (searchFunction == null)
                    {
                        if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} - Search function not defined");
                        _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;
                        instructionsToRemove.Add(instruction);
                        continue;
                    }

                    switch (searchFunction.TargetType.ToLower())
                    {
                        case "file":
                            if(searchFunction.TargetExtension != null && !searchFunction.TargetExtension.StartsWith('.'))
                                searchFunction.TargetExtension = '.' + searchFunction.TargetExtension;
                            break;

                        case "package":
                            if(searchFunction.TargetInnerFileInnerText != null && string.IsNullOrEmpty(searchFunction.TargetInnerFile))
                            {                                    
                                if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} searchFunction {searchFunction.ID} error - TargetInnerFile is required if TargetInnerFileInnerText is used");
                                _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;
                                instructionsToRemove.Add(instruction);
                                continue;
                            }
                            break;

                        default:                       
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} searchFunction {searchFunction.ID} error - Missing or invalid TargetType");
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;
                            instructionsToRemove.Add(instruction);
                            continue;
                    }

                    var newPath = Utilities.SearchFunctionExecute(searchFunction,destination.Path);
                    if (string.IsNullOrEmpty(newPath))
                    {                 
                        if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} searchFunction {searchFunction.ID} error - Cannot resolve {destination.Path}");
                        _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;
                        instructionsToRemove.Add(instruction);
                        continue;
                    }
                    
                    // Optimization: Check if the same exact string is somewhere among all instructions, not only this one!
                    foreach (var thisInstruction in _config.Instructions)
                    {
                        if (thisInstruction.Destinations == null ) throw new Exception("Unexpected condition!"); // Virtually impossible. Should be catched by previous controls.
                
                        foreach (var thisDestination in thisInstruction.Destinations.Where(_ => _.Path == destination.Path))
                            thisDestination.Path = newPath;                        
                    } 
                }
            }

            // Removing instructions with errors
            foreach (var instruction in instructionsToRemove)
                _config.Instructions.Remove(instruction);
        }
        private void CompileCheckData()
        {
            List<TemplateConfigClass.InstructionClass> instructionsToRemove = [];
            List<TemplateConfigClass.SourceClass> sourcesToRemove = [];
            List<TemplateConfigClass.XmlElementsGroupClass> xmlElementsGroupsToRemove = [];

            // Check for errors in instructions            
            foreach (var instruction in _config.Instructions)
            {
                if (instruction.Sources == null) throw new Exception("Unexpected condition!"); // Virtually impossible. Should be catched by previous controls.
                if (instruction.Destinations == null || string.IsNullOrEmpty(instruction.Destinations[0].Path)) throw new Exception("Unexpected condition!"); // Virtually impossible. Should be catched by previous controls.
                         
                switch (instruction.Type)
                {
                    case "Check":
                        foreach (var destination in instruction.Destinations)
                        {                            
                            if(!Directory.Exists(destination.Path) && !File.Exists(destination.Path))
                            {
                                if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Check error - {destination.Path} must be an existing directory or file"); 
                                _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                                instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                                continue;
                            }
                        }
                        break;

                    case "Copy":    
                        // Destination path must be a directory
                        if(!Directory.Exists(instruction.Destinations[0].Path))
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - {instruction.Destinations[0].Path} must be an existing directory"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        
                        var destFiles = Directory.GetFiles(instruction.Destinations[0].Path);

                        sourcesToRemove.Clear();

                        foreach (var source in instruction.Sources)
                        {
                            if (instruction.Destinations == null || string.IsNullOrEmpty(instruction.Destinations[0].Path)) throw new Exception("Unexpected condition!"); // Virtually impossible. Should be catched by previous controls.
                
                            if (source.Type != null)
                            {
                                if (!Constants.AllowedTypes.Contains(source.Type.ToLower()))
                                {
                                    if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - Invalid type {source.Type}"); 
                                    _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                                    sourcesToRemove.Add(source);
                                    continue;
                                }                          
                            }
                               
                            if (File.Exists(source.Path))
                            {                                
                                // Destination should not contains a file with the same name
                                if (destFiles.Any(_ => Path.GetFileName(_) == Path.GetFileName(source.Path)))
                                {
                                    if (!source.Overwrite)
                                    {
                                        if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - The file {Path.GetFileName(source.Path)} already exists in {instruction.Destinations[0]?.Path}. Use Overwrite attribute to skip this error."); 
                                        _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                                        sourcesToRemove.Add(source);
                                        continue;
                                    }
                                }
                            } 
                            else 
                            {
                                // If source is not a file, it must be a directory
                                if (!Directory.Exists(source.Path)) 
                                {                                    
                                    if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - Invalid source {source.Path}");
                                    _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                                    sourcesToRemove.Add(source);
                                    continue;
                                }

                                // If source is a directory it must have a valid name for a directory
                                string newDestinationName = Path.GetFileName(source.Path);
                                if(_config.Keywords != null && !instruction.SkipKeywordReplacement) // Keywords in sources will be replaced after the copy execution, not at compile time!
                                {
                                    foreach (var keyword in _config.Keywords)
                                        newDestinationName = newDestinationName.Replace(keyword.ID,keyword.Value);
                                }
                                if (!Utilities.IsFolderNameValid(newDestinationName))
                                {                        
                                    if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - Invalid name for a package {newDestinationName}");
                                    _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                                    sourcesToRemove.Add(source);
                                    continue;
                                }

                                if (instruction.Destinations == null || string.IsNullOrEmpty(instruction.Destinations[0].Path)) continue; // Virtually impossible. Should be catched by previous controls.
                        
                                // If source is a directory, it should not exists
                                if (Directory.GetDirectories(instruction.Destinations[0].Path).Any(_ => Path.GetFileName(_) == newDestinationName))
                                {       
                                    if (!source.Overwrite)
                                    {  
                                        if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Copy error - The package {newDestinationName} already exists in {instruction.Destinations[0]?.Path}. Use Overwrite attribute to skip this error."); 
                                        _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore; 
                                        sourcesToRemove.Add(source);
                                        continue;
                                    }
                                }
                            }
                                                     
                        }
                        
                        // Removing sources with errors
                        foreach (var source in sourcesToRemove)
                            instruction.Sources.Remove(source);

                        if (instruction.Sources.Count == 0) instructionsToRemove.Add(instruction);
                              
                        break;

                    case "Append":
                        // Destination path must be a file
                        if(!File.Exists(instruction.Destinations[0].Path))
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Append error - {instruction.Destinations[0].Path} must be an existing file"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }

                        sourcesToRemove.Clear();

                        foreach (var source in instruction.Sources)
                        {
                            // source path must be a file!
                            if(!File.Exists(source.Path))
                            {
                                if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} Append error - {source.Path} must be an existing file"); 
                                _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                                sourcesToRemove.Add(source);
                                continue;
                            }
                        }

                        // Removing sources with errors
                        foreach (var source in sourcesToRemove)
                            instruction.Sources.Remove(source);

                        if (instruction.Sources.Count == 0) instructionsToRemove.Add(instruction);
                        
                        break;

                    case "AddXmlElement":                    
                        // Destination path must be a file
                        if(!File.Exists(instruction.Destinations[0].Path))
                        {
                            if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} AddXmlElement error - {instruction.Destinations[0].Path} must be an existing file"); 
                            _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                            instructionsToRemove.Add(instruction); // Serious error -> Remove the entire instruction
                            continue;
                        }
                        
                        if (instruction.XmlElementsGroups == null) throw new Exception("Unexpected condition!");  // Virtually impossible. Should be catched by previous controls.
                        
                        xmlElementsGroupsToRemove.Clear();

                        foreach (var xmlElementGroup in instruction.XmlElementsGroups)
                        {
                            if (instruction.Destinations[0] == null) throw new Exception("Unexpected condition!");  // Virtually impossible. Should be catched by previous controls.

                            xmlElementGroup.Path = Utilities.ConvertToNamespaceIndependentXPath(xmlElementGroup.Path);
                            if (!Utilities.IsXPathPresent(instruction.Destinations[0].Path,xmlElementGroup.Path))
                            {
                                if (!instruction.ErrorActionIgnore) _compilationErrors.Add($"Instruction {1+_config.Instructions.IndexOf(instruction)} XmlElementGroup error - Path {xmlElementGroup.Path} not found in {instruction.Destinations[0]?.Path}"); 
                                _compilationResult = _compilationResult && !instruction.ErrorActionContinue && !instruction.ErrorActionIgnore;                   
                                xmlElementsGroupsToRemove.Add(xmlElementGroup);
                                continue;
                            }
                        }

                        // Removing xmlElementGroups with errors
                        foreach (var xmlElementGroup in xmlElementsGroupsToRemove)
                            instruction.XmlElementsGroups.Remove(xmlElementGroup);

                        if (instruction.XmlElementsGroups.Count == 0) instructionsToRemove.Add(instruction);                        
                        
                        break;

                    default: 
                        throw new Exception("Unexpected condition!");  // Virtually impossible. Should be catched by previous controls.         
                }
            }
        
            // Removing instructions with errors
            foreach (var instruction in instructionsToRemove)
                _config.Instructions.Remove(instruction);
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
            _configConstants["$CONFIG_FILE_NAME"] = Constants.TemplateConfigFile;

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
            string configFileFullName = Path.Combine(templatePath,Constants.TemplateConfigFile);
            if (!File.Exists(configFileFullName)) throw new Exception($"{configFileFullName} file not found!");
            
            // Add attributes where required
            XmlAttributeOverrides overrides = new();
            // SearchFunctionClass -> ID
            XmlAttributes SearchFunctionID = new(){ XmlAttribute = new XmlAttributeAttribute("ID")};
            overrides.Add(typeof(Utilities.SearchFunctionClass), "ID", SearchFunctionID);

            XmlSerializer serializer = new(typeof(TemplateConfigClass),overrides);

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
                    if (directory.FullName != directoryName)
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