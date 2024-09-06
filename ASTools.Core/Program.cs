
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using ASTools.Library;
using ASTools.Core.Tools.Templates;

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
        private static string _logErrorFilePath = Constants.LogErrorFilePath; // Default path. It will change after GetLogErrorFilePath();
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

            Logic TemplatesLogic = new(_configFilePath); 

            string? newCmd;
            List<string> listNewCmd = [.. argv];
            while (true) // infinite loop
            {
                try
                {
                    // Parse & execute coming commands
                    if (listNewCmd.Count > 0)
                    {
                        var parsedCommands = Parser.Default.ParseArguments<Command.Mode,TemplatesOptions,Command.Exit>(listNewCmd)
                                                .MapResult(
                                                    (Command.Mode opts) => (object)opts,
                                                    (TemplatesOptions opts) => (object)opts,
                                                    (Command.Exit opts) => (object)opts,
                                                    errs => throw new Exception($"Error parsing command"));

                        if (parsedCommands is Command.Mode modeCommand)
                        {
                            ModeRunCommands(modeCommand);
                        }
                        else if(parsedCommands is TemplatesOptions templatesCommand)
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
