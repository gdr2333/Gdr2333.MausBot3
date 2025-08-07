// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Messages;
using Timer = System.Timers.Timer;

namespace Gdr2333.MausBot3.PluginSdk;

internal class SessionData : IDisposable
{
    public MessagePipe MessagePipe { get; private set; }

    internal CancellationTokenSource CancellationTokenSource { get; } = new();

    internal Timer Watchdog { get; private set; }

    public Func<Message, CancellationToken, Task> SendMessage { get; set; }

    public SessionData(Action removeSession, Func<Message, CancellationToken, Task> sendMessage)
    {
        SendMessage = sendMessage;
        MessagePipe = new(ResetWatchdog, (m, ct) => SendMessage(m, ct));
        Watchdog = new(TimeSpan.FromMinutes(10));
        Watchdog.Elapsed += (_, _) =>
        {
            CancellationTokenSource.Cancel();
            removeSession();
            SessionEnded();
        };
        Watchdog.AutoReset = false;
        Watchdog.Start();
    }

    public void ResetWatchdog()
    {
        Watchdog.Stop();
        Watchdog.Start();
    }

    public void SessionEnded()
    {
        CancellationTokenSource.Cancel();
        Watchdog.Stop();
        Dispose();
    }

    public void Dispose()
    {
        CancellationTokenSource.Cancel();
        Watchdog.Dispose();
    }
}
