// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 实际上的指令类（省的继承一大堆东西出来）
/// </summary>
/// <typeparam name="T">上报类型</typeparam>
/// <param name="name">指令主要名称</param>
/// <param name="alias">指令别名</param>
/// <param name="description">指令描述</param>
/// <param name="handler">指令执行函数</param>
/// <param name="check">检测某条消息是否会触发指令</param>
/// <param name="priority">指令优先级</param>
/// <param name="exclusive">指令是否会独占输入</param>
/// <param name="adminRequired">指令是否只能由管理员调用</param>
public class Command<T>(
    string name,
    string[] alias,
    string description,
    Func<OnebotV11ClientBase, T, Task> handler,
    Func<T, bool> check,
    sbyte priority = 0,
    bool exclusive = false,
    bool adminRequired = false) 
    : CommandBase<T>
    where T : OnebotV11EventArgsBase
{
    /// <inheritdoc/>
    public override string CommandName => name;

    /// <inheritdoc/>
    public override string[] CommandAlias => alias;

    /// <inheritdoc/>
    public override string CommandDescription => description;

    /// <inheritdoc/>
    public override bool IsExclusiveHandler => exclusive;

    /// <inheritdoc/>
    public override sbyte Priority => priority;

    /// <inheritdoc/>
    public override bool AdminRequired => adminRequired;

    /// <inheritdoc/>
    public override bool CheckHandle(T message) =>
        check(message);

    /// <inheritdoc/>
    public override Task Handle(OnebotV11ClientBase client, T message) =>
        handler(client, message);
}
