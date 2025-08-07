// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;

namespace Gdr2333.MausBot3.PluginSdk;

public abstract class CommandBase<T> : CommandBase
    where T : OnebotV11EventArgsBase
{
    public abstract bool CheckHandle(T message);

    public override bool CheckHandle(OnebotV11EventArgsBase message) =>
        CheckHandle((T)message);

    public abstract void Handle(OnebotV11ClientBase client, T message);

    public override void Handle(OnebotV11ClientBase client, OnebotV11EventArgsBase message) =>
        Handle(client, (T)message);
}
