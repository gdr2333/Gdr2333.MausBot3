// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;

namespace Gdr2333.MausBot3.PluginSdk;

public abstract class Command<T> : CommandBase
    where T : OnebotV11EventArgsBase
{
    public abstract string CommandName { get; }

    public abstract string[] CommandAilas { get; }

    public abstract string CommandDescription { get; }

    public abstract bool IsExclusiveHandler { get; }

    public abstract bool IsPassive { get; }

    public abstract bool CheckHandle(T message);

    public abstract void Handle(OnebotV11ClientBase client, T message);
}
