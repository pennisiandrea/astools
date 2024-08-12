using System.Xml;

namespace ASTools.Tools.Module
{
    class Module
    {
        private const string TEMPLATES_FOLDER_NAME = "Templates";
        public const int MAX_MOD_NAME_LEN = 12;
        private string ModuleName;
        private string TemplateName;

        private static readonly string[] KEYWORDS = ["__MODNAME__","__MODNAMECAPITAL__"];
    
        public Module(string moduleName, string templateName)
        {
            ModuleName = moduleName;
            TemplateName = templateName;
        }
        public Module(string moduleName, string templateName, string destinationPath)
        {
            ModuleName = moduleName;
            TemplateName = templateName;
            Create(destinationPath);
        }
        

        public void Create(string destinationPath)
        {
            string templatePath = Path.Combine(Program.GetModuleToolPath,TEMPLATES_FOLDER_NAME,TemplateName);

            var replacements = KEYWORDS.Zip([ModuleName,ModuleName.ToUpper()], (first, second) => new {First = first, Second = second});

            Utilities.MoveAndReplace(destinationPath,templatePath,replacements);


            

            // Edit template




            /*
            string FileContent;
            string? FileName;

            // Package directory
            Directory.CreateDirectory(PackageDirectory);

            // Program directory
            Directory.CreateDirectory(ProgramDirectory);
            
            // Main file
            FileContent = Module.MAIN_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).Replace(MODCAPITAL_NAME_KEYWORD,ModuleName.ToUpper()).TrimStart();
            CreateFile(ProgramDirectory,Module.MAIN_FILE_NAME,FileContent);

            // Actions file
            FileContent = Module.ACTIONS_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).Replace(MODCAPITAL_NAME_KEYWORD,ModuleName.ToUpper()).TrimStart();
            CreateFile(ProgramDirectory,Module.ACTIONS_FILE_NAME,FileContent);
            
            // Local types file
            FileContent = Module.LOC_TYPES_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).TrimStart();
            CreateFile(ProgramDirectory,Module.LOC_TYPES_FILE_NAME,FileContent);
            
            // Local variables file
            FileContent = Module.LOC_VARIABLES_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).TrimStart();
            CreateFile(ProgramDirectory,Module.LOC_VARIABLES_FILE_NAME,FileContent);
            
            // IEC file
            FileContent = Module.IEC_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).TrimStart();
            CreateFile(ProgramDirectory,Module.IEC_FILE_NAME,FileContent);
            
            // Alarms text file
            FileContent = Module.ALARMS_TXT_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).TrimStart();
            FileName = Module.ALARMS_TXT_FILE_NAME.Replace(MOD_NAME_KEYWORD,ModuleName);
            CreateFile(PackageDirectory,FileName,FileContent);
            
            // Global types file
            FileContent = Module.GLB_TYPES_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).Replace(MODCAPITAL_NAME_KEYWORD,ModuleName.ToUpper()).TrimStart();
            FileName = Module.GLB_TYPES_FILE_NAME.Replace(MOD_NAME_KEYWORD,ModuleName);
            CreateFile(PackageDirectory,FileName,FileContent);
            
            // Global variables file
            FileContent = Module.GLB_VARIABLES_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).TrimStart();
            FileName = Module.GLB_VARIABLES_FILE_NAME.Replace(MOD_NAME_KEYWORD,ModuleName);
            CreateFile(PackageDirectory,FileName,FileContent);
            
            // This package file
            FileContent = Module.THIS_PKG_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).TrimStart();
            FileName = Module.THIS_PKG_FILE_NAME.Replace(MOD_NAME_KEYWORD,ModuleName);
            CreateFile(PackageDirectory,FileName,FileContent);
            
            // Parent package file
            FileContent = Module.PARENT_PKG_FILE_CONTENT.Replace(MOD_NAME_KEYWORD,ModuleName).TrimStart();
            FileName = Directory.GetFiles(ThisDirectory,"Package.pkg").FirstOrDefault();
            if (FileName != null) MergePackageFiles(FileName,FileContent);
            else 
            {                
                FileName = Module.PARENT_PKG_FILE_NAME.Replace(MOD_NAME_KEYWORD,ModuleName);
                CreateFile(ThisDirectory,FileName,FileContent);
            }
            */
        }
        private void CreateFile(string path, string name, string content)
        {
            string pathAndName = Path.Combine(path,name);

            var a = File.Create(pathAndName);
            a.Close();
            
            using (StreamWriter writer = new StreamWriter(pathAndName,true)) 
                writer.Write(content);
        }
        private void MergePackageFiles(string destPath, string content)
        {
            XmlDocument TemplateXmlFile = new XmlDocument();
            XmlDocument ThisXmlFile = new XmlDocument();
            XmlNode? ThisFilesNode;
            XmlNodeList? TemplateFileNodes;

            ThisXmlFile.Load(destPath);
            ThisFilesNode = ThisXmlFile.SelectSingleNode("//*[local-name()='Objects']");
            if (ThisFilesNode == null) throw new Exception("Cannot find Objects node in this package file.");
            
            TemplateXmlFile.LoadXml(content);
            TemplateFileNodes = TemplateXmlFile.SelectNodes("//*[local-name()='Object']");         
            if (TemplateFileNodes == null) throw new Exception("Cannot find Object nodes in template package file.");

            foreach(XmlNode node in TemplateFileNodes) ThisFilesNode.AppendChild(ThisXmlFile.ImportNode(node,true));                  

            // Check for duplicates
            foreach(XmlNode node1 in ThisFilesNode)
            {
                var cnt = 0;
                foreach(XmlNode node2 in ThisFilesNode)
                {
                    if (node1.InnerText == node2.InnerText) cnt = cnt + 1;
                    if (cnt>1) ThisFilesNode.RemoveChild(node2);
                }
            }

            ThisXmlFile.Save(destPath);
        }
    }

}