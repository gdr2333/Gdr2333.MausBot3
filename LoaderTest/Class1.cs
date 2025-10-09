// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Reflection;
using Gdr2333.MausBot3.PluginSdk;
using Microsoft.Extensions.Logging;

namespace LoaderTest;

public class LoaderTest(ILoggerFactory loggerFactory) : Plugin
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<LoaderTest>();

    public override CommandBase[] Commands => [
        new StandardCommand(
            "mb3ldr-test",
            [],
            "我这动态加载能用吗？",
            "^{0}{1}$",
            async (c, e, fuck0) =>
            {
                _logger.LogInformation("DI Works!");
                await c.SendMessageAsync(e, new($"MausBot3 {new AssemblyName(Assembly.GetEntryAssembly()?.FullName).Version} & MausBot3-PluginSdk {new AssemblyName(Assembly.GetAssembly(typeof(CommandBase)).FullName).Version} on CoreCLR {Environment.Version} on {Environment.OSVersion} as PID {Environment.ProcessId}.\nStackTrace:\n{Environment.StackTrace}"));
            }
        ),
        new StandardCommand(
            "mb3ldr-exception",
            [],
            "我要是引发一个异常会发生什么？",
            "^{0}{1}$",
            async (c, e, fuck0) =>
            {
                throw new InvalidOperationException("Manual triggered. For testing use only.");
            }
        ),
        .. new CommandEx(
            "mb3ldr-exception1",
            [],
            "我要是引发一个异常会发生什么？(CommandEx版)",
            "^{0}{1}$",
            async (mp, e, ct) =>
            {
                throw new InvalidOperationException("Manual triggered. For testing use only.");
            }
        ).Commands
    ];

    public override string PluginId => "Gdr2333.MausBot3.ADemo";

    public override string PluginName => "动态加载测试";
}
