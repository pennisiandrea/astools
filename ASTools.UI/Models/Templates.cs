using System.ComponentModel;

namespace ASTools.UI;

public class RepositoryDataModel
{
    public required string Name {get; set;}
    public required string Path {get; set;}
}

public class TemplateDataModel
{
    public required string RepositoryName {get; set;}
    public required string Name {get; set;}
    public required string Path {get; set;}
}

public class KeywordDataModel
{
    public required string Keyword {get; set;}
    public string? Value {get; set;}
}
