using ASTools.Tools.Templates;
using System.Xml;

namespace ASTools
{
    static class Utilities
    {
        static public string Copy(string destPath, string sourcePath)
        {
            string CopiedPath = string.Empty;

            if (File.Exists(sourcePath)) // Copying a single file
            {
                FileInfo file = new FileInfo(sourcePath);
                CopiedPath = Path.Combine(destPath,file.Name);
                file.CopyTo(CopiedPath);
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
                CopiedPath = destPath;

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
            return CopiedPath;
        }
        static public void Append(string destPath, string sourcePath)
        {
            // Append source content to dest content

            if (!File.Exists(destPath) || !File.Exists(sourcePath)) throw new Exception($"Failed to append {sourcePath} to {destPath}");
            
            string sourceContent = File.ReadAllText(sourcePath);
            string destContent = File.ReadAllText(destPath);

            File.WriteAllText(destPath,destContent + "\n" + sourceContent);
        }
        
        static public string GetASProjectPath(string actPath)
        {
            // Check if .apj file is in this directory
            DirectoryInfo actDir = new(actPath);
            FileInfo[] files = actDir.GetFiles();            
            if (files.Any(file => file.Extension == ".apj")) return actDir.FullName;
            else 
            {
                if (actDir.Parent != null) return GetASProjectPath(actDir.Parent.FullName);
                else throw new Exception($"Automation Studio project not found.");
            }
        }
    
        static public string GetASActiveConfiguration(string projectPath)
        {
            string lastUserFile = Path.Combine(projectPath,"LastUser.set");
            if (!File.Exists(lastUserFile)) throw new Exception($"Cannot find file {lastUserFile}");

            // Carica il documento XML dal file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(lastUserFile);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            if (xmlDoc.DocumentElement != null)
            {
                string namespaceUri = xmlDoc.DocumentElement.NamespaceURI;
                nsmgr.AddNamespace("ns", namespaceUri);
            } 
        
            // Trova l'elemento specificato dal percorso 
            XmlNode? node = xmlDoc.SelectSingleNode("/ns:ProjectSettings/ns:ConfigurationManager",nsmgr);

            if (node is XmlElement element)
            {
                var attribute = element.Attributes["ActiveConfigurationName"];
                if (attribute != null) return attribute.Value;  
                else throw new Exception($"{lastUserFile} ActiveConfigurationName attribute not found");     
            }            
            else throw new Exception($"{lastUserFile} file structure not supported");
        }
        static public string GetASActiveConfigurationPath(string projectPath, string activeConfigName)
        {
            // Carica il documento XML dal file
            string activeConfigFile = Path.Combine(projectPath,"Physical",activeConfigName,"Config.pkg");
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(activeConfigFile);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
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

        static public FileInfo GetASDescriptiveFile(string path)
        {
            DirectoryInfo DirInfo = new(path);
            return DirInfo.GetFiles().First(file => file.Extension == ".pkg" || file.Extension == ".lby");    
        }

        static public void AddXmlElementsToFile(XmlElement[] xmlElements, string xmlPath, string xmlFilePath)
        {
            // Carica il documento XML dal file
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            // Estrazione spazio dei nomi dal nodo radice
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
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
                foreach (var element in xmlElements)
                {
                    XmlElement newElement = xmlDoc.CreateElement(element.LocalName,namespaceUri);
                    newElement.InnerText = element.InnerText;
                    foreach (XmlAttribute attribute in element.Attributes)
                        newElement.SetAttribute(attribute.Name, attribute.Value);
        
                    XmlNode importedNode = xmlDoc.ImportNode(newElement, true);
                    parentNode.AppendChild(importedNode);  
                }

                // Salva il documento XML aggiornato
                xmlDoc.Save(xmlFilePath);

            }
            else throw new Exception($"Cannot find path {xmlPath} in file {xmlFilePath}. Failed to add xml elements");
        }
    
        static public void AddDescriptiveXmlElement(string addedItemPath, string type)
        {      
            // Get DescriptiveFile
            string? DescriptiveDirName = Path.GetDirectoryName(addedItemPath);
            if (DescriptiveDirName == null) throw new Exception($"Cannot find {addedItemPath} folder");
            FileInfo DescriptiveFile = GetASDescriptiveFile(DescriptiveDirName);    

            // Get ItemName
            string ItemName = Path.GetFileName(addedItemPath);  
               
            // Get Xml element
            XmlElement[] newElement = [GetDescriptiveXmlElement(type,ItemName)];

            // Get xmlPath
            string? xmlPath = null;
            switch (DescriptiveFile.Extension)
            {
                case ".pkg":
                    xmlPath = "/ns:Package/ns:Objects";
                    break;

                case ".lby":      
                    xmlPath = "/ns:Library/ns:Objects";
                    break;                    
            }
            if (xmlPath == null) throw new Exception($"Descriptive file {DescriptiveFile.FullName} not supported.");

            // Add element to xml
            AddXmlElementsToFile(newElement,xmlPath,DescriptiveFile.FullName);
        }

        static public XmlElement GetDescriptiveXmlElement(string type, string name)
        {
            XmlDocument xmlDoc = new XmlDocument();

            string? xmlString = null;

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

            if (xmlString == null) throw new Exception($"Specified type {type} of element {name} not supported.");
            xmlDoc.LoadXml(xmlString);
            
            if (xmlDoc.DocumentElement == null) throw new Exception($"Something went wrong generating Xml descriptive object of type {type} and name {name}");
            return xmlDoc.DocumentElement;
        }

    } 

    class Program
    {
        static void Main(string[] argv)
        {
            string TemplatePath = "C:\\Data\\astools\\Tools\\Templates\\StdModule";
            string UserPath = "C:\\Data\\astools\\Test\\test\\Logical";

            //string TemplatePath = "C:\\Data\\astools\\Tools\\Templates\\StdEnableFB";
            //string UserPath = "C:\\Data\\astools\\Test\\test\\Logical\\Libraries\\Library";
            Template template = new(TemplatePath,UserPath);

            Console.WriteLine(template.Config.ToString());

            template.ExecuteInstructions();

        }
    }

    /*
    static class Utilities
    {
        static public void MoveAndReplace(string destPath, string sourcePath, IEnumerable<dynamic> replacementsList)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);

            // Copy all files
            FileInfo[] sourceFiles = sourceDir.GetFiles();
            foreach (FileInfo file in sourceFiles)
            {
                string destFileName = Path.Combine(destPath,file.Name);
                foreach (var item in replacementsList) destFileName = destFileName.Replace(item.First,item.Second);
                file.MoveTo(destFileName);
            }

            //Edit content of all copied files
            FileInfo[] destFiles = sourceDir.GetFiles();
            foreach (FileInfo file in destFiles)
            {   
                string content = File.ReadAllText(file.FullName);

                string updatedContent = "";
                foreach (var item in replacementsList) updatedContent = content.Replace(item.First,item.Second);

                File.WriteAllText(file.FullName,updatedContent);
            }

            // Copy folders 
            DirectoryInfo[] directories = sourceDir.GetDirectories();
            foreach (DirectoryInfo directory in directories)
            {
                string destDirectoryName = Path.Combine(destPath,directory.Name);
                foreach (var item in replacementsList) destDirectoryName = destDirectoryName.Replace(item.First,item.Second);
                directory.MoveTo(destDirectoryName);
                MoveAndReplace(destDirectoryName, directory.FullName, replacementsList);
            }

        }

        public static string GetToolsDirectory()
        {
            return "C:\\Data\\astools\\Tools";
        }
    } 

    class Program
    {
        static public string GetToolsPath = "C:\\Data\\astools\\ASTools\\Tools";
        static public string GetModuleToolPath = "C:\\Data\\astools\\ASTools\\Tools\\Module";
        
        static void Main(string[] argv)
        {
            //Console.Clear();
            
            // Get This folder
            string? ThisDirectory;
            if (argv.Count()>=1) 
            {  
                ThisDirectory = argv[1];
                if (ThisDirectory == null || !Directory.Exists(ThisDirectory)) throw new Exception("Exception: Cannot retrieve This directory.");
            }
            else
            {
                try
                {
                    ThisDirectory = Directory.GetCurrentDirectory();
                    if (ThisDirectory == null) throw new Exception("Exception: Cannot retrieve This directory.");         
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.ReadLine();
                    return;
                }
                Console.WriteLine("Working directory: " + ThisDirectory);
            }

            // Get module name
            string? ModuleName;
            try
            {
                if (argv.Count()>=2) 
                {
                    if (argv[2].Length < Module.MAX_MOD_NAME_LEN) ModuleName = argv[2];
                    else throw new Exception("Exception: Module name too long. Max " + Module.MAX_MOD_NAME_LEN.ToString() + " chars!");
                }
                else
                { 
                    var InputOk = false;
                    do
                    {
                        Console.WriteLine("Write the module name: ");
                        ModuleName = Console.ReadLine();
                        if (ModuleName == null) throw new Exception("Exception: Empty module name");
                        else if (ModuleName.Length > Module.MAX_MOD_NAME_LEN ) Console.WriteLine("Module name too long. Max " + MyModule.MAX_MOD_NAME_LEN.ToString() + " chars!");
                        else if (!Regex.IsMatch(ModuleName,"^[a-zA-Z][a-zA-Z0-9]*$")) Console.WriteLine("Module name invalid!");
                        else InputOk = true;
                    } while (!InputOk); 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
                return;
            }

            // Create MyFunctionBlock object
            Module myModule;            
            try
            {
                myModule = new Module(ModuleName);

                myModule.Create();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Done. Bye Bye");
        }

    }
    */
}
