using System.CommandLine;
using LocalProxy.Commands;

var rootCommand = new RootCommand("LocalProxy - 本地端口代理转发工具");
rootCommand.Add(RunCommand.Build());

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
