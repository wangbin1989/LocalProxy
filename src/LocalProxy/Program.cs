using System.CommandLine;
using LocalProxy.Commands;

// 构建根命令，注册 run 和 config 子命令
var rootCommand = new RootCommand("LocalProxy - 本地端口代理转发工具");
rootCommand.Add(RunCommand.Build());

foreach (var cmd in ConfigCommands.Build())
    rootCommand.Add(cmd);

// 解析命令行参数并调用对应的命令处理程序
var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
