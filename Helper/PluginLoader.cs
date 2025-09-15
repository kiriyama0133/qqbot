using qqbot.Abstractions;
using qqbot.Handlers;
using System.Reflection;

namespace qqbot.Helper;

public static class PluginLoaderExtensions
{
    /// <summary>
    /// 发现并加载 ExtensionsEntry 文件夹下的所有程序集
    /// </summary>
    /// <returns>已加载的插件程序集列表</returns>
    public static List<Assembly> DiscoverPluginAssemblies()
    {
        Console.WriteLine("开始发现插件程序集...");
        var loadedAssemblies = new List<Assembly>();
        string extensionsEntryPath = Path.Combine(AppContext.BaseDirectory, "ExtensionsEntry");

        if (!Directory.Exists(extensionsEntryPath))
        {
            Console.WriteLine("ExtensionsEntry 目录未找到。");
            return loadedAssemblies;
        }

        // 扫描 ExtensionsEntry 下的所有子目录
        var pluginDirectories = Directory.GetDirectories(extensionsEntryPath);
        
        foreach (var pluginDir in pluginDirectories)
        {
            var pluginId = Path.GetFileName(pluginDir);
            Console.WriteLine($"扫描插件目录: {pluginId}");
            
            // 在每个插件目录中查找 DLL 文件
            var dllFiles = Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly);
            
            foreach (var dllPath in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);
                    loadedAssemblies.Add(assembly);
                    Console.WriteLine($"✅ 发现插件: {assembly.GetName().Name} (来自 {pluginId})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 加载插件 {Path.GetFileName(dllPath)} 失败: {ex.Message}");
                }
            }
        }
        
        Console.WriteLine($"共发现 {loadedAssemblies.Count} 个插件程序集。");
        return loadedAssemblies;
    }
}