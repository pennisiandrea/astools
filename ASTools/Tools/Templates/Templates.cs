using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Security.Cryptography;
using System.IO.Compression;

namespace ASTools.Tools.Templates
{
    [XmlRoot("Template")]
    public class TemplateConfigClass
    {
        [XmlArray("Keywords")]
        [XmlArrayItem("Keyword")]
        public List<KeywordClass>? Keywords {get; set;}

        [XmlArray("Instructions")]
        [XmlArrayItem("Instruction")]
        public List<InstructionClass>? Instructions {get; set;}

        // Submembers
        public class KeywordClass
        {
            [XmlAttribute("ID")]
            public required string ID { get; set; }
        }
        public class InstructionClass
        {
            [XmlAttribute("Type")]
            public required string Type { get; set; }

            [XmlElement("Source")]
            public SourceClass? Source { get; set; }

            [XmlElement("Destination")]
            public DestinationClass? Destination { get; set; }

            [XmlElement("XmlElements2Add")]
            public XmlElements2AddClass? XmlElements2Add { get; set; }
        }
        public class SourceClass
        {
            [XmlAttribute("Path")]
            public required string Path { get; set; }
        }
        public class DestinationClass
        {
            [XmlAttribute("Path")]
            public required string Path { get; set; }
        }
        public class XmlElements2AddClass
        {
            [XmlAttribute("Path")]
            public required string Path { get; set; }

            [XmlAnyElement]
            public required XmlElement[] XmlElements { get; set; }
        }

        // Others
        public override string ToString()
        {
            string returnValue = "";

            // Keywords
            if (Keywords != null)
            {
                returnValue += "\nKeywords:\n";
                foreach (var _ in Keywords) returnValue += $"\tID:{_.ID}\n";
            }

            // Instructions
            if (Instructions != null)
            {
                returnValue += "\nInstructions:\n";
                foreach (var _ in Instructions) 
                {
                    switch (_.Type)
                    {
                        case "Copy":
                            if (_.Source != null && _.Destination != null)
                            {
                                returnValue += $"\tType:{_.Type}\n";
                                returnValue += $"\tSource:{_.Source.Path}\n";
                                returnValue += $"\tDestination:{_.Destination.Path}\n";
                            }
                            break;

                        case "ReplaceKeywords":
                            if (_.Destination != null)
                            {
                                returnValue += $"\tType:{_.Type}\n";
                                returnValue += $"\tDestination:{_.Destination.Path}\n";
                            }
                            break;
                            
                        case "AddXmlElement":
                            if (_.Destination != null && _.XmlElements2Add != null)
                            {
                                returnValue += $"\tType:{_.Type}\n";
                                returnValue += $"\tDestination:{_.Destination.Path}\n";
                                returnValue += $"\tPath:{_.XmlElements2Add.Path}\n";
                                foreach (var item in _.XmlElements2Add.XmlElements)
                                {
                                    returnValue += $"\tXmlElement:{item.OuterXml}\n";
                                }
                            }
                            break;
                    }
                    returnValue += "\n";
                }
            }

            return returnValue;
        }
    }

    static class Utilities
    {
        static public void Copy(string destPath, string sourcePath, string?[]? exceptions)
        {
            if (File.Exists(sourcePath)) // Copying a single file
            {
                FileInfo file = new FileInfo(sourcePath);
                file.CopyTo(Path.Combine(destPath,file.Name));
            }
            else if (Directory.Exists(sourcePath)) // Copying a folder
            {                
                DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);
                DirectoryInfo destDir = new DirectoryInfo(destPath);

                // Check if in destPath exists a directory like sourcePath. This sould be true only the first time
                if (sourceDir.Name != destDir.Name)
                {
                    destPath = Path.Combine(destPath,sourceDir.Name);
                    Directory.CreateDirectory(destPath);
                }

                // Copy all files (except exceptions)
                FileInfo[] sourceFiles = sourceDir.GetFiles();
                foreach (FileInfo file in sourceFiles) 
                {
                    if (exceptions != null)
                    {
                        if (!exceptions.Any(ex => ex == file.FullName))
                            file.CopyTo(Path.Combine(destPath,file.Name));
                    }
                    else file.CopyTo(Path.Combine(destPath,file.Name));
                }
                // Copy all folders (except exceptions)
                DirectoryInfo[] directories = sourceDir.GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    string newDestPath = Path.Combine(destPath,directory.Name);
                    if (exceptions != null)
                    {
                        if (!exceptions.Any(ex => ex == directory.FullName))
                            Directory.CreateDirectory(newDestPath);                    
                    }
                    else Directory.CreateDirectory(newDestPath);                
                    Copy(newDestPath, directory.FullName,exceptions);
                }                
            }
        }
        static public void ReplaceKeywords(string destPath, List<KeyValuePair<string,string>> replacementsList)
        {
            if (File.Exists(destPath)) // Provided path is just a file
            {
                FileInfo destFile = new FileInfo(destPath);
                string fileName = destFile.FullName;
                
                // Replace keywords in the file name
                if (replacementsList.Any(_ => _.Key == destFile.Name))
                {
                    foreach (var item in replacementsList) fileName = fileName.Replace(item.Key,item.Value);
                    File.Move(destPath,fileName);
                }

                // Replace keywords in the content
                string content = File.ReadAllText(fileName);
                foreach (var item in replacementsList) content = content.Replace(item.Key,item.Value);
                File.WriteAllText(fileName,content);
            }
            else if(Directory.Exists(destPath)) // Provided path is a folder
            {
                DirectoryInfo destDir = new DirectoryInfo(destPath);

                // Check if destDir name contains a keyword
                if (replacementsList.Any(_ => _.Key == destDir.Name))
                {
                    foreach (var item in replacementsList) destPath = destDir.FullName.Replace(item.Key,item.Value);
                    destDir.MoveTo(destPath);
                }

                // Rename files and contents
                FileInfo[] destFiles = destDir.GetFiles();
                foreach (FileInfo file in destFiles)
                {
                    string fileName = file.FullName;
                    foreach (var item in replacementsList) fileName = fileName.Replace(item.Key,item.Value);
                    file.MoveTo(fileName);

                    string content = File.ReadAllText(file.FullName);
                    foreach (var item in replacementsList) content = content.Replace(item.Key,item.Value);
                    File.WriteAllText(file.FullName,content);
                }

                // Rename folders and navigates in they
                DirectoryInfo[] directories = destDir.GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    string directoryName = directory.FullName;
                    foreach (var item in replacementsList) directoryName = directoryName.Replace(item.Key,item.Value);
                    directory.MoveTo(directoryName);
                    ReplaceKeywords(directoryName, replacementsList);
                }
            }
            else throw new Exception($"Invalid path to replace keywords: {destPath}");
        }
        public static void AddXmlElementsToFile(XmlElement[] xmlElements, string xmlPath, string xmlFilePath)
        {
            // Carica il documento XML dal file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            if (xmlDoc.DocumentElement != null)
            {
                string namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
                nsmgr.AddNamespace("ns", namespaceUri);
            } 
        
            // Trova l'elemento specificato dal percorso XPath
            XmlNode? parentNode = xmlDoc.SelectSingleNode(xmlPath,nsmgr);

            if (parentNode != null)
            {
                // Aggiunge l'elemento come figlio del nodo trovato
                foreach (var element in xmlElements)
                {
                    XmlNode importedNode = xmlDoc.ImportNode(element, true);
                    parentNode.AppendChild(importedNode);   

                    // Remove unwanted xmlns from imported Node
                    foreach (XmlNode childNode in parentNode.ChildNodes)
                    {
                        if (childNode.Name == element.Name && childNode.Value == element.Value)
                        {
                            if (childNode is XmlElement childElement)
                            {
                                if (element.Attributes["xmlns"] != null)
                                    childElement.RemoveAttribute("xmlns");
                            }
                        }
                    }               
                }

                // Salva il documento XML aggiornato
                xmlDoc.Save(xmlFilePath);
            }
            else throw new Exception($"Cannot find path {xmlPath} in file {xmlFilePath}. Failed to add xml elements");
        }
    } 

    public class Templates
    {     
    }

    public class Template
    {   
        private Dictionary<string,string?> ConfigConstants = new ()
        {
            {"$CONFIG_FILE_NAME","template_config.xml"},
            {"$TEMPLATE_PATH",null},
            {"$USER_PATH",null}
        };

        private TemplateConfigClass? _config;
        public TemplateConfigClass? Config { get => _config;}

        public Template(string templatePath, string userPath)
        {
            ConfigConstants["$TEMPLATE_PATH"] = templatePath;
            ConfigConstants["$USER_PATH"] = userPath;

            LoadConfig();            
        }

        public void LoadConfig()
        {

            string? configFileName = ConfigConstants["$CONFIG_FILE_NAME"];
            string? templatePath = ConfigConstants["$TEMPLATE_PATH"];
            if (configFileName != null && templatePath != null) configFileName = Path.Combine(templatePath,configFileName);
            else throw new Exception($"Wrong ConfigConstants initialization!");
            if (!File.Exists(configFileName)) throw new Exception($"{configFileName} file not found!");

            XmlSerializer serializer = new XmlSerializer(typeof(TemplateConfigClass));

            using (FileStream fileStream = new FileStream(configFileName, FileMode.Open))
            {
                _config = (TemplateConfigClass?)serializer.Deserialize(fileStream);

                if (_config == null) throw new Exception($"Failed to deserialize {configFileName}");

                // Convert CONSTANTS to real values:
                if (_config.Instructions != null)
                {
                    foreach (var item in _config.Instructions)
                    {
                        if(item.Destination != null) item.Destination.Path = ReplaceConstants(item.Destination.Path);
                        if(item.Source != null) item.Source.Path = ReplaceConstants(item.Source.Path);
                        if(item.XmlElements2Add != null) item.XmlElements2Add.Path = ReplaceConstants(item.XmlElements2Add.Path);                        
                    }
                }
            }
        }

        public void ExecuteInstructions()
        {
            if (_config != null && _config.Instructions != null)
            {
                foreach (var _ in _config.Instructions)
                {
                    switch (_.Type)
                    {
                        case "Copy":
                            Console.WriteLine("Copy instruction");
                            if (_.Source != null && _.Destination != null)
                            {                                
                                string? configFileName = ConfigConstants["$CONFIG_FILE_NAME"];
                                string? templatePath = ConfigConstants["$TEMPLATE_PATH"];
                                if (configFileName != null && templatePath != null) configFileName = Path.Combine(templatePath,configFileName);
                                string?[] exceptions = [configFileName];
                                Console.WriteLine($"\tSrc:{_.Source.Path}\n\tDest:{_.Destination.Path}");
                                Utilities.Copy(_.Destination.Path,_.Source.Path,exceptions);
                            }
                            break;

                        case "ReplaceKeywords":
                            Console.WriteLine("ReplaceKeywords instruction\n");
                            if (_.Destination != null && _config.Keywords != null)
                            {
                                List<KeyValuePair<string,string>> replacements = new();
                                replacements.Add(new KeyValuePair<string,string>(_config.Keywords[0].ID,"ciao"));
                                Utilities.ReplaceKeywords(_.Destination.Path,replacements);
                            }
                            break;
                            
                        case "AddXmlElement":
                            Console.WriteLine("AddXmlElement instruction\n");
                            if (_.Destination != null && _.XmlElements2Add != null)
                                Utilities.AddXmlElementsToFile(_.XmlElements2Add.XmlElements,_.XmlElements2Add.Path,_.Destination.Path);
                            break;
                    }
                }
            }
        }

        private string ReplaceConstants(string str)
        {
            foreach (var item in ConfigConstants) str = str.Replace(item.Key,item.Value);
            
            return str;
        }

    }

}