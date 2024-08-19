using System.Configuration;
using System.Data;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace ASTools.UI;

public partial class App : Application
{
    public Process? astoolsProcess;
    public StreamWriter? astoolsInputInterface;

    public App() : this(true){}
    private App(bool x) 
    {
        // Compose command
        var startInfo = new ProcessStartInfo
        {
            FileName = "C:\\Data\\astools\\astools.core\\bin\\Debug\\net8.0\\ASTools.Core.exe",
            Arguments = "",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        astoolsProcess = Process.Start(startInfo);  
        if (astoolsProcess != null)
            astoolsInputInterface = astoolsProcess.StandardInput;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        this.Exit += new ExitEventHandler(App_Exit);
    }
    private void App_Exit(object sender, ExitEventArgs e)
    {
        if (astoolsInputInterface != null)
            astoolsInputInterface.Close();

        if (astoolsProcess != null)
            astoolsProcess.Kill();
    }
}

