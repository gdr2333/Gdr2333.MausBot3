// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;
using System.Text.RegularExpressions;

namespace Gdr2333.MausBot3.PluginSdk;

public class StandardCommand(
    string name,
    string[] alias,
    string description,
    string regexFormat,
    Action<OnebotV11ClientBase, MessageReceivedEventArgsBase> handler,
    Func<MessageReceivedEventArgsBase, bool>? extraCheck = null,
    sbyte priority = 0,
    bool exclusive = false,
    bool adminRequired = false)
    : CommandBase<MessageReceivedEventArgsBase>
{
    private string CommandFormatRegex => regexFormat;

    private Regex[] _commandRegexes = [];

    public override sbyte Priority => priority;

    public override string CommandName => name;

    public override string[] CommandAlias => alias;

    public override string CommandDescription => description;

    public override bool IsExclusiveHandler => exclusive;

    public override bool AdminRequired => adminRequired;

    private readonly ReaderWriterLockSlim _commandRegexesRWLck = new();

    public void SetRealAlias(string prompt, string[] alias)
    {
        var tmp = Array.ConvertAll(alias, cmd => new Regex(string.Format(CommandFormatRegex, prompt, cmd)));
        try
        {
            _commandRegexesRWLck.EnterWriteLock();
            _commandRegexes = tmp;
        }
        finally
        {
            _commandRegexesRWLck.ExitWriteLock();
        }
    }

    public bool ExtraCheck(MessageReceivedEventArgsBase message) =>
        extraCheck?.Invoke(message) ?? true;

    public override bool CheckHandle(MessageReceivedEventArgsBase message)
    {
        try
        {
            _commandRegexesRWLck.EnterReadLock();
            return Array.Exists(_commandRegexes, regex => regex.IsMatch(message.Message.ToString())) && ExtraCheck(message);
        }
        finally
        {
            _commandRegexesRWLck.ExitReadLock();
        }
    }

    public override void Handle(OnebotV11ClientBase client, MessageReceivedEventArgsBase message) =>
        handler(client, message);
}
