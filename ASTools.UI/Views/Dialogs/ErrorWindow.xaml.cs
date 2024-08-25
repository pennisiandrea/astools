using System.Windows;
using MahApps.Metro.Controls;

namespace ASTools.UI;

public partial class ErrorWindow : MetroWindow
{
    public bool OnScreen {get; private set;}
    public ErrorWindow()
    {
        InitializeComponent();
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
