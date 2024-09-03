using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.ObjectModel;

namespace ASTools.Library;

public static class Utilities
{
    public static string GetStringKeyFromRegistry(string registryPath, string registryKey)
    {
        /* 
        This method retrieve a key from the windows registry and returns its value.
        */
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new Exception($"This method can be executed only on windows.");
        
        string? stringKeyValue;
        RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryPath);
        if (key != null)
        {
            var keyValue = key.GetValue(registryKey);
            if (keyValue == null || keyValue is not string) 
                throw new Exception($"Invalid value on registry key {registryPath}\\{registryKey}");
            stringKeyValue = (string)keyValue;
        }
        else throw new Exception($"Registry path {registryPath} not valid");
        if (stringKeyValue == null) throw new Exception($"Cannot retrieve value from registry: {registryPath}\\{registryKey}");
        return stringKeyValue;
    }
    
    public static string Copy(string destPath, string sourcePath)
    {
        /* 
        This method copy whatever is the object in sourcePath to the destPath.
        Source path can be a single file or a folder. If source path is a folder all files and subfolder are copied.
        */
        string copiedPath;

        if (File.Exists(sourcePath)) // Copying a single file
        {
            FileInfo file = new(sourcePath);
            copiedPath = Path.Combine(destPath,file.Name);
            file.CopyTo(copiedPath);
        }
        else if (Directory.Exists(sourcePath)) // Copying a folder
        {                
            DirectoryInfo sourceDir = new(sourcePath);
            DirectoryInfo destDir = new(destPath);

            // Check if in destPath exists a directory like sourcePath. This sould be true only the first time
            if (sourceDir.Name != destDir.Name)
            {
                destPath = Path.Combine(destPath,sourceDir.Name);
                Directory.CreateDirectory(destPath);
            }
            copiedPath = destPath;

            // Copy all files
            FileInfo[] sourceFiles = sourceDir.GetFiles();
            foreach (FileInfo file in sourceFiles) 
                file.CopyTo(Path.Combine(destPath,file.Name));
            
            // Copy all folders
            DirectoryInfo[] directories = sourceDir.GetDirectories();
            foreach (DirectoryInfo directory in directories)
            {
                string newDestPath = Path.Combine(destPath,directory.Name);
                Directory.CreateDirectory(newDestPath);                
                Copy(newDestPath, directory.FullName);
            }             
        }
        else throw new Exception($"Invalid path to copy: {sourcePath}");

        return copiedPath;
    }
    
    public static void Append(string destPath, string sourcePath)
    {
        /* 
        This method append the content of the file in sourcePath to the end of the file in destPath.
        */
        if (!File.Exists(destPath) || !File.Exists(sourcePath)) throw new Exception($"Failed to append {sourcePath} to {destPath}");
        
        string sourceContent = File.ReadAllText(sourcePath);
        string destContent = File.ReadAllText(destPath);

        File.WriteAllText(destPath,destContent + "\n" + sourceContent);
    }

    public static string GetASProjectPath(string path)
    {
        /* 
        This method retrieve the main Automation Studio project path
        */
        DirectoryInfo actDir = new(path);
        FileInfo[] files = actDir.GetFiles();            
        if (files.Any(file => file.Extension == ".apj")) return actDir.FullName;
        else 
        {
            if (actDir.Parent != null) return GetASProjectPath(actDir.Parent.FullName);
            else throw new Exception($"Automation Studio project not found.");
        }
    }

    public static string GetASActiveConfigurationName(string projectPath)
    {
        /* 
        This method retrieve the Automation Studio project active configuration name
        */

        string lastUserFile = Path.Combine(projectPath,"LastUser.set");
        if (!File.Exists(lastUserFile)) throw new Exception($"Cannot find file {lastUserFile}");

        // Load xml document from file
        XmlDocument xmlDoc = new();
        xmlDoc.Load(lastUserFile);

        // Extract namespace
        XmlNamespaceManager nsmgr = new(xmlDoc.NameTable);
        if (xmlDoc.DocumentElement != null)
        {
            string namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
            nsmgr.AddNamespace("ns", namespaceUri);
        } 
    
        // Find the searched element 
        XmlNode? node = xmlDoc.SelectSingleNode("/ns:ProjectSettings/ns:ConfigurationManager",nsmgr);

        if (node is XmlElement element)
        {
            var attribute = element.Attributes["ActiveConfigurationName"];
            if (attribute != null) return attribute.Value;  
            else throw new Exception($"{lastUserFile} ActiveConfigurationName attribute not found");     
        }            
        else throw new Exception($"{lastUserFile} file structure not supported");
    }
    
    public static string GetASActiveConfigurationPath(string projectPath, string activeConfigName)
    {
        /* 
        This method retrieve the Automation Studio project active configuration path
        */

        // Load xml document from file
        string activeConfigFile = Path.Combine(projectPath,"Physical",activeConfigName,"Config.pkg");
        XmlDocument xmlDoc = new();
        xmlDoc.Load(activeConfigFile);

        // Extract namespace
        XmlNamespaceManager nsmgr = new(xmlDoc.NameTable);
        if (xmlDoc.DocumentElement != null)
        {
            string namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
            nsmgr.AddNamespace("ns", namespaceUri);
        } 
    
        // Searching for CPU name
        string? cpuName = null; 
        XmlNode? parentNode = xmlDoc.SelectSingleNode("/ns:Configuration/ns:Objects",nsmgr);

        if (parentNode != null)
        {
            foreach (XmlNode childNode in parentNode.ChildNodes)
            {
                if (childNode is XmlElement element)
                {                       
                    var attribute = element.Attributes["Type"];
                    if (attribute != null && attribute.Value == "Cpu")
                    {
                        cpuName = element.InnerText;
                        break;
                    }
                }    
            }
        }            
        else throw new Exception($"{activeConfigFile} file structure not supported");
        
        if (cpuName == null) throw new Exception($"{activeConfigFile} cannot find Cpu");

        return Path.Combine(activeConfigFile,cpuName);
    }
    
    public static FileInfo GetASDescriptiveFile(string path)
    {
        try
        {
            DirectoryInfo dirInfo = new(path);
            return dirInfo.GetFiles().First(file => Constants.DescriptiveFileExtensionsAndXMLObj.Any(_ => _.Item1 == file.Extension));   
        }
        catch (System.Exception)
        {
            throw new Exception($"Cannot find descriptive file in {path}");
        }   
    }

    public static void AddXmlElementsToFile(XmlElement[] elements, string xmlPath, string filePath)
    {
        if (!File.Exists(filePath)) throw new Exception($"Cannot find xml file {filePath}.");

        // Carica il documento XML dal file
        XmlDocument xmlDoc = new();
        xmlDoc.Load(filePath);

        // Estrazione spazio dei nomi dal nodo radice
        XmlNamespaceManager nsmgr = new(xmlDoc.NameTable);
        string namespaceUri = "";
        if (xmlDoc.DocumentElement != null)
        {
            namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
            nsmgr.AddNamespace("ns", namespaceUri);
        } 
    
        // Trova l'elemento specificato dal percorso XPath
        XmlNode? parentNode = xmlDoc.SelectSingleNode(xmlPath,nsmgr);

        if (parentNode != null)
        {
            // Aggiunge l'elemento come figlio del nodo trovato
            foreach (var element in elements)
            {
                XmlElement newElement = xmlDoc.CreateElement(element.LocalName,namespaceUri);
                newElement.InnerText = element.InnerText;
                foreach (XmlAttribute attribute in element.Attributes)
                    newElement.SetAttribute(attribute.Name, attribute.Value);
    
                XmlNode importedNode = xmlDoc.ImportNode(newElement, true);
                parentNode.AppendChild(importedNode);  
            }

            // Salva il documento XML aggiornato
            xmlDoc.Save(filePath);

        }
        else throw new Exception($"Cannot find path {xmlPath} in file {filePath}. Failed to add xml elements");
    } 
    
    public static void AddDescriptiveXmlElement(string addedItemPath, string type)
    {      
        // Get DescriptiveFile
        string? descriptiveDirName = Path.GetDirectoryName(addedItemPath) ?? throw new Exception($"Cannot find {addedItemPath} folder");
        FileInfo descriptiveFile = Utilities.GetASDescriptiveFile(descriptiveDirName);    

        // Get ItemName
        string itemName = Path.GetFileName(addedItemPath);  
            
        // Get Xml element
        XmlElement[] newElement = [GetDescriptiveXmlElement(type,itemName)];

        // Get xmlPath
        string xmlPath = Constants.DescriptiveFileExtensionsAndXMLObj.First(_ => _.Item1 == descriptiveFile.Extension).Item2;
        
        // Add element to xml
        AddXmlElementsToFile(newElement,xmlPath,descriptiveFile.FullName);
    }

    public static XmlElement GetDescriptiveXmlElement(string type, string name)
    {
        XmlDocument xmlDoc = new();

        if (!Constants.AllowedTypes.Contains(type.ToLower())) throw new Exception($"Specified type {type} of element {name} not supported.");

        string xmlString = "";

        switch (type.ToLower())
        {
            case "package":   
                xmlString = $"<Object Type=\"Package\">{name}</Object>";
                break;

            case "file":   
                xmlString = $"<Object Type=\"File\">{name}</Object>";
                break;

            case "library_binary":   
                xmlString = $"<Object Type=\"Library\" Language=\"binary\">{name}</Object>";
                break;

            case "library_iec":   
                xmlString = $"<Object Type=\"Library\" Language=\"IEC\">{name}</Object>";
                break;

            case "program_iec":   
                xmlString = $"<Object Type=\"Program\" Language=\"IEC\">{name}</Object>";
                break;
        }

        xmlDoc.LoadXml(xmlString);
        
        if (xmlDoc.DocumentElement == null) throw new Exception($"Something went wrong generating Xml descriptive object of type {type} and name {name}");
        return xmlDoc.DocumentElement;
    }

    public static bool IsFolderNameValid(string inputText)
    {
        // Null or empty
        if (string.IsNullOrWhiteSpace(inputText))
            return false;

        // Max length
        if (inputText.Length > 255)
            return false;

        // Check for invalid chars
        string invalidChars = new(System.IO.Path.GetInvalidFileNameChars());
        if (inputText.IndexOfAny(invalidChars.ToCharArray()) >= 0)
            return false;

        // Check for reserved names
        string[] reservedNames = [  "CON", "PRN", "AUX", "NUL", 
                                    "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                                    "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" ];
        if (reservedNames.Contains(inputText.ToUpper()))
            return false;

        return true;
    }

    public static bool IsTextValidForIniValue(string inputText)
    {
        // Invalid chars to start with
        if (inputText.StartsWith(';') || inputText.StartsWith('#'))
            return false;

        return true;
    }

}

public class Models
{
       
}
public static class Constants
{
    // Registry
    public const string RegistryMainPath = "SOFTWARE\\ASTools\\";  
    public const string ConfigFileRegistryKey = "ConfigFile"; 
    public const string LogErrorFileRegistryKey = "LogError";
    public const string CorePathRegistryKey = "CorePath"; 
    
    // Significat files
    public const string LogErrorFilePath = "astools_error.log";
    public const string TemplateConfigFile = "template_config.xml";

    // Parameters
    public static ReadOnlyCollection<string> AllowedTypes { get => new(["package", "file", "library_binary", "library_iec", "program_iec"]);}
    public static ReadOnlyCollection<(string,string)> DescriptiveFileExtensionsAndXMLObj 
    { 
        get => new([(".pkg","/ns:Package/ns:Objects"), (".lby","/ns:Library/ns:Objects")]);
    }
}
