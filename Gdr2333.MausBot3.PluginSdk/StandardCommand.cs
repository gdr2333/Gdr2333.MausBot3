// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Events;
using System.Text.RegularExpressions;

namespace Gdr2333.MausBot3.PluginSdk;

public abstract class StandardCommand : Command<MessageReceivedEventArgsBase>
{
    protected abstract string CommandFormatRegex { get; }

    private Regex[] _commandRegexes = [];

    public override bool IsPassive => false;

    public void SetRealAlias(string prompt, string[] alias)
    {
        _commandRegexes = Array.ConvertAll(alias, cmd => new Regex(string.Format(CommandFormatRegex, prompt, cmd)));
    }

    public abstract bool ExtraCheck(MessageReceivedEventArgsBase message);

    public override bool CheckHandle(MessageReceivedEventArgsBase message) =>
        Array.Exists(_commandRegexes, regex => regex.IsMatch(message.Message.ToString())) && ExtraCheck(message);
}
