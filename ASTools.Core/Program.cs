using ASTools.Core.Tools.Templates;
using CommandLine;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using ConsoleTables;
using System.Text;

namespace ASTools.Core
{
    class Command
    {
        [Verb("mode", HelpText = "Toggle presentation mode")]
        public class Mode
        { 
            [Option("ui", SetName = "command", Default = false, HelpText = "Run as UI")]
            public bool UI { get; set; }

            [Option("console", SetName = "command", Default = false, HelpText = "Run as console")]
            public bool Console { get; set; }
        }

        // Templates module
        [Verb("templates", HelpText = "Send command to Templates module")]
        public class Templates 
        {      
            [Option("selected", Default = null, HelpText = "Selected template")]
            public string? SelectedTemplate { get; set; }
            
            // Templates repositories commands
            [Option("repo-list", SetName = "command", Default = false, HelpText = "Print the list of templates repositories")]
            public bool RepositoriesList { get; set; }
            [Option("repo-add", SetName = "command", Default = null, HelpText = "Add a repository to templates repositories")]
            public string? RepositoryAdd { get; set; }
            [Option("repo-remove", SetName = "command", Default = null, HelpText = "Remove a repository from templates repositories")]
            public string? RepositoryRemove { get; set; }

            // Templates list command
            [Option("list", SetName = "command", Default = false, HelpText = "Print the list of templates")]
            public bool TemplatesList { get; set; }

            // Execute command
            [Option("exec", SetName = "command", Default = false, HelpText = "Execute a template")]
            public bool Exec { get; set; }
            [Option("exec-working-dir", Default = null, HelpText = "Working directory where execute the template")]
            public string? ExecWorkingDir { get; set; }

            // Keywords commands
            [Option("k-list", SetName = "command", Default = false, HelpText = "Print keywords list")]
            public bool KeywordsList { get; set; }
            [Option("k-name", SetName = "command", Default = null, HelpText = "Name of the keyword to insert")]
            public string? KeywordInsertName { get; set; }
            [Option("k-value", Default = null, HelpText = "Name of the keyword to insert")]
            public string? KeywordInsertValue { get; set; }

            // ToString override
            public override string ToString()
            {
                var str = "";

                if (TemplatesList) str += "\nTemplate list command";
                else if (Exec)
                {
                    str += $"\nExecute command";
                    str += $"\n\tWorking directory: {ExecWorkingDir}";
                    str += $"\n\tSelected template: {SelectedTemplate}";
                }
                else if (KeywordsList)
                {
                    str += "\nKeywords list command";
                    str += $"\n\tSelected template: {SelectedTemplate}";
                }
                else if (KeywordInsertName != null)
                {
                    str += "\nKeyword insert command";
                    str += $"\n\tSelected template: {SelectedTemplate}";
                    str += $"\n\tKeyword name: {KeywordInsertName}";
                    str += $"\n\tKeyword value: {KeywordInsertValue}";
                }

                return str;
            }
        }

        [Verb("exit", HelpText = "Exit command")]
        public class Exit
        { }
    
    }
    class IniFile(string path)
    {
        public string Path { get; } = path;

        // Metodo per scrivere una chiave in una sezione
        public void Write(string section, string? key, string? value)
        {
            _ = WritePrivateProfileString(section, key, value, Path);
        }

        // Metodo per leggere una chiave da una sezione
        public string Read(string section, string key)
        {
            var retVal = new StringBuilder(255);
            _ = GetPrivateProfileString(section, key, "", retVal, 255, Path);
            return retVal.ToString();
        }

        // Metodo per cancellare una chiave da una sezione
        public void DeleteKey(string section, string key)
        {
            Write(section, key, null);
        }

        // Metodo per cancellare una sezione
        public void DeleteSection(string section)
        {
            Write(section, null, null);
        }

        // Metodo per verificare se una chiave esiste in una sezione
        public bool KeyExists(string section, string key)
        {
            return Read(section, key).Length > 0;
        }

        // Funzioni esterne
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string? key, string? value, string filePath);
    }

    class Program
    {
        private const string InstallPathRegistryPath = "SOFTWARE\\ASTools\\";
        private const string InstallPathRegistryKey = "InstallPath";
        private const string ConfigFileName = "Config.ini";
        private const string EndOfCommandUI = ""; // The empty line is enough
        private static Templates? _templates;
        private static Template? _template;
        private static readonly string _configFilePath;
        private static bool _executionByUI = false;

        static Program()
        {
            _configFilePath = Path.Combine(GetConfigFilePath(),ConfigFileName);
        }

        static void Main(string[] argv)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new Exception($"This program be executed only on windows.");
            #pragma warning disable CA1416 // Validate platform compatibility

            // Runtime
            string? newCmd = null;
            List<string> listNewCmd = [.. argv];
            bool exit = false;
            while (!exit)
            {
                if (listNewCmd.Count > 0)
                {
                    Parser.Default.ParseArguments<Command.Mode,Command.Templates,Command.Exit>(listNewCmd)
                        .WithParsed<Command.Mode>(opts => ModeRunCommands(opts))
                        .WithParsed<Command.Templates>(opts => TemplatesRunCommands(opts))
                        .WithParsed<Command.Exit>(opts => {exit = true;})
                        .WithNotParsed(errors => HandleParseError(errors));
                }
                if (!exit)
                {
                    if (!_executionByUI)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("astools > ");
                    }
                    do
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        newCmd = Console.ReadLine();
                    } while (newCmd == null);

                    listNewCmd = newCmd.Split(' ')
                        .ToList()
                        .Select(_ => _.Trim('\"'))
                        .Select(_ => _.Replace("\\\\","\\"))
                        .ToList();  
                }
            }  
        }
        
        static int ModeRunCommands(Command.Mode opts)
        {   
            if(opts.UI) _executionByUI = true;
            else if(opts.Console) _executionByUI = false;

            return 0;
        }
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            if (errors != null){
                // todo
            };
        }
        private static string GetConfigFilePath()
        {
            string? configFilePath = null;
            RegistryKey? key = Registry.LocalMachine.OpenSubKey(InstallPathRegistryPath);
            if (key != null)
            {
                var keyValue = key.GetValue(InstallPathRegistryKey);
                if (keyValue == null || keyValue is not string) 
                    throw new Exception($"Invalid value on registry key {InstallPathRegistryPath}\\{InstallPathRegistryKey}");
                configFilePath = (string)keyValue;
            }
            if (configFilePath == null) throw new Exception($"Cannot retrieve configFilePath {InstallPathRegistryPath}\\{InstallPathRegistryPath}");
            return configFilePath;
        }

        // Templates - General
        static int TemplatesRunCommands(Command.Templates opts)
        {
            TemplatesCheckCommand(opts);
            
            if(_executionByUI) TemplatesUICommands(opts);
            else TemplatesConsoleCommands(opts);

            return 0;
        }
        static void TemplatesCheckCommand(Command.Templates opts)
        {
            // Check all invalid combinations of options

            // 1. 
            if (opts == null) throw new Exception($"Invalid options combination");

        }
        static void TemplatesCommandRepositoryAdd(string repository)
        {
            _templates ??= new(_configFilePath);
            
            _templates.AddRepository(repository); 
        }
        static void TemplatesCommandRepositoryRemove(string repository)
        {
            _templates ??= new(_configFilePath);
            _templates.RemoveRepository(repository);  
        }

        // Templates - Console
        static void TemplatesConsoleCommands(Command.Templates opts)
        {
            //Commands 
            if (opts.TemplatesList) TemplatesConsoleCommandTemplatesList();   
            else if (opts.Exec) TemplatesConsoleCommandExec(opts);          
            else if (opts.KeywordsList) TemplatesConsoleCommandKeywordsList(opts.SelectedTemplate);  
            else if (opts.KeywordInsertName != null) TemplatesConsoleCommandKeywordInsert(opts); 
            else if (opts.RepositoriesList) TemplatesConsoleCommandRepositoriesList(); 
            else if (opts.RepositoryAdd != null) TemplatesCommandRepositoryAdd(opts.RepositoryAdd); 
            else if (opts.RepositoryRemove != null) TemplatesCommandRepositoryRemove(opts.RepositoryRemove); 
            else Console.WriteLine("No command provided");       
        }
        static void TemplatesConsoleCommandTemplatesList()
        {
            _templates ??= new(_configFilePath);

            _templates.UpdateTemplatesList();
            
            TemplatesConsoleWriteTemplatesList(_templates.TemplatesList);              
        }
        static void TemplatesConsoleWriteTemplatesList(List<Templates.TemplateListItem> list)
        {
            var table = new ConsoleTable("ID", "Name", "Path");
            
            if (list != null)
                foreach (var template in list)
                    table.AddRow(template.ID,template.Name,template.Path);                     

            table.Write(Format.Minimal); 
        }
        static void TemplatesConsoleCommandRepositoriesList()
        {
            _templates ??= new(_configFilePath);  
            var table = new ConsoleTable("Path");
            
            if (_templates.Repositories != null)
                foreach (var repository in _templates.Repositories)
                    table.AddRow(repository);                     

            table.Write(Format.Minimal);              
        }
        static void TemplatesConsoleCommandExec(Command.Templates opts)
        {
            _templates ??= new(_configFilePath);

            if (opts.SelectedTemplate == null) // Ask user to select a template
            {
                _templates.UpdateTemplatesList();

                TemplatesConsoleWriteTemplatesList(_templates.TemplatesList);

                int selectedTemplateId;
                bool userInputValid;
                do
                {
                    Console.Write("Please, select a template by ID:");
                    string? userInput = Console.ReadLine();
                    userInputValid = int.TryParse(userInput,out selectedTemplateId);
                } while (!userInputValid || !_templates.TemplatesList.Any(_ => _.ID == selectedTemplateId));

                opts.SelectedTemplate = _templates.TemplatesList.First(_ => _.ID == selectedTemplateId).Path;
            }

            if (_template == null) _template = new(opts.SelectedTemplate);
            else if (_template.TemplatePath != opts.SelectedTemplate) _template.Init(opts.SelectedTemplate);

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

            if (!_template.KeywordsReady) throw new Exception($"{opts.SelectedTemplate} template still not ready");
            if (opts.ExecWorkingDir == null) throw new Exception($"No working path provided");

            _template.Execute(opts.ExecWorkingDir);            
        }
        static void TemplatesConsoleCommandKeywordsList(string? templatePath)
        {
            if (templatePath == null) throw new Exception($"No template path provided");
            if (_template == null) _template = new(templatePath);
            else if (_template.TemplatePath != templatePath) _template.Init(templatePath);
            
            var table = new ConsoleTable("ID", "Value");
            
            if (_template.Config.Keywords != null)
                foreach (var keyword in _template.Config.Keywords)
                    table.AddRow(keyword.ID,keyword.Value);                     

            table.Write(Format.Minimal);              
        }
        static void TemplatesConsoleCommandKeywordInsert(Command.Templates opts)
        {
            if (opts.SelectedTemplate == null) throw new Exception($"No template path provided");
            if (opts.KeywordInsertName == null) throw new Exception($"No keyword name provided");
            if (opts.KeywordInsertValue == null) throw new Exception($"No keyword value provided");
            if (_template == null) _template = new(opts.SelectedTemplate);
            else if (_template.TemplatePath != opts.SelectedTemplate) _template.Init(opts.SelectedTemplate);
                                                       
            _template.SetKeywordValue(new TemplateConfigClass.KeywordClass(){ID = opts.KeywordInsertName,Value = opts.KeywordInsertValue});               
        }
        
        // Templates - UI
        static void TemplatesUICommands(Command.Templates opts)
        {
            //Commands 
            if (opts.TemplatesList) TemplatesUICommandTemplatesList();   
            else if (opts.Exec) TemplatesUICommandExec(opts);    
            else if (opts.KeywordsList) TemplatesUICommandKeywordsList(opts.SelectedTemplate); 
            else if (opts.KeywordInsertName != null) TemplatesUICommandKeywordInsert(opts);   
            else if (opts.RepositoriesList) TemplatesUICommandRepositoriesList();    
            else if (opts.RepositoryAdd != null) TemplatesCommandRepositoryAdd(opts.RepositoryAdd); 
            else if (opts.RepositoryRemove != null) TemplatesCommandRepositoryRemove(opts.RepositoryRemove); 
            Console.WriteLine(EndOfCommandUI);            
        }
        static void TemplatesUICommandTemplatesList()
        {
            _templates ??= new(_configFilePath);

            _templates.UpdateTemplatesList();
            
            if (_templates.TemplatesList != null)
                foreach (var template in _templates.TemplatesList)
                    Console.WriteLine($"{template.ID}|{template.Name}|{template.Path}");     
        }
        static void TemplatesUICommandRepositoriesList()
        {
            _templates ??= new(_configFilePath);  
            
            foreach (var repository in _templates.Repositories)
                Console.WriteLine(repository);  
        }
        static void TemplatesUICommandExec(Command.Templates opts)
        {
            if (opts.SelectedTemplate == null) throw new Exception($"No template selected");
            
            if (_template == null) _template = new(opts.SelectedTemplate);
            else if (_template.TemplatePath != opts.SelectedTemplate) _template.Init(opts.SelectedTemplate);

            if (!_template.KeywordsReady) throw new Exception($"{opts.SelectedTemplate} template not ready");
            if (opts.ExecWorkingDir == null) throw new Exception($"No working path provided");

            _template.Execute(opts.ExecWorkingDir);                  
        }
        static void TemplatesUICommandKeywordsList(string? templatePath)
        {
            if (templatePath == null) throw new Exception($"No template path provided");
            if (_template == null) _template = new(templatePath);
            else if (_template.TemplatePath != templatePath) _template.Init(templatePath);
              
            if (_template.Config.Keywords != null)
                foreach (var keyword in _template.Config.Keywords)
                    Console.WriteLine($"{keyword.ID}|{keyword.Value}");                        
        }
        static void TemplatesUICommandKeywordInsert(Command.Templates opts)
        {
            if (opts.SelectedTemplate == null) throw new Exception($"No template path provided");
            if (opts.KeywordInsertName == null) throw new Exception($"No keyword name provided");
            if (opts.KeywordInsertValue == null) throw new Exception($"No keyword value provided");
            if (_template == null) _template = new(opts.SelectedTemplate);
            else if (_template.TemplatePath != opts.SelectedTemplate) _template.Init(opts.SelectedTemplate);
                                                       
            _template.SetKeywordValue(new TemplateConfigClass.KeywordClass(){ID = opts.KeywordInsertName,Value = opts.KeywordInsertValue});               
        }
    }
}
