// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;

namespace Gdr2333.MausBot3.PluginSdk;

public abstract class CommandBase
{
    public abstract string CommandName { get; }

    public abstract string[] CommandAlias { get; }

    public abstract string CommandDescription { get; }

    public abstract bool IsExclusiveHandler { get; }

    public abstract sbyte Priority { get; }

    public abstract bool AdminRequired { get; }

    public bool IsGenerated { get; protected internal set; } = false;

    public abstract bool CheckHandle(OnebotV11EventArgsBase message);

    public abstract void Handle(OnebotV11ClientBase client, OnebotV11EventArgsBase message);
}
