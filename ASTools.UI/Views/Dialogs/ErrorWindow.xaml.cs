using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;

namespace ASTools.UI;

public partial class ErrorWindow : MetroWindow
{
    public bool OnScreen {get; private set;}
    public ErrorWindow()
    {
        InitializeComponent();
        ThemeManager.Current.ChangeTheme(this,App.ErrorTheme);
    }
    
    public void AddMessage(string error)
    {
        ErrorTextBlock.Text += $"\n{error}";
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {            
        OnScreen = true;       
    }
    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        OnScreen = false;
    }
    
}
