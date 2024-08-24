using System.Windows.Navigation;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace ASTools.UI
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            MainFrame.Navigated += PageChanged;
        }
        private void PageChanged(object sender, NavigationEventArgs e)
        {
            if (MainFrame.Content is Page currentPage)
                this.Title = currentPage.Title;
            
        }
    }
}