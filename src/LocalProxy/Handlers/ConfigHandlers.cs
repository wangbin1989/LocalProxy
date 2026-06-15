using LocalProxy.Infrastructure;
using LocalProxy.Services;

namespace LocalProxy.Handlers;

public static class ConfigHandlers
{
    public static async Task<int> HandleList(string file)
    {
        var configs = await ConfigService.LoadAsync(file);
        ConsoleOutput.RenderConfigTable(configs);
        return 0;
    }

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
