// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Events;
using System.Collections.Concurrent;

namespace Gdr2333.MausBot3.PluginSdk;

public class CommandEx
{
    private readonly ConcurrentDictionary<SessionSource, SessionData> sessions;

    public StandardCommand StarterCommand { get; private set; }

    public Command<MessageReceivedEventArgsBase> HandlerCommand { get; private set; }

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
            (c, e) =>
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
                    dat.SendMessage = (m, ct) => c.SendMessageAsync(e, m, ct);
                    await dat.MessagePipe.ReadChannel.Writer.WriteAsync(e.Message);
                }
            },
            (e) => sessions.ContainsKey(new(e.BotId, e is IGroupEventArgs ge ? ge.GroupId : -1, e.UserId)) && (stillExtraCheck?.Invoke(e) ?? true),
            priority,
            exclusive,
            adminRequired
            )
        {
            Hide = true,
        };
    }

    public CommandBase<MessageReceivedEventArgsBase>[] Commands => [StarterCommand, HandlerCommand];
}
