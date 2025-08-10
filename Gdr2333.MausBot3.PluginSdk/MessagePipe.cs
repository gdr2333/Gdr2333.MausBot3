// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Messages;
using System.Threading.Channels;

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 消息管线
/// </summary>
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

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>消息内容</returns>
    public async Task<Message> ReadMessageAsync(CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= default;
        ResetWatchdog();
        return await ReadChannel.Reader.ReadAsync(cancellationToken.Value);
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task SendMessageAsync(Message message, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= default;
        ResetWatchdog();
        await _sender(message, cancellationToken.Value);
    }
}
