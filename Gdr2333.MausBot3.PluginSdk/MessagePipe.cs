// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Messages;
using System.Threading.Channels;

namespace Gdr2333.MausBot3.PluginSdk;

public class MessagePipe
{
    internal readonly Channel<Message> ReadChannel = Channel.CreateUnbounded<Message>();
    private readonly Action ResetWatchdog;
    private Func<Message,CancellationToken, Task> _sender;

    internal MessagePipe(Action resetWatchdog, Func<Message, CancellationToken, Task> sender)
    {
        ResetWatchdog = resetWatchdog;
        _sender = sender;
    }

    public async Task<Message> ReadMessageAsync(CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= default;
        ResetWatchdog();
        return await ReadChannel.Reader.ReadAsync(cancellationToken.Value);
    }

    public async Task SendMessageAsync(Message message, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= default;
        ResetWatchdog();
        await _sender(message, cancellationToken.Value);
    }
}
