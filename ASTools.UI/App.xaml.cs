using System.Windows;
using System.Diagnostics;
using System.IO;
using CommandLine;

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
    
    public App()
    {
        ASToolsProcess = Process.Start(_astoolsProcessStartInfo) ?? throw new Exception($"Cannot start ASTools.Core process");  
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
    private static void ASToolsClose()
    {
        ASToolsProcess.StandardInput?.Close();

        ASToolsProcess?.Kill();
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

