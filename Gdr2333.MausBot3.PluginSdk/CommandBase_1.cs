// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 指定上报类型的指令基类
/// </summary>
/// <typeparam name="T">上报类型</typeparam>
public abstract class CommandBase<T> : CommandBase
    where T : OnebotV11EventArgsBase
{
    /// <summary>
    /// 检测某条消息是否会触发指令
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns>是否触发</returns>
    public abstract bool CheckHandle(T message);

    /// <inheritdoc/>
    public override bool CheckHandle(OnebotV11EventArgsBase message) =>
        CheckHandle((T)message);

    /// <summary>
    /// 指令执行函数
    /// </summary>
    /// <param name="client">收到消息的客户端</param>
    /// <param name="message">触发指令的消息</param>
    public abstract void Handle(OnebotV11ClientBase client, T message);

    /// <inheritdoc/>
    public override void Handle(OnebotV11ClientBase client, OnebotV11EventArgsBase message) =>
        Handle(client, (T)message);
}
