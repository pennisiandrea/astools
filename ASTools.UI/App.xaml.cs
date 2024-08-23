using System.Configuration;
using System.Data;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using CommandLine;
using System.Windows.Navigation;
using System.Windows.Controls;
using System;

namespace ASTools.UI;
class Command
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
    public Process? ASToolsProcess;
    public StreamWriter? ASToolsInputInterface;
    public readonly string ASToolsConsolePredefinedText = "astools > ";
    private readonly ProcessStartInfo _astoolsProcessStartInfo = new()
    {
        FileName = "C:\\Data\\astools\\astools.core\\bin\\Debug\\net8.0\\ASTools.Core.exe",
        Arguments = "",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.Exit += new ExitEventHandler(App_Exit);

        ASToolsStart();
        
        Parser.Default.ParseArguments<Command>(e.Args)
                    .WithParsed<Command>(opts => RunCommands(opts));

    }
    private static void RunCommands(Command opts)
    {
        // Manage startup page
        OpenStartupPage(opts);
    }
    private static void OpenStartupPage (Command opts)
    {
        MainWindow mainWindow = new();

        switch (opts.Page)
        {
            case "templates": 
                mainWindow.MainFrame.Navigate(new TemplatesPage(opts.WorkingDir));                    
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
        ASToolsInputInterface?.Close();

        ASToolsProcess?.Kill();
    }
    private void ASToolsStart()
    {
        try
        {
            ASToolsProcess = Process.Start(_astoolsProcessStartInfo);  
            if (ASToolsProcess != null)
                ASToolsInputInterface = ASToolsProcess.StandardInput;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
    }
}

