using System.Collections.ObjectModel;
using MahApps.Metro.Controls;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;

namespace ASTools.UI;

public partial class TemplatesSettingsWindow : MetroWindow
{
    public ObservableCollection<RepositoryDataModel> RepositoriesList { get; set; } = [];
    private readonly TemplatesPage _templatesPage;
    private bool _repositoriesListChanged = false;
    
    public TemplatesSettingsWindow(TemplatesPage templatesPage)
    {
        InitializeComponent();
        repositoriesListGrid.ItemsSource = RepositoriesList;
        _templatesPage = templatesPage;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {            
        // Load repositories list at window open
        LoadRepositoriesList();        
    }
    private void LoadRepositoriesList()
    {
        // Send command
        var answer = App.ASToolsSendCommand("templates --repo-list");

        // Fill RepositoriesList with new values
        RepositoriesList.Clear(); 
        foreach (var line in answer)
        {
            var parts = line.Split('|'); // Each line is RepositoryName|RepositoryPath
            if (parts.Length >= 2)
                RepositoriesList.Add(new RepositoryDataModel { Name = parts[0].Trim(),Path = parts[1].Trim()});
        }

        // Select first element
        if (repositoriesListGrid.Items.Count > 0)
            repositoriesListGrid.SelectedIndex = 0;   
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // When closing the page, is any change happens, trigger the reload of templates list. 
        if (_repositoriesListChanged)
            _templatesPage.LoadTemplatesList();
    } 
    private void NewButton_Click(object sender, RoutedEventArgs e)
    {        
        // Send add command
        App.ASToolsSendCommand($"templates --repo-add \"{newRepositoryName.Text}\" --repo-add-path \"{newRepositoryPath.Text}\"");

        // Reload repositories list
        LoadRepositoriesList();   
        
        // Save repositories list changed flag
        _repositoriesListChanged = true;    
        
        // Select the last element of the list
        if (repositoriesListGrid.Items.Count > 0)
            repositoriesListGrid.SelectedIndex = repositoriesListGrid.Items.Count - 1;         
    }
    private void RepositoriesListGrid_OpenFolder_Click(object sender, RoutedEventArgs e)
    {        
        if (repositoriesListGrid.SelectedItem == null) return;
        
        var repository = (RepositoryDataModel)repositoriesListGrid.SelectedItem;
        
        if (!Directory.Exists(repository.Path)) throw new Exception($"Invalid repository path");

        Process.Start(new ProcessStartInfo
        {
            FileName = repository.Path,
            UseShellExecute = true,
            Verb = "open"
        });
                       
    }
    private void RepositoriesListGrid_Remove_Click(object sender, RoutedEventArgs e)
    {
        if (repositoriesListGrid.SelectedItem == null) return;
        
        // Get selected repository
        var dataItem = (RepositoryDataModel)repositoriesListGrid.SelectedItem;
        
        // Send remove command
        App.ASToolsSendCommand($"templates --repo-remove \"{dataItem.Name}\"");

        // Reload repositories list
        LoadRepositoriesList();  
        
        // Save repositories list changed flag
        _repositoriesListChanged = true;
    }
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog();

        if (dialog.ShowDialog() ?? false)
        {
            newRepositoryPath.Text = dialog.SelectedPath;
        }
    }
}
