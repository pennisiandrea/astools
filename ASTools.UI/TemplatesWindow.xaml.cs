using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using System.Printing;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class TemplatesWindow : Window
    {
        public ObservableCollection<TemplateDataModel> TemplatesList { get; set; } = [];
        public ObservableCollection<KeywordDataModel> KeywordsList { get; set; } = [];
        private readonly ASTools.UI.App? ThisApp = Application.Current as App;

        public TemplatesWindow()
        {
            InitializeComponent();
            templatesListGrid.ItemsSource = TemplatesList;  
            keywordsListGrid.ItemsSource = KeywordsList;     

            KeywordsList.CollectionChanged += KeywordsList_CollectionChanged;
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
        private void LoadTemplatesList()
        {
            if (ThisApp == null) throw new Exception($"Missing reference to application");
            if (ThisApp.astoolsProcess == null || ThisApp.astoolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

            // Clear template list
            TemplatesList.Clear(); 

            // Clear pending data
            ThisApp.astoolsProcess.StandardOutput.DiscardBufferedData();

            // Send command
            ThisApp.astoolsInputInterface.WriteLine("templates --ui --list");

            // Read process output
            bool exit = false;
            do
            {
                string? line = ThisApp.astoolsProcess.StandardOutput.ReadLine();
                if (line == null || line == string.Empty) exit = true;
                else 
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3)
                        TemplatesList.Add(new TemplateDataModel { Name = parts[1].Trim(), Path = parts[2].Trim() });                        
                }
            } while(!exit);
            
        }
        private void LoadKeywordsList(string templatePath)
        {
            if (ThisApp == null) throw new Exception($"Missing reference to application");
            if (ThisApp.astoolsProcess == null || ThisApp.astoolsInputInterface == null) throw new Exception($"Missing reference to ASTools process");

            // Clear template list
            KeywordsList.Clear(); 

            // Clear pending data
            ThisApp.astoolsProcess.StandardOutput.DiscardBufferedData();

            // Send command
            ThisApp.astoolsInputInterface.WriteLine($"templates --ui --selected {templatePath} --k-list");

            // Read process output
            bool exit = false;
            do
            {
                string? line = ThisApp.astoolsProcess.StandardOutput.ReadLine();
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
                    LoadKeywordsList(dataItem.Path);
                else throw new Exception($"Error loading data from templates list");
            }
        }
        private void KeywordsList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Gestisci aggiunta di nuovi elementi
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
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
        private void Keyword_ValueChanged(object sender, PropertyChangedEventArgs e)
        {
            CheckExecuteButtonEnable();
        }
        private void CheckExecuteButtonEnable()
        {
            executeButton.IsEnabled = KeywordsList.All(_ => _.Value != null && _.Value != string.Empty);
        }
        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pulsante cliccato!");
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pulsante cliccato!");
        }
    }
}