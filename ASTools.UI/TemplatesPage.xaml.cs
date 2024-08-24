using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ASTools.UI
{
    public class TemplateDataModel
    {
        public required string Name {get; set;}
        public required string Path {get; set;}
    }
    public class KeywordDataModel : INotifyPropertyChanged
    {
        public required string Keyword {get; set;}
        private string _value {get; set;} = string.Empty;
        public required string Value 
        {
            get { return _value;} 
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public partial class TemplatesPage : Page
    {
        public ObservableCollection<TemplateDataModel> TemplatesList { get; set; } = [];
        private TemplateDataModel? _selectedTemplate;
        public ObservableCollection<KeywordDataModel> KeywordsList { get; set; } = [];
        private readonly App? _thisApp = Application.Current as App;        
        private readonly string? _workingDir;
        public TemplatesPage()
        {
            InitializeComponent(); 
            templatesListGrid.ItemsSource = TemplatesList;  
            keywordsListGrid.ItemsSource = KeywordsList;   
            KeywordsList.CollectionChanged += KeywordsList_CollectionChanged;
        }
        public TemplatesPage(string? workingDir) : this()
        { 
            _workingDir = workingDir;
            workingDirectoryTextBox.Text = workingDir ?? "";
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            try
            {
                LoadTemplatesList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while executing command: {ex.Message}");
            }
        }
        public void LoadTemplatesList()
        {
            if (_thisApp == null) throw new Exception($"Missing reference to application");
            if (_thisApp.ASToolsProcess == null || _thisApp.ASToolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

            // Clear template list
            TemplatesList.Clear(); 

            // Clear pending data
            _thisApp.ASToolsProcess.StandardOutput.DiscardBufferedData();

            // Send command
            _thisApp.ASToolsInputInterface.WriteLine("templates --ui --list");

            // Read process output
            bool exit = false;
            do
            {
                string? line = _thisApp.ASToolsProcess.StandardOutput.ReadLine()?.Replace(_thisApp.ASToolsConsolePredefinedText,"");
                if (line == null || line == string.Empty) exit = true;
                else 
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3)
                        TemplatesList.Add(new TemplateDataModel { Name = parts[1].Trim(), Path = parts[2].Trim() });                        
                }
            } while(!exit);
            
            if (templatesListGrid.Items.Count > 0)
                templatesListGrid.SelectedIndex = 0;
        }
        private void LoadKeywordsList(string templatePath)
        {
            if (_thisApp == null) throw new Exception($"Missing reference to application");
            if (_thisApp.ASToolsProcess == null || _thisApp.ASToolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

            // Clear template list
            KeywordsList.Clear(); 

            // Clear pending data
            _thisApp.ASToolsProcess.StandardOutput.DiscardBufferedData();

            // Send command
            _thisApp.ASToolsInputInterface.WriteLine($"templates --ui --selected {templatePath} --k-list");

            // Read process output
            bool exit = false;
            do
            {
                string? line = _thisApp.ASToolsProcess.StandardOutput.ReadLine()?.Replace(_thisApp.ASToolsConsolePredefinedText,"");;
                if (line == null || line == string.Empty) exit = true;
                else 
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2)
                        KeywordsList.Add(new KeywordDataModel { Keyword = parts[0].Trim(), Value = parts[1].Trim() });                        
                }
            } while(!exit);
            
        }
        private void TemplatesListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (templatesListGrid.SelectedItem != null)
            {
                var selectedRow = templatesListGrid.SelectedItem;

                if (selectedRow is TemplateDataModel dataItem)
                {
                    _selectedTemplate = dataItem;
                    LoadKeywordsList(_selectedTemplate.Path);
                }
                else throw new Exception($"Error loading data from templates list");
            }
        }
        private void KeywordsList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Gestisci aggiunta di nuovi elementi
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is KeywordDataModel keyword)
                    {
                        keyword.PropertyChanged += Keyword_ValueChanged;
                    }
                }
            }  
            CheckExecuteButtonEnable();
        }
        private void Keyword_ValueChanged(object? sender, PropertyChangedEventArgs e)
        {
            CheckExecuteButtonEnable();
        }
        private void KeywordSendValueToASTools(TemplateDataModel template, KeywordDataModel keyword)
        {
            if (_thisApp == null) throw new Exception($"Missing reference to application");
            if (_thisApp.ASToolsProcess == null || _thisApp.ASToolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

            // Clear pending data
            _thisApp.ASToolsProcess.StandardOutput.DiscardBufferedData();

            // Send command
            _thisApp.ASToolsInputInterface.WriteLine($"templates --ui --selected \"{template.Path}\" --k-name \"{keyword.Keyword}\" --k-value \"{keyword.Value}\"");
            
            // Wait end of command
            bool exit = false;
            do
            {
                string? line = _thisApp.ASToolsProcess.StandardOutput.ReadLine()?.Replace(_thisApp.ASToolsConsolePredefinedText,"");
                if (line == null || line == string.Empty) exit = true;
            } while(!exit);                    
        }
        private void CheckExecuteButtonEnable()
        {
            executeButton.IsEnabled = KeywordsList.All(_ => _.Value != null && _.Value != string.Empty);
        }
        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTemplate == null) throw new Exception($"No template selected");
            foreach (var keyword in KeywordsList)
                KeywordSendValueToASTools(_selectedTemplate,keyword);

            if (_thisApp == null) throw new Exception($"Missing reference to application");
            if (_thisApp.ASToolsProcess == null || _thisApp.ASToolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

            // Clear pending data
            _thisApp.ASToolsProcess.StandardOutput.DiscardBufferedData();

            if (_workingDir == null) throw new Exception($"No working directory");
            // Send command
            _thisApp.ASToolsInputInterface.WriteLine($"templates --ui --selected \"{_selectedTemplate.Path}\" --exec --exec-working-dir \"{_workingDir}\"");
            
            // Wait end of command
            bool exit = false;
            do
            {
                string? line = _thisApp.ASToolsProcess.StandardOutput.ReadLine()?.Replace(_thisApp.ASToolsConsolePredefinedText,"");
                if (line == null || line == string.Empty) exit = true;
            } while(!exit);
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            TemplatesSettingsWindow mainWindow = new(this);
            mainWindow.Show();
        }
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage());
        }
        private void TemplatesListGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (templatesListGrid.Items.Count > 0)
                templatesListGrid.SelectedIndex = 0;            
        }
    }
}