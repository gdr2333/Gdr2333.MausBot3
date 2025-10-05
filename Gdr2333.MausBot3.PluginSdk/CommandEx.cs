// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Events;
using System.Collections.Concurrent;

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 带上下文的文本指令
/// </summary>
public class CommandEx
{
    private readonly ConcurrentDictionary<SessionSource, SessionData> sessions;

    /// <summary>
    /// 内部启动命令
    /// </summary>
    public StandardCommand StarterCommand { get; private set; }

    /// <summary>
    /// 内部处理命令
    /// </summary>
    public Command<MessageReceivedEventArgsBase> HandlerCommand { get; private set; }

    /// <summary>
    /// 初始化带上下文的文本指令
    /// </summary>
    /// <param name="name">指令主要名称</param>
    /// <param name="alias">指令别名</param>
    /// <param name="description">指令描述</param>
    /// <param name="regexFormat">正则表达式生成器——{0}作为命令前缀，{1}作为命令名称</param>
    /// <param name="handler">指令执行函数</param>
    /// <param name="startExtraCheck">启动指令的输入额外检查</param>
    /// <param name="stillExtraCheck">指令执行过程中的额外检查</param>
    /// <param name="priority">指令优先级</param>
    /// <param name="exclusive">指令是否会独占输入</param>
    /// <param name="adminRequired">指令是否只能由管理员调用</param>
    public CommandEx(string name,
    string[] alias,
    string description,
    string regexFormat,
    Func<MessagePipe, MessageReceivedEventArgsBase, CancellationToken, Task> handler,
    Func<MessageReceivedEventArgsBase, bool>? startExtraCheck = null,
    Func<MessageReceivedEventArgsBase, bool>? stillExtraCheck = null,
    sbyte priority = 0,
    bool exclusive = false,
    bool adminRequired = false)
    {
        sessions = new();
        StarterCommand = new(
            name,
            alias,
            description,
            regexFormat,
            (c, e, fuck0) =>
            {
                var src = new SessionSource(e.BotId, e is IGroupEventArgs ge ? ge.GroupId : -1, e.UserId);
                var dat = new SessionData(() => sessions.TryRemove(src, out _), (m, ct) => c.SendMessageAsync(e, m, ct));
                sessions.TryAdd(src, dat);
                handler(dat.MessagePipe, e, dat.CancellationTokenSource.Token).ContinueWith((_) =>
                {
                    sessions.TryRemove(src, out dat);
                    dat?.SessionEnded();
                });
            },
            e => (!sessions.ContainsKey(new(e.BotId, e is IGroupEventArgs ge ? ge.GroupId : -1, e.UserId))) && (startExtraCheck?.Invoke(e) ?? true),
            priority,
            exclusive,
            adminRequired
            );
        HandlerCommand = new(
            name + ".1",
            [],
            "",
            async (c, e) =>
            {
                var dat = sessions?[new(e.BotId, e is IGroupEventArgs ge ? ge.GroupId : -1, e.UserId)];
                if (dat != null)
                {
                    await dat.MessagePipe.ReadChannel.Writer.WriteAsync(e.Message);
                    dat.ResetWatchdog();
                }
            },
            (e) => sessions.ContainsKey(new(e.BotId, e is IGroupEventArgs ge ? ge.GroupId : -1, e.UserId)) && (stillExtraCheck?.Invoke(e) ?? true),
            priority,
            exclusive,
            adminRequired
            )
        {
            IsGenerated = true,
        };
    }

    /// <summary>
    /// 内部指令列表
    /// </summary>
    public CommandBase<MessageReceivedEventArgsBase>[] Commands => [StarterCommand, HandlerCommand];
}
