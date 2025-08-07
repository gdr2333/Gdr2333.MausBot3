// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.MausBot3;
using Gdr2333.MausBot3.InternalPlugins;
using Gdr2333.MausBot3.PluginSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("MausBot3 by df1050 - 内部测试版本");
Console.WriteLine("初始化数据......");

var jsonso = new JsonSerializerOptions() { WriteIndented = true };
jsonso.Converters.Add(new JsonStringEnumConverter());

Data data;

using (var configfile = File.OpenRead("Config.json"))
{
    data = JsonSerializer.Deserialize<Data>(configfile, jsonso) ?? throw new InvalidDataException("找不到配置文件或格式错误！");
}

HostApplicationBuilder builder = new();

ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

List<Plugin> plugins = [new AdminPlugin(data, factory)];

// TODO : 基于反射的插件加载

List<CommandHelper> commands = [];
foreach (var plugin in plugins)
{
    Console.WriteLine($"加载插件{plugin.PluginName}");
    foreach (var command in plugin.Commands)
    {
        var cmdHelper = new CommandHelper(command, plugin);
        Console.WriteLine($"加载命令{cmdHelper.Id}");
        commands.Add(cmdHelper);
        if (command is StandardCommand stdcmd)
        {
            Console.WriteLine($"配置命令{cmdHelper.Id}");
            string[] names;
            if (data.CommandAliases.TryGetValue(cmdHelper.Id, out var alias))
                if (alias.UseDrfaultName)
                    if (alias.UseDefaultAlias)
                        names = [stdcmd.CommandName, .. stdcmd.CommandAlias, .. alias.NewAliases];
                    else
                        names = [stdcmd.CommandName, .. alias.NewAliases];
                else if (alias.UseDefaultAlias)
                    names = [.. stdcmd.CommandAlias, .. alias.NewAliases];
                else
                    names = [.. alias.NewAliases];
            else
                names = [stdcmd.CommandName, .. stdcmd.CommandAlias];
            stdcmd.SetRealAlias(data.Prompt, names);
            Console.WriteLine($"为{cmdHelper.Id}配置了{names.Length}个名称");
        }
    }
    Console.WriteLine($"插件{plugin.PluginName}加载完成，共{plugin.Commands.Length}条命令");
}
Console.WriteLine($"共加载了{plugins.Count}个插件，{commands.Count}条命令");
commands.Sort((a, b) => b.Command.Priority.CompareTo(a.Command.Priority));

Console.WriteLine($"正在尝试在{data.Address}启动反向WebSocket服务器......");
ReverseWebSocketClient client = new(data.Address, data.AccessToken);

client.Start();
Console.WriteLine("反向WebSocket服务器启动成功。");

Console.WriteLine("正在配置服务主机......");
builder.Services.AddSingleton(jsonso)
                .AddSingleton(commands)
                .AddSingleton(data)
                .AddSingleton(new PluginData() { Comamnds = [.. commands], Plugins = [.. plugins] })
                .AddSingleton(client);
builder.Services.AddHostedService<BotService>();

Console.WriteLine("服务主机配置成功。");
builder.Build().Run();