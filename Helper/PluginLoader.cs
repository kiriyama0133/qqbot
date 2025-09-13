using qqbot.Abstractions;
using qqbot.Handlers;
using System.Reflection;

namespace qqbot.Helper;

public static class PluginLoaderExtensions
{
    /// <summary>
    /// 发现并加载 Plugins 文件夹下的所有程序集
    /// </summary>
    /// <returns>已加载的插件程序集列表</returns>
    public static List<Assembly> DiscoverPluginAssemblies()
    {
        Console.WriteLine("开始发现插件程序集...");
        var loadedAssemblies = new List<Assembly>();
        string pluginsPath = Path.Combine(AppContext.BaseDirectory, "Plugins");

        if (!Directory.Exists(pluginsPath))
        {
            Console.WriteLine("插件目录未找到。");
            return loadedAssemblies;
        }

        var pluginDlls = Directory.GetFiles(pluginsPath, "*.dll");
        foreach (var dllPath in pluginDlls)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                loadedAssemblies.Add(assembly);
                Console.WriteLine($"✅ 发现插件: {assembly.GetName().Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 加载插件 {Path.GetFileName(dllPath)} 失败: {ex.Message}");
            }
        }
        Console.WriteLine($"共发现 {loadedAssemblies.Count} 个插件程序集。");
        return loadedAssemblies;
    }
}