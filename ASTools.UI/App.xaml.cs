using System.Windows;
using System.Diagnostics;
using CommandLine;
using System.ComponentModel;
using System.Windows.Threading;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ASTools.UI;
public class Command
{
    // Startup page
    [Option("page", Default = null, HelpText = "Page to open")]
    public string? Page { get; set; }

    // Working directory
    [Option("working-dir", Default = null, HelpText = "Working directory")]
    public string? WorkingDir { get; set; }

    public override string ToString()
    {
        return $"page:{Page}\nworking-dir:{WorkingDir}";
    }
}

public class Error : INotifyPropertyChanged
{
    private string _text = string.Empty;
    public string Text 
    {
        get { return _text;} 
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class App : Application
{
    private const string RegistryMainPath = "SOFTWARE\\ASTools\\";
    private const string LogErrorFileRegistryKey = "LogError";
    private const string CorePathRegistryKey = "CorePath";
    public static Process ASToolsProcess {get; private set;} = null!;
    private readonly ProcessStartInfo _astoolsProcessStartInfo = new()
    {
        FileName = "",
        Arguments = "mode --ui",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }; 
    public static Command Arguments {get; private set;} = null!;
    private Thread? _astoolsMonitorErrorsThread;
    private static string _logErrorFilePath = "astools_error.log"; // Default path. It will change in OnStartup method
    private static ErrorWindow? _errorWindow;
    public static bool AppStarted {get; private set;}
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.Exit += new ExitEventHandler(App_Exit);
        this.DispatcherUnhandledException += Application_DispatcherUnhandledException;

        Thread appInitThread = new(() => 
        {
            // Retrieve logError file path from windows registry
            _logErrorFilePath = GetStringKeyFromRegistry(RegistryMainPath,LogErrorFileRegistryKey);

            // Error handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                        
            // Parse input arguments and open startup page
            Parser.Default.ParseArguments<Command>(e.Args)
                .WithParsed<Command>(opts => {Arguments = opts;});

        });
        appInitThread.Start();

        // Start ASTools.Core process
        Thread astoolsThread = new(() => 
        {
            // Retrieve corePath from windows registry
            _astoolsProcessStartInfo.FileName = GetStringKeyFromRegistry(RegistryMainPath,CorePathRegistryKey);

            ASToolsProcess = Process.Start(_astoolsProcessStartInfo) ?? throw new Exception($"Cannot start ASTools.Core process");
            
            // Start a thread to monitor ASTools.Core errors
            _astoolsMonitorErrorsThread = new(ASToolsMonitorErrors); 
            _astoolsMonitorErrorsThread.Start(); 

        });
        astoolsThread.Start();
        
        appInitThread.Join();
        astoolsThread.Join();  

        OpenStartupPage();  
        //ASToolsSendCommand("mode --ui");
        AppStarted = true;   
    }
    private static string GetStringKeyFromRegistry(string registryPath, string registryKey)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new Exception($"This program can be executed only on windows.");
        
        string? stringKeyValue;
        RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryPath);
        if (key != null)
        {
            var keyValue = key.GetValue(registryKey);
            if (keyValue == null || keyValue is not string) 
                throw new Exception($"Invalid value on registry key {registryPath}\\{registryKey}");
            stringKeyValue = (string)keyValue;
        }
        else throw new Exception($"Registry path {registryPath} not valid");
        if (stringKeyValue == null) throw new Exception($"Cannot retrieve value from registry: {registryPath}\\{registryKey}");
        return stringKeyValue;
    }
    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException($"UI:{e.Exception.Message}", e.Exception.StackTrace);
        e.Handled = true;
        SendErrorToErrorWindow(e.Exception.Message);
    }
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        LogException($"UI:{ex.Message}", ex.StackTrace);
        SendErrorToErrorWindow(ex.Message);
    }
    private static void LogException(string message, string? stackTrace)
    {
        File.AppendAllText(_logErrorFilePath, $"{DateTime.Now}: {message}\n{stackTrace}\n");
    }
    private static void OpenStartupPage ()
    {
        MainWindow mainWindow = new();

        switch (Arguments.Page)
        {
            case "templates": 
                mainWindow.MainFrame.Navigate(new TemplatesPage());            
                break;

            default:                
                mainWindow.MainFrame.Navigate(new MainPage());                   
                break;
        }

        mainWindow.Show();
    }
    private void App_Exit(object sender, ExitEventArgs e)
    {
        ASToolsClose();        
    } 
    private void ASToolsClose()
    {
        ASToolsProcess.StandardInput?.Close();

        ASToolsProcess?.Kill();

        _astoolsMonitorErrorsThread?.Join();
    }
    public static List<string> ASToolsSendCommand(string command)
    {
        // Clear pending data
        ASToolsProcess.StandardOutput.DiscardBufferedData();

        // Send command
        ASToolsProcess.StandardInput.WriteLine(command);

        // Read process output
        bool exit = false;
        List<string> answer = [];
        do
        {
            string? line = App.ASToolsProcess.StandardOutput.ReadLine();
            if (line == null || line == string.Empty) exit = true;
            else answer.Add(line);
        } while(!exit);
        
        return answer;
    }
    private static void ASToolsMonitorErrors()
    {
        do
        {
            string? errorLine = ASToolsProcess.StandardError.ReadLine();

            if (!string.IsNullOrEmpty(errorLine))
            {
                Application.Current.Dispatcher.Invoke(() => 
                {
                    SendErrorToErrorWindow(errorLine);
                });
            }
        } while (!ASToolsProcess.HasExited);
    }
    private static void SendErrorToErrorWindow(string errorMessage)
    {
        if (_errorWindow != null && _errorWindow.OnScreen) _errorWindow.AddMessage(errorMessage);
        else 
        {
            _errorWindow = new();
            _errorWindow.AddMessage(errorMessage);
            _errorWindow.Show();
        }
    }
    
}

