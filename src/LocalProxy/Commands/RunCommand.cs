using System.CommandLine;
using LocalProxy.Handlers;
using LocalProxy.Services;

namespace LocalProxy.Commands;

public static class RunCommand
{
    public static Command Build()
    {
        var cmd = new Command("run", "启动代理隧道");

        var configOption = new Option<string>("--config")
            { Description = "配置文件路径 (JSON 数组)", Required = true };

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
