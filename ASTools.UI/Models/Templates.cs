using System.ComponentModel;

namespace ASTools.UI;

public class RepositoryDataModel
{
    public required string Path {get; set;}
}

public class TemplateDataModel
{
    public required string Name {get; set;}
    public required string Path {get; set;}
}

public class KeywordDataModel : INotifyPropertyChanged
{
    public required string Keyword {get; set;}
    private string _value = string.Empty;
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
