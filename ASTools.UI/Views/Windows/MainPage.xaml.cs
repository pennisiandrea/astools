using System.Windows;
using System.Windows.Navigation;
using System.Windows.Controls;

namespace ASTools.UI;

public partial class MainPage : Page
{
    private readonly Action<string?,bool> _setAppTheme;
    public MainPage(Action<string?,bool> SetAppTheme)
    {
        InitializeComponent(); 
        _setAppTheme = SetAppTheme;
    }
    
    private void Button_Template_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new TemplatesPage(_setAppTheme));
    }
}
