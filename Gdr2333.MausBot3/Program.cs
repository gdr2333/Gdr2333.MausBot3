// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.MausBot3;
using Gdr2333.MausBot3.InternalPlugins;
using Gdr2333.MausBot3.PluginSdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("MausBot3 by df1050 - 0.0.4-alpha3 & MausBot3-PluginSdk 0.0.4-alpha3");
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

// 在此处声明是为了能让它扔到内部插件的初始化函数里。
List<CommandHelper> commands = [];

List<Plugin> plugins = [new AdminPlugin(data, factory, commands)];

List<PluginLoadingContext> asmContexts = [];

if (!Directory.Exists("plugins"))
    Directory.CreateDirectory("plugins");

List<object> DIObjects = [data, factory];

Console.WriteLine("开始搜索插件");
foreach (var plugindir in Directory.EnumerateDirectories($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}plugins"))
{
    Console.WriteLine($"进入文件夹{plugindir}");
    var asmContext = new PluginLoadingContext(plugindir);
    // 这个上下文就是用来销毁的。
    var coAsmContext = new AssemblyLoadContext("TEMP", true);
    foreach (var file in Directory.EnumerateFiles(plugindir))
    {
        try
        {
            var asm = coAsmContext.LoadFromAssemblyPath(file);
            var asmName = new AssemblyName(asm.FullName);
            // 如果**默认加载的程序集**里面有这个，用默认的版本
            // 还请插件作者不要瞎更新依赖版本
            if (!AssemblyLoadContext.Default.Assemblies.Any(asm1 => new AssemblyName(asm1.FullName).Name == asmName.Name))
            {
                asmContext.LoadFromAssemblyPath(file);
                Console.WriteLine($"托管程序集已加载：{asmName}");
            }
        }
        catch (BadImageFormatException)
        {
        }
        catch (FileLoadException)
        {
            Console.WriteLine($"{file}已被加载，跳过......");
        }
    }
    asmContext.LoadNativeLib(plugindir);
    bool asmContextHavePlugin = false;
    foreach (var asm in asmContext.Assemblies)
        foreach (var type in asm.GetExportedTypes())
            if (type.IsAssignableTo(typeof(Plugin)) && type.FullName != typeof(Plugin).FullName)
            {
                asmContextHavePlugin |= true;
                Console.WriteLine($"找到插件类型{type}，正在尝试初始化");
                bool loaded = false;
                foreach (var initWay in type.GetConstructors())
                {
                    List<object> para = [];
                    foreach (var param in initWay.GetParameters())
                        if (DIObjects.Any(obj => param.ParameterType.IsAssignableFrom(obj.GetType())))
                            para.Add(DIObjects.Where(obj => param.ParameterType.IsAssignableFrom(obj.GetType())).First());
                        else
                            goto TryNext;
                    var obj = Activator.CreateInstance(type, [.. para]);
                    if (obj is Plugin plugin)
                    {
                        plugins.Add(plugin);
                        loaded = true;
                        Console.WriteLine($"初始化了类型{type}，插件ID={plugin.PluginId}");
                        break;
                    }
                TryNext:;
                }
                if (!loaded)
                {
                    Console.WriteLine($"无法初始化{type}，继续加载其它类型......");
                }
            }
    if (!asmContextHavePlugin)
    {
        Console.WriteLine("警告：文件夹中不包含可识别插件。程序集加载上下文已被销毁。");
        asmContext.Dispose();
    }
    coAsmContext.Unload();
}

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