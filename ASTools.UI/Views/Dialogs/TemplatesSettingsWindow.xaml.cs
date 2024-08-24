using System.Collections.ObjectModel;
using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace ASTools.UI;

public partial class TemplatesSettingsWindow : MetroWindow
{
    public ObservableCollection<RepositoryDataModel> RepositoriesList { get; set; } = [];
    private readonly App? _thisApp = Application.Current as App;
    private readonly TemplatesPage _templatesPage;
    public TemplatesSettingsWindow(TemplatesPage templatesPage)
    {
        InitializeComponent();
        repositoriesListGrid.ItemsSource = RepositoriesList;
        _templatesPage = templatesPage;
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {            
        try
        {
            LoadRepositoriesList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error while executing command: {ex.Message}");
        }
    }
    private void LoadRepositoriesList()
    {
        if (_thisApp == null) throw new Exception($"Missing reference to application");
        if (_thisApp.ASToolsProcess == null || _thisApp.ASToolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

        // Clear template list
        RepositoriesList.Clear(); 

        // Clear pending data
        _thisApp.ASToolsProcess.StandardOutput.DiscardBufferedData();

        // Send command
        _thisApp.ASToolsInputInterface.WriteLine("templates --repo-list");

        // Read process output
        bool exit = false;
        do
        {
            string? line = _thisApp.ASToolsProcess.StandardOutput.ReadLine();
            if (line == null || line == string.Empty) exit = true;
            else RepositoriesList.Add(new RepositoryDataModel{ Path = line});            
        } while(!exit);
        
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _templatesPage.LoadTemplatesList();
    }
    private void RepositoriesListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (repositoriesListGrid.SelectedItem != null)
        {
            var selectedRow = repositoriesListGrid.SelectedItem;

            if (selectedRow is RepositoryDataModel dataItem)
                selectedRepositoryPath.Text = dataItem.Path;
            else throw new Exception($"Error loading data from repository list");
        }
    }
    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (repositoriesListGrid.SelectedItem != null)
        {
            var selectedRow = repositoriesListGrid.SelectedItem;

            if (selectedRow is RepositoryDataModel dataItem)
            {
                if (_thisApp == null) throw new Exception($"Missing reference to application");
                if (_thisApp.ASToolsProcess == null || _thisApp.ASToolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

                // Clear pending data
                _thisApp.ASToolsProcess.StandardOutput.DiscardBufferedData();

                // Send command
                _thisApp.ASToolsInputInterface.WriteLine($"templates --repo-remove {dataItem.Path}");

                // Wait end of command
                bool exit = false;
                do
                {
                    string? line = _thisApp.ASToolsProcess.StandardOutput.ReadLine();
                    if (line == null || line == string.Empty) exit = true;
                } while(!exit);

                LoadRepositoriesList();
                
                if (repositoriesListGrid.Items.Count > 0)
                    repositoriesListGrid.SelectedIndex = 0;
            }
            else throw new Exception($"Error loading data from repository list");
        }              
    }
    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_thisApp == null) throw new Exception($"Missing reference to application");
        if (_thisApp.ASToolsProcess == null || _thisApp.ASToolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

        // Clear pending data
        _thisApp.ASToolsProcess.StandardOutput.DiscardBufferedData();

        // Send command
        _thisApp.ASToolsInputInterface.WriteLine($"templates --repo-add {selectedRepositoryPath.Text}");

        // Wait end of command
        bool exit = false;
        do
        {
            string? line = _thisApp.ASToolsProcess.StandardOutput.ReadLine();
            if (line == null || line == string.Empty) exit = true;
        } while(!exit);
        
        LoadRepositoriesList();   
        
        if (repositoriesListGrid.Items.Count > 0)
            repositoriesListGrid.SelectedIndex = repositoriesListGrid.Items.Count - 1;         
    }
    private void RepositoriesListGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (repositoriesListGrid.Items.Count > 0)
            repositoriesListGrid.SelectedIndex = 0;            
    }

}
