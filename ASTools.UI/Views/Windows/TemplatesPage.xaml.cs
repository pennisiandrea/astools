using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Collections.Specialized;
using System.ComponentModel;

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
        workingDirectoryTextBox.Text = App.Arguments.WorkingDir ?? "";

        KeywordsList.CollectionChanged += KeywordsList_CollectionChanged;
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {    
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
            var parts = line.Split('|');
            if (parts.Length == 3)
                TemplatesList.Add(new TemplateDataModel { Name = parts[1].Trim(), Path = parts[2].Trim() });
        }
        
        // Select first element in the grid
        if (templatesListGrid.Items.Count > 0)
            templatesListGrid.SelectedIndex = 0;
    }
    private static void LoadTemplate(string templatePath)
    {        
        // Send command
        App.ASToolsSendCommand($"templates --load \"{templatePath}\"");
    }
    private void LoadKeywordsList()
    {        
        // Send command
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
        LoadTemplate(loadedTemplate.Path);

        // Load keywords
        LoadKeywordsList();        
    }
    private void KeywordsList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null) return;
        
        // Link each keyword in the list to the ValueChanged event. This is used to trigger the change value event
        foreach (var newItem in e.NewItems)
        {
            if (newItem is KeywordDataModel keyword)
                keyword.PropertyChanged += Keyword_ValueChanged;            
        }
        
        // Check if enabling Execute button
        CheckExecuteButtonEnable();
    }
    private void Keyword_ValueChanged(object? sender, PropertyChangedEventArgs e)
    {
        // A value is changed in a keyword. Check if is possible to enable Execute button
        CheckExecuteButtonEnable();
    }
    private static void KeywordSendValueToASTools(KeywordDataModel keyword)
    {
        // Send command
        App.ASToolsSendCommand($"templates --k-name \"{keyword.Keyword}\" --k-value \"{keyword.Value}\"");                   
    }
    private void CheckExecuteButtonEnable()
    {
        // Execute button is enabled only if all keywords are filled.
        executeButton.IsEnabled = KeywordsList.All(_ => _.Value != null && _.Value != string.Empty)
                                    && !string.IsNullOrEmpty(workingDirectoryTextBox.Text);
    }
    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        // Send keywords
        foreach (var keyword in KeywordsList)
            KeywordSendValueToASTools(keyword);
        
        // Send execute
        App.ASToolsSendCommand($"templates --exec --exec-working-dir \"{workingDirectoryTextBox.Text}\"");                   
        
        // Keywords are automatically resetted by ASTools.Core process                
        foreach (var keyword in KeywordsList)
            keyword.Value = string.Empty;
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
    
}
