using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using ASTools.Tools.Templates;

namespace ASTools
{
    class Program
    {
        static void Main(string[] argv)
        {
            string TemplatePath = "C:\\Data\\astools\\Tools\\Templates\\StdModule";
            string UserPath = "C:\\Data\\astools\\Test";
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
