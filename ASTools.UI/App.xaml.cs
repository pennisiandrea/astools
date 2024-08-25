using System.Windows;
using System.Diagnostics;
using CommandLine;
using System.ComponentModel;
using MahApps.Metro.Controls.Dialogs;

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
    public static Process ASToolsProcess {get; private set;} = null!;
    private readonly ProcessStartInfo _astoolsProcessStartInfo = new()
    {
        FileName = "C:\\Data\\astools\\astools.core\\bin\\Debug\\net8.0\\ASTools.Core.exe",
        Arguments = "mode --ui",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }; 
    public static Command Arguments {get; private set;} = null!;
    private readonly Thread? _astoolsMonitorErrorsThread;
    
    private static ErrorWindow? _errorWindow;

    public App()
    {
        // Start ASTools.Core process
        ASToolsProcess = Process.Start(_astoolsProcessStartInfo) ?? throw new Exception($"Cannot start ASTools.Core process");
        
        // Start a thread to monitor ASTools.Core errors
        _astoolsMonitorErrorsThread = new(ASToolsMonitorErrors); 
        _astoolsMonitorErrorsThread.Start(); 
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
                        if (_errorWindow != null && _errorWindow.OnScreen) _errorWindow.AddMessage(errorLine);
                        else 
                        {
                            _errorWindow = new();
                            _errorWindow.AddMessage(errorLine);
                            _errorWindow.Show();
                        }
                    });
            }
        } while (!ASToolsProcess.HasExited);
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.Exit += new ExitEventHandler(App_Exit);
        
        Parser.Default.ParseArguments<Command>(e.Args)
                    .WithParsed<Command>(opts => {Arguments = opts;});

        OpenStartupPage();
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
}

