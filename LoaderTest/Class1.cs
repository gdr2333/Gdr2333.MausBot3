// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

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
            async (c,e) =>
            {
                _logger.LogInformation("DI Works!");
                await c.SendMessageAsync(e, new($"MausBot3 on CoreCLR {Environment.Version} on {Environment.OSVersion} as PID {Environment.ProcessId}.\nStackTrace:\n{Environment.StackTrace}"));
            }
        )
    ];

    public override string PluginId => "Gdr2333.MausBot3.ADemo";

    public override string PluginName => "动态加载测试";
}
