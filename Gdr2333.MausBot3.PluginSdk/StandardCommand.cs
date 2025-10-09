// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;
using System.Text.RegularExpressions;

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 标准文本指令默认包装
/// </summary>
/// <param name="name">指令主要名称</param>
/// <param name="alias">指令别名</param>
/// <param name="description">指令描述</param>
/// <param name="regexFormat">正则表达式生成器——{0}作为命令前缀，{1}作为命令名称</param>
/// <param name="handler">指令执行函数</param>
/// <param name="extraCheck">输入额外检查</param>
/// <param name="priority">指令优先级</param>
/// <param name="exclusive">指令是否会独占输入</param>
/// <param name="adminRequired">指令是否只能由管理员调用</param>
public class StandardCommand(
    string name,
    string[] alias,
    string description,
    string regexFormat,
    Func<OnebotV11ClientBase, MessageReceivedEventArgsBase, Match, Task> handler,
    Func<MessageReceivedEventArgsBase, bool>? extraCheck = null,
    sbyte priority = 0,
    bool exclusive = false,
    bool adminRequired = false)
    : CommandBase<MessageReceivedEventArgsBase>
{
    private string CommandFormatRegex => regexFormat;

    private Regex[] _commandRegexes = [];

    /// <inheritdoc/>
    public override sbyte Priority => priority;

    /// <inheritdoc/>
    public override string CommandName => name;

    /// <inheritdoc/>
    public override string[] CommandAlias => alias;

    /// <inheritdoc/>
    public override string CommandDescription => description;

    /// <inheritdoc/>
    public override bool IsExclusiveHandler => exclusive;

    /// <inheritdoc/>
    public override bool AdminRequired => adminRequired;

    private readonly ReaderWriterLockSlim _commandRegexesRWLck = new();

    /// <summary>
    /// <strong>不应在外部调用</strong>设置指令实际别名
    /// </summary>
    /// <param name="prompt">指令前缀</param>
    /// <param name="alias">指令别名列表</param>
    public void SetRealAlias(string prompt, string[] alias)
    {
        var tmp = Array.ConvertAll(alias, cmd => new Regex(string.Format(CommandFormatRegex, prompt, cmd), RegexOptions.Compiled));
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

    /// <inheritdoc/>
    public override bool CheckHandle(MessageReceivedEventArgsBase message) =>
        throw new InvalidOperationException($"文本命令类不可以调用{nameof(CheckHandle)}，请改为使用{nameof(CheckHandleEx)}。");

    // 我hack了你的IDE（不是）
    /// <inheritdoc cref="M:Gdr2333.MausBot3.PluginSdk.CommandBase`1.CheckHandle(`0)"/>
    public Match? CheckHandleEx(MessageReceivedEventArgsBase message)
    {
        var msg = message.Message.ToString();
        try
        {
            _commandRegexesRWLck.EnterReadLock();
            foreach(var regex in _commandRegexes)
            {
                var res = regex.Match(msg);
                if (res.Success && (extraCheck?.Invoke(message) ?? true))
                    return res;
            }
            return null;
        }
        finally
        {
            _commandRegexesRWLck.ExitReadLock();
        }
    }

    /// <inheritdoc/>
    public override Task Handle(OnebotV11ClientBase client, MessageReceivedEventArgsBase message) =>
        throw new InvalidOperationException($"文本命令类不可以调用{nameof(Handle)}，请改为使用{nameof(HandleEx)}。");

    public Task HandleEx(OnebotV11ClientBase client, MessageReceivedEventArgsBase message, Match match) =>
        handler(client, message, match);
}
