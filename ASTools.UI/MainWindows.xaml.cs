using System.Windows.Navigation;
using MahApps.Metro.Controls;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASTools.UI
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            MainFrame.Navigated += PageChanged;
            this.NonActiveWindowTitleBrush = new SolidColorBrush(Colors.DarkOrange);
        }
        
        private void PageChanged(object sender, NavigationEventArgs e)
        {
            if (MainFrame.Content is Page currentPage)
                this.Title = currentPage.Title;
        }
        
    }
}