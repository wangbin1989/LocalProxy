using System.CommandLine;
using LocalProxy.Handlers;
using LocalProxy.Services;

namespace LocalProxy.Commands;

/// <summary>Run 命令注册</summary>
public static class RunCommand
{
    /// <summary>构建 run 命令：启动代理隧道</summary>
    public static Command Build()
    {
        var cmd = new Command("run", "启动代理隧道");

        var configOption = new Option<string>("--config")
            { Description = "配置文件路径 (JSON 数组)", DefaultValueFactory = _ => ConfigService.DefaultConfigPath };

        cmd.Add(configOption);

        cmd.SetAction(async parseResult =>
        {
            var configPath = parseResult.GetValue(configOption)!;
            var configs = await ConfigService.LoadAsync(configPath);
            return await RunHandler.HandleMultiple(configs);
        });

        return cmd;
    }
}
