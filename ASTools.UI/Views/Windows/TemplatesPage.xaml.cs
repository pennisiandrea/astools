using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Ookii.Dialogs.Wpf;

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
        
        workingDirectoryTextBox.Text = App.Arguments.WorkingDir;

        // Load templates list at window opening
        RefreshTemplates();
        LoadTemplatesList();
    }
    private static void RefreshTemplates()
    {
        App.ASToolsSendCommand("templates --update-all");
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
        if (keyword.Value != null)
            App.ASToolsSendCommand($"templates --k-name \"{keyword.Keyword}\" --k-value \"{keyword.Value}\"");      
        else             
            App.ASToolsSendCommand($"templates --k-name \"{keyword.Keyword}\"");      
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

        templatesListGrid.Focus();
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
        if (templatesListGrid.SelectedItem == null) return;
        
        var template = (TemplateDataModel)templatesListGrid.SelectedItem;
        
        if (!Directory.Exists(template.Path)) throw new Exception($"Invalid repository path");

        Process.Start(new ProcessStartInfo
        {
            FileName = template.Path,
            UseShellExecute = true,
            Verb = "open"
        });    
    }
    private void TemplatesListGrid_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (templatesListGrid.SelectedItem == null) return;

        var template = (TemplateDataModel)templatesListGrid.SelectedItem;
        
        if (!Directory.Exists(template.Path)) throw new Exception($"Invalid template path");
        
        var result = MessageBox.Show("Are you sure? This operation cannot be undone.", 
                                    $"You are deleting {template.Name}.", 
                                    MessageBoxButton.YesNo, 
                                    MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            App.ASToolsSendCommand($"templates --delete-name \"{template.Name}\" --delete-repo \"{template.RepositoryName}\"");
            LoadTemplatesList();
        }   
    }
    private void KeywordsListGrid_Reset_Click(object sender, RoutedEventArgs e)
    {        
        if (keywordsListGrid.SelectedItem == null) return;
        
        var keyword = (KeywordDataModel)keywordsListGrid.SelectedItem;
        
        KeywordsList.First(_ => _.Keyword == keyword.Keyword).Value = null;
    }
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VistaFolderBrowserDialog();

        if (dialog.ShowDialog() ?? false)
        {
            workingDirectoryTextBox.Text = dialog.SelectedPath;
        }
        executeButton.Focus();
    }
    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        App.ASToolsSendCommand($"templates --rename-template-new-name \"{renameTextBox.Text}\"");
        LoadTemplatesList();
        templatesListGrid_ContextMenu.IsOpen = false; // The click on the rename button does't trigger the automatic closure of context menu
    }
    private void RenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // The visibility of the placeholder, in this specific case cannot be 
        // done via bindings directly on the xaml file due to namescope problems

        if (string.IsNullOrWhiteSpace(renameTextBox.Text))
        {
            placeholderRenameTextBox.Visibility = Visibility.Visible;
            renameButton.IsEnabled = false;
        }
        else
        {
            placeholderRenameTextBox.Visibility = Visibility.Collapsed;
            renameButton.IsEnabled = true;
        }
    }
    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(renameTextBox.Text))
        {
            renameButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
    }
    private void MenuItemRename_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        // Reset rename text box when opening it
        renameTextBox.Text = null;
        renameButton.IsEnabled = false;
    }
    private void KeywordsListGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            keywordsListGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            keywordsListGrid.CommitEdit(DataGridEditingUnit.Row, true);

            executeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
    }
    
}
