using ASTools.Core.Tools.Templates;
using CommandLine;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using ConsoleTables;
using System.Text;
using System.Text.RegularExpressions;

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
            [Option("load", Default = null, HelpText = "Load a very specific template providing the path")]
            public string? LoadTemplate { get; set; }
            [Option("loaded", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Print the path of the loaded template")]
            public bool LoadedTemplate { get; set; }
            [Option("unload", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Unload the loaded template")]
            public bool UnloadTemplate { get; set; }
            
            // Templates repositories commands
            [Option("repo-list", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Print the list of templates repositories")]
            public bool RepositoriesList { get; set; }
            [Option("repo-add", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Add a repository to templates repositories")]
            public string? RepositoryAdd { get; set; }
            [Option("repo-remove", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Remove a repository from templates repositories")]
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

            // ToString override
            public override string ToString()
            {
                var str = "";

                if (TemplatesList) str += "\nTemplate list command";
                else if (LoadedTemplate) str += "\nLoaded template command";
                else if (UnloadTemplate) str += "\nUnload template command";
                else if (Exec)
                {
                    str += $"\nExecute command";
                    str += $"\n\tWorking directory: {ExecWorkingDir}";
                }
                else if (KeywordsList)
                {
                    str += "\nKeywords list command";
                }
                else if (KeywordInsertName != null)
                {
                    str += "\nKeyword insert command";
                    str += $"\n\tKeyword name: {KeywordInsertName}";
                    str += $"\n\tKeyword value: {KeywordInsertValue}";
                }
                if (LoadTemplate != null) str += $"\nTemplate to load: {LoadTemplate}";

                return str;
            }
        }

        [Verb("exit", HelpText = "Exit command")]
        public class Exit
        { }
    
    }
    
    class IniFile(string path)
    {
        public string Path {get => path;}

        public void Write(string section, string? key, string? value)
        {
            _ = WritePrivateProfileString(section, key, value, Path);
        }

        public string Read(string section, string key)
        {
            var retVal = new StringBuilder(255);
            _ = GetPrivateProfileString(section, key, "", retVal, 255, Path);
            return retVal.ToString();
        }

        public void DeleteKey(string section, string key)
        {
            Write(section, key, null);
        }

        public void DeleteSection(string section)
        {
            Write(section, null, null);
        }

        public bool KeyExists(string section, string key)
        {
            return Read(section, key).Length > 0;
        }

        // Funzioni esterne
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetPrivateProfileString(
            string section,          
            string key,              
            string defaultValue,     
            StringBuilder retVal,    
            int size,                
            string filePath);        
        
        #pragma warning disable // Because the following code generates an useless advice
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WritePrivateProfileString(
            string section,          
            string? key,              
            string? value,            
            string filePath);        
        #pragma warning restore
    }

    partial class Program
    {
        private const string InstallPathRegistryPath = "SOFTWARE\\ASTools\\";
        private const string InstallPathRegistryKey = "InstallPath";
        private const string ConfigFileName = "Config.ini";
        private const string EndOfCommandUI = ""; // An empty line is enough
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
            string? newCmd;
            List<string> listNewCmd = [.. argv];
            while (true) // infinite loop
            {
                try
                {
                    if (listNewCmd.Count > 0)
                    {
                        Parser.Default.ParseArguments<Command.Mode,Command.Templates,Command.Exit>(listNewCmd)
                            .WithParsed<Command.Mode>(opts => ModeRunCommands(opts))
                            .WithParsed<Command.Templates>(opts => TemplatesRunCommands(opts))
                            .WithParsed<Command.Exit>(_ => Environment.Exit(0));
                    }
                    
                    if (!_executionByUI)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("astools > ");
                    }
                    do
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        newCmd = Console.ReadLine();
                    } while (string.IsNullOrEmpty(newCmd));

                    listNewCmd.Clear();

                    #pragma warning disable // Because the following code generates an useless advice
                    var matches = Regex.Matches(newCmd, "\"(.*?)\"|\\S+");
                    #pragma warning restore

                    foreach (Match match in matches.Cast<Match>())
                    {
                        // Se la parola è tra virgolette, rimuovi le virgolette
                        string word = match.Value.Trim('\"').Replace("\\\\","\\");
                        listNewCmd.Add(word);
                    }    
                }
                catch (Exception e)
                {                    
                    Console.Error.WriteLine(e.Message);
                    Console.WriteLine(EndOfCommandUI);
                    listNewCmd.Clear();
                }
            }  
        }
        
        static int ModeRunCommands(Command.Mode opts)
        {   
            if(opts.UI) _executionByUI = true;
            else if(opts.Console) _executionByUI = false;

            return 0;
        }
        private static string GetConfigFilePath()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new Exception($"This program can be executed only on windows.");
            
            string? configFilePath;
            RegistryKey? key = Registry.LocalMachine.OpenSubKey(InstallPathRegistryPath);
            if (key != null)
            {
                var keyValue = key.GetValue(InstallPathRegistryKey);
                if (keyValue == null || keyValue is not string) 
                    throw new Exception($"Invalid value on registry key {InstallPathRegistryPath}\\{InstallPathRegistryKey}");
                configFilePath = (string)keyValue;
            }
            else throw new Exception($"Registry path {InstallPathRegistryPath} not valid");
            if (configFilePath == null) throw new Exception($"Cannot retrieve .config file path {InstallPathRegistryPath}\\{InstallPathRegistryKey}");
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
        static void TemplatesCommandKeywordInsert(string name, string? value)
        {
            if (_template == null) throw new Exception($"No template loaded"); 
            if (name == null) throw new Exception($"No keyword name provided");
            if (value == null) throw new Exception($"No keyword value provided");
                                                       
            _template.SetKeywordValue(new TemplateConfigClass.KeywordClass(){ID = name,Value = value});               
        }
        static void TemplatesCommandKeywordClean()
        {
            if (_template == null) throw new Exception($"No template loaded");  
            
            _template.ResetKeywordsValues();            
        }
        static void TemplatesCommandLoadTemplate(string templatePath)
        {
            // Create a complete new instance of _template.
            try
            {
                _template = new(templatePath);
            }
            catch (Exception)
            {
                _template = null; // In case of error delete previously created _template instances
                throw;
            }
        }
        static void TemplatesCommandLoadedTemplate()
        {
            if (_template != null) Console.WriteLine($"{_template.TemplatePath}"); 
            else throw new Exception($"No template loaded");                
        }
        static void TemplatesCommandUnloadTemplate()
        {
            _template = null;
        }
        
        // Templates - Console
        static void TemplatesConsoleCommands(Command.Templates opts)
        {
            //Commands typ 1
            if (opts.LoadTemplate != null) TemplatesCommandLoadTemplate(opts.LoadTemplate);    

            //Commands typ 2
            if (opts.TemplatesList) TemplatesConsoleCommandTemplatesList();    
            else if (opts.LoadedTemplate) TemplatesCommandLoadedTemplate();   
            else if (opts.UnloadTemplate) TemplatesCommandUnloadTemplate();          
            else if (opts.Exec) TemplatesConsoleCommandExec(opts.ExecWorkingDir);          
            else if (opts.KeywordsList) TemplatesConsoleCommandKeywordsList();  
            else if (opts.KeywordInsertName != null) TemplatesCommandKeywordInsert(opts.KeywordInsertName, opts.KeywordInsertValue); 
            else if (opts.KeywordsClean) TemplatesCommandKeywordClean(); 
            else if (opts.RepositoriesList) TemplatesConsoleCommandRepositoriesList(); 
            else if (opts.RepositoryAdd != null) TemplatesCommandRepositoryAdd(opts.RepositoryAdd); 
            else if (opts.RepositoryRemove != null) TemplatesCommandRepositoryRemove(opts.RepositoryRemove); 
                  
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
        static void TemplatesConsoleCommandExec(string? execWorkingDir)
        {   
            if (execWorkingDir == null) throw new Exception($"No working path provided");

            if (_template == null) // If no template is loaded, present the list to the user and wait a choice
            {
                _templates ??= new(_configFilePath);

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

                var templatePath = _templates.TemplatesList.First(_ => _.ID == selectedTemplateId).Path;
                    
                _template = new(templatePath);
            }

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
        static void TemplatesConsoleCommandKeywordsList()
        {
            if (_template == null) throw new Exception($"No template loaded");  
            
            var table = new ConsoleTable("ID", "Value");
            
            if (_template.Config.Keywords != null)
                foreach (var keyword in _template.Config.Keywords)
                    table.AddRow(keyword.ID,keyword.Value);                     

            table.Write(Format.Minimal);              
        }
        
        // Templates - UI
        static void TemplatesUICommands(Command.Templates opts)
        {
            //Commands typ 1
            if (opts.LoadTemplate != null) TemplatesCommandLoadTemplate(opts.LoadTemplate);  

            //Commands typ 2
            if (opts.TemplatesList) TemplatesUICommandTemplatesList();   
            else if (opts.LoadedTemplate) TemplatesCommandLoadedTemplate();   
            else if (opts.UnloadTemplate) TemplatesCommandUnloadTemplate();   
            else if (opts.Exec) TemplatesUICommandExec(opts.ExecWorkingDir);    
            else if (opts.KeywordsList) TemplatesUICommandKeywordsList(); 
            else if (opts.KeywordInsertName != null) TemplatesCommandKeywordInsert(opts.KeywordInsertName, opts.KeywordInsertValue);    
            else if (opts.KeywordsClean) TemplatesCommandKeywordClean(); 
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
        static void TemplatesUICommandExec(string? execWorkingDir)
        {
            if (_template == null) throw new Exception($"No template loaded");  
            if (!_template.Ready) throw new Exception($"Template not ready to be installed");
            if (execWorkingDir == null) throw new Exception($"No working path provided");

            _template.Execute(execWorkingDir);  
            _template.ResetKeywordsValues(); // This prevents undesired repetitions                 
        }
        static void TemplatesUICommandKeywordsList()
        {
            if (_template == null) throw new Exception($"No template loaded"); 
              
            if (_template.Config.Keywords != null)
                foreach (var keyword in _template.Config.Keywords)
                    Console.WriteLine($"{keyword.ID}|{keyword.Value}");                        
        }

    }
}
