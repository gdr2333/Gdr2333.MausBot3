// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 通用指令基类
/// </summary>
public abstract class CommandBase
{
    /// <summary>
    /// 指令主要名称
    /// </summary>
    public abstract string CommandName { get; }

    /// <summary>
    /// 指令别名
    /// </summary>
    public abstract string[] CommandAlias { get; }

    /// <summary>
    /// 指令描述
    /// </summary>
    public abstract string CommandDescription { get; }

    /// <summary>
    /// 指令是否会独占输入
    /// </summary>
    public abstract bool IsExclusiveHandler { get; }

    /// <summary>
    /// 指令优先级
    /// </summary>
    public abstract sbyte Priority { get; }

    /// <summary>
    /// 指令是否只能由管理员调用
    /// </summary>
    public abstract bool AdminRequired { get; }

    /// <summary>
    /// 指令是否由SDK生成
    /// </summary>
    public bool IsGenerated { get; protected internal set; } = false;

    /// <summary>
    /// 检测某条消息是否会触发指令
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns>是否触发</returns>
    public abstract bool CheckHandle(OnebotV11EventArgsBase message);

    /// <summary>
    /// 指令执行函数
    /// </summary>
    /// <param name="client">收到消息的客户端</param>
    /// <param name="message">触发指令的消息</param>
    public abstract Task Handle(OnebotV11ClientBase client, OnebotV11EventArgsBase message);
}
