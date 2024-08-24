using System.Windows;
using System.Windows.Navigation;
using System.Windows.Controls;

namespace ASTools.UI;

public partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }
    
    private void Button_Template_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(new TemplatesPage(null));
    }
}
