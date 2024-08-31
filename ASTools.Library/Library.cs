using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace ASTools.Library;

public static class Utilities
{
    public static string GetStringKeyFromRegistry(string registryPath, string registryKey)
    {
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
    
    // Others
    public const string LogErrorFilePathDefaultValue = "astools_error.log";
}
