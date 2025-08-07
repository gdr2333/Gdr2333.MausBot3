// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;

namespace Gdr2333.MausBot3.PluginSdk;

public class Command<T>(
    string name,
    string[] alias,
    string description,
    Action<OnebotV11ClientBase, T> handler,
    Func<T, bool> check,
    sbyte priority = 0,
    bool exclusive = false,
    bool adminRequired = false) 
    : CommandBase<T>
    where T : OnebotV11EventArgsBase
{
    public override string CommandName => name;

    public override string[] CommandAlias => alias;

    public override string CommandDescription => description;

    public override bool IsExclusiveHandler => exclusive;

    public override sbyte Priority => priority;

    public override bool AdminRequired => adminRequired;

    public bool Hide { get; internal set; } = false;

    public override bool CheckHandle(T message) =>
        check(message);

    public override void Handle(OnebotV11ClientBase client, T message) =>
        handler(client, message);
}
