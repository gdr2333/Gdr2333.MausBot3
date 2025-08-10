// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 插件类
/// </summary>
public abstract class Plugin
{
    /// <summary>
    /// 插件内包含的指令
    /// </summary>
    public abstract CommandBase[] Commands { get; }

    /// <summary>
    /// 插件Id，遵循<see href="https://learn.microsoft.com/zh-cn/nuget/create-packages/package-authoring-best-practices#package-id">nuget包名格式</see>
    /// </summary>
    public abstract string PluginId { get; }

    /// <summary>
    /// 插件名称
    /// </summary>
    public abstract string PluginName { get; }
}
