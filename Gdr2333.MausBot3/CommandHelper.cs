// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Events;
using Gdr2333.MausBot3.PluginSdk;

namespace Gdr2333.MausBot3;

internal class CommandHelper
{
    public string Id { get; init; }

    public Type WantEventType { get; init; }

    public bool TypeTest(OnebotV11EventArgsBase post) =>
        WantEventType.IsAssignableFrom(post.GetType());

    public CommandBase Command { get; init; }

    public CommandHelper(CommandBase cmd, Plugin src)
    {
        Id = $"{src.PluginId}::{cmd.CommandName}";
        Command = cmd;
        var type = cmd.GetType();
        while (type.GUID != typeof(CommandBase<>).GUID)
            type = type?.BaseType ?? throw new ArgumentException($"{Id}：插件不合法：没有继承自CommandBase<T>");
        WantEventType = type.GetGenericArguments()[0];
    }
}
