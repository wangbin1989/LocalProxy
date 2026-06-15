using LocalProxy.Infrastructure;
using LocalProxy.Services;

namespace LocalProxy.Handlers;

/// <summary>Config 命令处理程序：代理配置的增删改查</summary>
public static class ConfigHandlers
{
    /// <summary>列出所有代理配置</summary>
    public static async Task<int> HandleList(string file)
    {
        var configs = await ConfigService.LoadAsync(file);
        ConsoleOutput.RenderConfigTable(configs);
        return 0;
    }

    /// <summary>添加代理配置</summary>
    public static async Task<int> HandleAdd(string file, ProxyConfig config)
    {
        try
        {
            await ConfigService.AddAsync(file, config);
            ConsoleOutput.Success($"代理 '{config.Name}' 已添加");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            ConsoleOutput.Error(ex.Message);
            return 1;
        }
    }

    /// <summary>更新代理配置</summary>
    public static async Task<int> HandleUpdate(string file, string name, ProxyConfig updated)
    {
        try
        {
            await ConfigService.UpdateAsync(file, name, updated);
            ConsoleOutput.Success($"代理 '{name}' 已更新");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            ConsoleOutput.Error(ex.Message);
            return 1;
        }
    }

    /// <summary>删除代理配置，非 force 模式下需确认</summary>
    public static async Task<int> HandleRemove(string file, string name, bool force)
    {
        try
        {
            if (!force && !ConsoleOutput.Confirm($"确定删除代理 '{name}'？"))
                return 5;

            await ConfigService.RemoveAsync(file, name);
            ConsoleOutput.Success($"代理 '{name}' 已删除");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            ConsoleOutput.Error(ex.Message);
            return 1;
        }
    }

    /// <summary>启用代理</summary>
    public static async Task<int> HandleEnable(string file, string name)
    {
        try
        {
            await ConfigService.SetEnabledAsync(file, name, true);
            ConsoleOutput.Success($"代理 '{name}' 已启用");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            ConsoleOutput.Error(ex.Message);
            return 1;
        }
    }

    /// <summary>停用代理</summary>
    public static async Task<int> HandleDisable(string file, string name)
    {
        try
        {
            await ConfigService.SetEnabledAsync(file, name, false);
            ConsoleOutput.Success($"代理 '{name}' 已停用");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            ConsoleOutput.Error(ex.Message);
            return 1;
        }
    }
}
