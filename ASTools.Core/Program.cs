using ASTools.Core.Tools.Templates;
using CommandLine;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using ConsoleTables;

namespace ASTools.Core
{
    class Command
    {
        // Templates module
        [Verb("templates", HelpText = "Send command to Templates module")]
        public class Templates
        {            
            // General options
            [Option('u', "ui", Default = false, HelpText = "Print results as expected by UI")]
            public bool UI { get; set; }

            [Option('s', "selected-template", Default = null, HelpText = "Selected template")]
            public string? SelectedTemplate { get; set; }

            // Templates list command
            [Option('l', "templates-list", SetName = "command", Default = false, HelpText = "Print the list of templates")]
            public bool TemplatesList { get; set; }

            // Execute command
            [Option('e', "exec", SetName = "command", Default = false, HelpText = "Execute a template")]
            public bool Exec { get; set; }

            [Option('w', "exec-working-dir", Default = null, HelpText = "Working directory where execute the template")]
            public string? ExecWorkingDir { get; set; }

            // Keywords list command
            [Option('k', "keywords-list", SetName = "command", Default = false, HelpText = "Print keywords list")]
            public bool KeywordsList { get; set; }

            // Keyword add command
            [Option('i', "keyword-insert", SetName = "command", Default = false, HelpText = "Keyword insert command")]
            public bool KeywordInsert { get; set; }
            [Option('n', "keyword-insert-name", Default = null, HelpText = "Name of the keyword to insert")]
            public string? KeywordInsertName { get; set; }
            [Option('v', "keyword-insert-value", Default = null, HelpText = "Name of the keyword to insert")]
            public string? KeywordInsertValue { get; set; }

            // ToString override
            public override string ToString()
            {
                var str = "";
                
                if (UI) str += "\nUI format";
                else str += "\nConsole format";

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
                else if (KeywordInsert)
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
    class Program
    {
        private const string EndOfCommandUI = ""; // The empty line is enough
        private static Templates? _templates;
        private static Template? _template;

        static void Main(string[] argv)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new Exception($"This program be executed only on windows.");
            #pragma warning disable CA1416 // Validate platform compatibility

            string? newCmd = null;
            List<string> listNewCmd = argv.ToList();
            bool exit = false;

            while (!exit)
            {
                if (listNewCmd.Count() > 0)
                {
                    Parser.Default.ParseArguments<Command.Templates,Command.Exit>(listNewCmd)
                        .WithParsed<Command.Templates>(opts => TemplatesRunCommands(opts))
                        .WithParsed<Command.Exit>(opts => {exit = true;})
                        .WithNotParsed(errors => HandleParseError(errors));
                }
                if (!exit)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("astools > ");
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
        private static void HandleParseError(IEnumerable<Error> errors)
        {
        }

        // Templates - General
        static int TemplatesRunCommands(Command.Templates opts)
        {
            TemplatesCheckCommand(opts);

            if(opts.UI) TemplatesUICommands(opts);
            else TemplatesConsoleCommands(opts);

            return 0;
        }
        static void TemplatesCheckCommand(Command.Templates opts)
        {
            // Check all invalid combinations of options

            // 1. 
            if (false) throw new Exception($"Invalid options combination");

        }
        static string[] TemplatesRetrieveTemplatesDirFromRegistry()
        {
            string registryKeyPath = "SOFTWARE\\ASTools\\ASTemplates\\";
            string[] templatesPath = [];
            RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKeyPath);
            if (key != null)
            {
                var keyValue = key.GetValue("TemplatesPath");
                if (keyValue == null || keyValue is not string[]) throw new Exception($"Invalid value on registry key {registryKeyPath}\\TemplatesPath");
                templatesPath = (string[])keyValue;
            }
            if (templatesPath.Length == 0) throw new Exception($"No templates directory registered.");

            return templatesPath;
        }
        
        // Templates - Console
        static void TemplatesConsoleCommands(Command.Templates opts)
        {
            //Commands 
            if (opts.TemplatesList) TemplatesConsoleCommandTemplatesList();   
            else if (opts.Exec) TemplatesConsoleCommandExec(opts);          
            else if (opts.KeywordsList) TemplatesConsoleCommandKeywordsList(opts.SelectedTemplate);  
            else if (opts.KeywordInsert) TemplatesConsoleCommandKeywordInsert(opts); 
            else Console.WriteLine("No command provided");       
        }
        static void TemplatesConsoleCommandTemplatesList()
        {
            if (_templates == null) _templates = new();

            string[] templatesPath = TemplatesRetrieveTemplatesDirFromRegistry();
            
            _templates.UpdateTemplatesList(templatesPath);
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
        static void TemplatesConsoleCommandExec(Command.Templates opts)
        {
            if (_templates == null) _templates = new();

            string[] templatesPath = TemplatesRetrieveTemplatesDirFromRegistry();

            if (opts.SelectedTemplate == null) // Ask user to select a template
            {
                _templates.UpdateTemplatesList(templatesPath);

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
            
            TemplatesConsoleWriteKeywordsList(_template.Config.Keywords);                
        }
        static void TemplatesConsoleWriteKeywordsList(List<TemplateConfigClass.KeywordClass>? list)
        {
            var table = new ConsoleTable("ID", "Value");
            
            if (list != null)
                foreach (var keyword in list)
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
            else if (opts.KeywordInsert) TemplatesUICommandKeywordInsert(opts);      
            Console.WriteLine(EndOfCommandUI);            
        }
        static void TemplatesUICommandTemplatesList()
        {
            if (_templates == null) _templates = new();

            string[] templatesPath = TemplatesRetrieveTemplatesDirFromRegistry();
            
            _templates.UpdateTemplatesList(templatesPath);
            TemplatesUIWriteTemplatesList(_templates.TemplatesList);      
        }
        static void TemplatesUIWriteTemplatesList(List<Templates.TemplateListItem> list)
        {
            if (list != null)
                foreach (var template in list)
                    Console.WriteLine($"{template.ID}|{template.Name}|{template.Path}");     
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
            
            TemplatesUIWriteKeywordsList(_template.Config.Keywords);                           
        }
        static void TemplatesUIWriteKeywordsList(List<TemplateConfigClass.KeywordClass>? list)
        {
            if (list != null)
                foreach (var keyword in list)
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
