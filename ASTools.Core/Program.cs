
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using ASTools.Library;

namespace ASTools.Core
{
    public class Command
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
            [Option("load-template", Default = null, HelpText = "Load a very specific template providing the name of the template")]
            public string? LoadTemplate { get; set; }
            [Option("load-template-repo", Default = null, HelpText = "Load a very specific template providing the name of the repository")]
            public string? LoadTemplateRepo { get; set; }
            [Option("loaded", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Print the path of the loaded template")]
            public bool LoadedTemplate { get; set; }
            [Option("unload", SetName = "mutual_exclusive_commands", Default = false, HelpText = "Unload the loaded template")]
            public bool UnloadTemplate { get; set; }

            [Option("delete-name", SetName = "mutual_exclusive_commands", Default = null, HelpText = "Delete a template - name")]
            public string? DeleteTemplateName { get; set; }
            [Option("delete-repo", Default = null, HelpText = "Delete a template - repository")]
            public string? DeleteTemplateRepo { get; set; }
            
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
        private const string EndOfCommandUI = ""; // An empty line is enough
        private static string _configFilePath = "";
        private static string _logErrorFilePath = Constants.LogErrorFilePathDefaultValue; // Default path. It will change after GetLogErrorFilePath();
        private static bool _executionByUI = false;

        static Program()
        {
            // Error handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        static void Main(string[] argv)
        {         
            _configFilePath = Utilities.GetStringKeyFromRegistry(Constants.RegistryMainPath,Constants.ConfigFileRegistryKey);
            _logErrorFilePath = Utilities.GetStringKeyFromRegistry(Constants.RegistryMainPath,Constants.LogErrorFileRegistryKey);

            Tools.Templates.Logic TemplatesLogic = new(_configFilePath); 

            string? newCmd;
            List<string> listNewCmd = [.. argv];
            while (true) // infinite loop
            {
                try
                {
                    // Parse & execute coming commands
                    if (listNewCmd.Count > 0)
                    {
                        var parsedCommands = Parser.Default.ParseArguments<Command.Mode,Command.Templates,Command.Exit>(listNewCmd)
                                                .MapResult(
                                                    (Command.Mode opts) => (object)opts,
                                                    (Command.Templates opts) => (object)opts,
                                                    (Command.Exit opts) => (object)opts,
                                                    errs => throw new Exception($"Error parsing command"));

                        if (parsedCommands is Command.Mode modeCommand)
                        {
                            ModeRunCommands(modeCommand);
                        }
                        else if(parsedCommands is Command.Templates templatesCommand)
                        {
                            TemplatesLogic.Execute(templatesCommand,_executionByUI);
                            if (_executionByUI) Console.WriteLine(EndOfCommandUI); // Handshake Core-UI "end of command"
                        }
                        else if(parsedCommands is Command.Exit exitCommand)
                        {
                            Environment.Exit(0);
                        }
                    }
                    
                    // Print the new line 
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

                    // Prepare new commands for the parsing on the next cycle
                    listNewCmd.Clear();
                    #pragma warning disable // Because the following code generates an useless advice
                    var matches = Regex.Matches(newCmd, "\"(.*?)\"|\\S+");
                    #pragma warning restore
                    foreach (Match match in matches.Cast<Match>())
                    {
                        // Remove escape chars and double \\
                        string word = match.Value.Trim('\"').Replace("\\\\","\\");
                        listNewCmd.Add(word);
                    }   
                     
                }
                catch (Exception e)
                {                    
                    LogException($"Core:{e.Message}",e.StackTrace);
                    Console.Error.WriteLine(e.Message);
                    Console.WriteLine(EndOfCommandUI);
                    listNewCmd.Clear();
                }
            }  
        }
        
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            LogException($"Core:{ex.Message}", ex.StackTrace);
        }
        private static void LogException(string message, string? stackTrace)
        {
            File.AppendAllText(_logErrorFilePath, $"{DateTime.Now}: {message}\n{stackTrace}\n");
        }
    
        static int ModeRunCommands(Command.Mode opts)
        {   
            if(opts.UI) _executionByUI = true;
            else if(opts.Console) _executionByUI = false;

            return 0;
        }
    
    }
}
