using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using ASTools.Library;

namespace ASTools.UI;

public partial class TemplatesPage : Page
{
    public ObservableCollection<TemplateDataModel> TemplatesList { get; set; } = [];
    public ObservableCollection<KeywordDataModel> KeywordsList { get; set; } = [];       
    
    public TemplatesPage()
    {
        InitializeComponent(); 

        templatesListGrid.ItemsSource = TemplatesList;  
        keywordsListGrid.ItemsSource = KeywordsList;   
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {    
        var i = 0;
        while(!App.AppStarted) 
        { 
            Thread.Sleep(50);
            if (i>100) throw new Exception($"App non started");
        }
        
        workingDirectoryTextBox.Text = App.Arguments.WorkingDir ?? "Insert a valid path here!";

        // Load templates list at window opening
        LoadTemplatesList();
    }
    public void LoadTemplatesList()
    {
        // Send command 
        var answer = App.ASToolsSendCommand("templates --list");

        // Fill TemplatesList with new values
        TemplatesList.Clear(); 
        foreach (var line in answer)
        {
            var parts = line.Split('|'); // Each line is RepositoryName|TemplateName|TemplatePath
            if (parts.Length >= 3)
                TemplatesList.Add(new TemplateDataModel { RepositoryName = parts[0].Trim(),Name = parts[1].Trim(), Path = parts[2].Trim() });
        }
        
        // Select first element in the grid
        if (templatesListGrid.Items.Count > 0)
            templatesListGrid.SelectedIndex = 0;
        else
            KeywordsList.Clear(); 
    }
    private void LoadTemplate(string repositoryName, string templateName)
    {        
        // Load template
        App.ASToolsSendCommand($"templates --load-template \"{templateName}\" --load-template-repo \"{repositoryName}\"");
           
        // Asks for keywords list
        var answer = App.ASToolsSendCommand($"templates --k-list");

        // Fill KeywordsList with new values
        KeywordsList.Clear(); 
        foreach (var line in answer)
        {
            var parts = line.Split('|');
            if (parts.Length == 2)
                KeywordsList.Add(new KeywordDataModel { Keyword = parts[0].Trim(), Value = parts[1].Trim() });  
        }   
    }
    private void TemplatesListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (templatesListGrid.SelectedItem == null) return;
        
        // Save loaded template for future uses
        var loadedTemplate = (TemplateDataModel)templatesListGrid.SelectedItem;

        // Load selected template
        LoadTemplate(loadedTemplate.RepositoryName,loadedTemplate.Name);    
    }
    private static void KeywordSendValueToASTools(KeywordDataModel keyword)
    {
        // Send command
        App.ASToolsSendCommand($"templates --k-name \"{keyword.Keyword}\" --k-value \"{keyword.Value}\"");                   
    }
    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        // Send keywords
        foreach (var keyword in KeywordsList)
            KeywordSendValueToASTools(keyword);
        
        // Send execute
        App.ASToolsSendCommand($"templates --exec --exec-working-dir \"{workingDirectoryTextBox.Text}\"");                   
        
        // Reload the template     
        if (templatesListGrid.SelectedItem == null) return;
        
        // Save loaded template for future uses
        var loadedTemplate = (TemplateDataModel)templatesListGrid.SelectedItem;

        // Load selected template
        LoadTemplate(loadedTemplate.RepositoryName,loadedTemplate.Name);
    }
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Open settings page as a dialog modal
        TemplatesSettingsWindow mainWindow = new(this);
        mainWindow.ShowDialog();
    }
    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        // Go back to main page
        NavigationService.Navigate(new MainPage());
    }
    private void TemplatesListGrid_Loaded(object sender, RoutedEventArgs e)
    {          
        // Templates list loaded -> select the first element
        if (templatesListGrid.Items.Count > 0)
            templatesListGrid.SelectedIndex = 0; 
    } 
    private void TemplatesListGrid_OpenFolder_Click(object sender, RoutedEventArgs e)
    {        
        if (templatesListGrid.SelectedItem != null)
        {
            var loadedTemplate = (TemplateDataModel)templatesListGrid.SelectedItem;
            
            if (!string.IsNullOrEmpty(loadedTemplate.Path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = loadedTemplate.Path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }        
    }
    private void TemplatesListGrid_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (templatesListGrid.SelectedItem != null)
        {
            var loadedTemplate = (TemplateDataModel)templatesListGrid.SelectedItem;
            
            if (!string.IsNullOrEmpty(loadedTemplate.Path))
            {
                var result = MessageBox.Show("Are you sure? This oepration cannot be undone.", 
                                            $"You are deleting {loadedTemplate.Name}.", 
                                            MessageBoxButton.YesNo, 
                                            MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    App.ASToolsSendCommand($"templates --delete-name \"{loadedTemplate.Name}\" --delete-repo \"{loadedTemplate.RepositoryName}\"");
                    LoadTemplatesList();
                }
            }
        }
    }
    
}
