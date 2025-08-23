// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Messages;
using Gdr2333.MausBot3.PluginSdk;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace Gdr2333.MausBot3.InternalPlugins;

internal class AdminPlugin(IInternalData data, ILoggerFactory loggerFactory, IList<CommandHelper> commands) : Plugin
{
    public override string PluginId => "Gdr2333.MausBot3.InternalPlugins.Admin";

    public override string PluginName => "MausBot3 - 内置管理插件";

    private ILogger _logger = loggerFactory.CreateLogger<AdminPlugin>();

    public override CommandBase[] Commands =>
        [
            new StandardCommand(
                "添加管理员",
                [],
                "添加指定用户到管理员组。用法：{命令前缀}添加管理员 [管理员QQ号|@新管理员]",
                "^{0}{1} +@?\\d+$",
                async (c, m, fuck0) =>
                {
                    long target;
                    if(m.Message[1] is AtPart at)
                        target = at.UserId;
                    else
                        target = long.Parse(m.Message.ToString().Split(' ')[^1]);
                    string res = "";
                    try
                    {
                        data.GlobalLock.EnterUpgradeableReadLock();
                        if(data.Admins.Contains(target))
                            res = $"{target}已经是管理员了！";
                        else
                        {
                            try
                            {
                                data.GlobalLock.EnterWriteLock();
                                data.Admins.Add(target);
                                data.Save();
                            }
                            finally
                            {
                                data.GlobalLock.ExitWriteLock();
                            }
                            res = $"添加了{target}到管理员列表";
                        }
                    }
                    finally
                    {
                        data.GlobalLock.ExitUpgradeableReadLock();
                    }
                    await c.SendMessageAsync(m, new(res));
                },
                null, 10, true),
            .. new CommandEx(
                "成为管理员",
                [],
                "将当前用户设置为管理员（当然要验证码）。用法：{命令前缀}添加管理员",
                "^{0}{1}$",
                async (mp, se, ct) =>
                {
                    bool isAdmin = false;
                    try
                    {
                        data.GlobalLock.EnterReadLock();
                        isAdmin = data.Admins.Contains(se.UserId);
                    }
                    finally
                    {
                        data.GlobalLock.ExitReadLock();
                    }
                    if(isAdmin)
                    {
                        await mp.SendMessageAsync(new([new TextPart("您已经是管理员了！")]), ct);
                        return;
                    }
                    var buf = new byte[10];
                    Random.Shared.NextBytes(buf);
                    var capcha = Convert.ToHexString(buf);
                    _logger.LogInformation($"管理员验证码是{capcha}");
                    await mp.SendMessageAsync(new([new TextPart($"用户{se.UserId}，请输入验证码：")]));
                    var capchaInput = (await mp.ReadMessageAsync(ct)).ToString();
                    if(capchaInput == capcha)
                    {
                        try
                        {
                            data.GlobalLock.EnterWriteLock();
                            data.Admins.Add(se.UserId);
                            data.Save();
                        }
                        finally
                        {
                            data.GlobalLock.ExitWriteLock();
                        }
                        await mp.SendMessageAsync(new([new TextPart("添加管理员成功！")]), ct);
                    }
                    else
                        await mp.SendMessageAsync(new([new TextPart("管理员验证码错误！")]), ct);
                }
                ).Commands,
            new StandardCommand(
                "搜索指令",
                [],
                "搜索符合条件的指令。用法：{指令前缀}搜索指令[关键词]",
                "^{0}{1}\\s?(?<keyword>.*)$",
                async (c, e, m) =>
                {
                    (string Id, string Name, string[] Alias)[] cmds = Array.ConvertAll<CommandHelper, (string Id, string Name, string[] Alias)>([..commands], (cmd) => (cmd.Id, cmd.Command.CommandName, cmd.Command.CommandAlias));
                    var keyword = m.Groups["keyword"];
                    StringBuilder sb = new();
                    {
                        var equName = from cmdinf in cmds where cmdinf.Name == keyword.ToString() select cmdinf;
                            if(equName.Any())
                            {
                                sb.AppendLine("=====名称，精准匹配=====");
                                foreach(var cmd in equName)
                                    sb.AppendLine($"{cmd.Name}，全称为{cmd.Id}");
                            }
                    }
                    {
                        var inName = from cmdinf in cmds where cmdinf.Name.Contains(keyword.ToString()) select cmdinf;
                        if(inName.Any())
                        {
                            sb.AppendLine("=====名称，模糊匹配=====");
                            foreach(var cmd in inName)
                                sb.AppendLine($"{cmd.Name}，全称为{cmd.Id}");
                        }
                    }
                    {
                        var equId = from cmdinf in cmds where cmdinf.Id == keyword.ToString() select cmdinf;
                        if(equId.Any())
                        {
                            sb.AppendLine("=====标识符，精准匹配=====");
                            foreach (var cmd in equId)
                                sb.AppendLine($"{cmd.Name}，全称为{cmd.Id}");
                        }
                    }
                    {
                        var inId = from cmdinf in cmds where cmdinf.Id.Contains(keyword.ToString()) select cmdinf;
                        if(inId.Any())
                        {
                            sb.AppendLine("=====标识符，模糊匹配=====");
                            foreach(var cmd in inId)
                                sb.AppendLine($"{cmd.Name}，全称为{cmd.Id}");
                        }
                    }
                    {
                        var equAlias = from cmdinf in cmds where cmdinf.Alias.Contains(keyword.ToString()) select cmdinf;
                        if(equAlias.Any())
                        {
                            sb.AppendLine("=====别名=====");
                            foreach(var cmd in equAlias)
                                sb.AppendLine($"{cmd.Name}，全称为{cmd.Id}");
                        }
                    }
                    await c.SendMessageAsync(e, new(sb.ToString()));
                }),
            .. new CommandEx(
                "添加黑名单",
                [],
                "将用户或群添加到某个指令或群的黑名单。单行用法：{命令前缀}添加黑名单 [群/用户][群号] [可选：命令ID]",
                "^{0}{1}(\\s*(群|用户)\\s*\\d+\\s*\\S*)?$",
                async (mp, se, ct) =>
                {
                    bool isGroup;
                    long targetId;
                    string? targetCommand = null;
                    var rs = Regex.Match(se.Message.ToString(), "(?<targetType>群|用户)\\s*(?<targetId>\\d+)\\s*(?<targetCommand>\\S*)$");
                    if(rs.Success)
                    {
                        isGroup = rs.Groups["targetType"].Value == "群";
                        if(!long.TryParse(rs.Groups["targetId"].Value, out targetId))
                        {
                            await mp.SendMessageAsync(new($"无法解析的{(isGroup ? "群" : "用户")}"), ct);
                            return;
                        }
                        targetCommand = rs.Groups["targetCommand"].Value;
                    }
                    else
                    {
                        await mp.SendMessageAsync(new("你想将屏蔽规则用于什么目标？请输入\"群\"或者\"用户\""), ct);
                        var res0 = (await mp.ReadMessageAsync(ct)).ToString();
                        if(res0 != "群" && res0 != "用户")
                            goto WrongInput;
                        isGroup = res0 == "群";
                        await mp.SendMessageAsync(new($"你想将屏蔽规则用于什么{(isGroup ? "群" : "用户")}？请输入{(isGroup ? "群" : "用户")}的ID"), ct);
                        if(!long.TryParse((await mp.ReadMessageAsync(ct)).ToString(), out targetId))
                            goto WrongInput;
                        await mp.SendMessageAsync(new("你想将屏蔽规则用于什么指令？请输入指令的ID，输入\"<全局>\"来使该规则全局启用。"), ct);
                        (string Id, string Name, string[] Alias)[] cmds = Array.ConvertAll<CommandHelper, (string Id, string Name, string[] Alias)>([..commands], (cmd) => (cmd.Id, cmd.Command.CommandName, cmd.Command.CommandAlias));
                        bool hasres = false;
                        while(!hasres)
                        {
                            var res1 = await mp.ReadMessageAsync(ct);
                            switch(res1.ToString())
                            {
                                case "<全局>":
                                    targetCommand = null;
                                    hasres = true;
                                    break;
                                default:
                                    var cmdres = from cmdinf in cmds where cmdinf.Id == res1.ToString() select cmdinf;
                                    if(cmdres.Any())
                                    {
                                        targetCommand = cmdres.First().Id;
                                        hasres = true;
                                        break;
                                    }
                                    else
                                        goto WrongInput;
                            }
                        }
                    }
                    try
                    {
                        data.GlobalLock.EnterWriteLock();
                        if(string.IsNullOrWhiteSpace(targetCommand))
                            data.GlobalBlockRoles.Add(new(){ TargetId = targetId, TargetType = isGroup ? BlockRoleTargetType.Group : BlockRoleTargetType.User});
                        data.Save();
                    }
                    finally
                    {
                        data.GlobalLock.ExitWriteLock();
                    }
                    await mp.SendMessageAsync(new($"已经设置了对{(isGroup ? "群" : "用户")}{targetId}的{targetCommand ?? "全局"}禁用"));
                    return;
         WrongInput:await mp.SendMessageAsync(new("无法解析的输入，退出......"));
                    return;
                }, null, null, 0, false, true).Commands,
            new StandardCommand(
                "全局黑名单列表",
                [],
                "显示当前的全局黑名单。用法：{命令前缀}全局黑名单列表[可选：页数]",
                "^{0}{1}\\s*(?<page>\\d+)?$",
                async (c, e, m) =>
                {
                    // 奇怪的特性：if的括号里放out var的话后者的作用域并没有局限在if内，所以下面两行代码是合法的
                    if(!int.TryParse(m.Groups["page"].Value, out var page))
                        page = 1;
                    int totalPage;
                    string r = "";
                    try
                    {
                        data.GlobalLock.EnterReadLock();
                        totalPage = data.GlobalBlockRoles.Count / 100 + (data.GlobalBlockRoles.Count % 100 == 0 ? 0 : 1);
                        if(page > totalPage || page < 0)
                        {
                            r = $"页面不存在！当前只有{totalPage}页。";
                            return;
                        }
                        StringBuilder sb = new($"=====第{page}页，共{totalPage}页=====\n");
                        for(int i = (page - 1) * 100; i < int.Min(page * 100, data.GlobalBlockRoles.Count); i++)
                            sb.AppendLine($"{data.GlobalBlockRoles[i].TargetType switch{
                                 BlockRoleTargetType.Group => "群",
                                 BlockRoleTargetType.User => "用户",
                                 _ => ""
                            }}{data.GlobalBlockRoles[i].TargetId}");
                        r = sb.ToString();
                    }
                    finally
                    {
                        data.GlobalLock.ExitReadLock();
                    }
                    await c.SendMessageAsync(e, new(r));
                }, null, 0, false, true),
            new StandardCommand(
                "指令黑名单列表",
                [],
                "显示当前的指令黑名单。用法：{命令前缀}指令黑名单列表{三选一：[命令={命令限定名称}]，[用户={用户ID}]。[群={群号}]}",
                "^{0}{1}\\s?((((?:cmd|command|命令)=(?<cmdId>\\S+))|((?:user|uid|用户)=(?<userId>\\d+))|((?:group|gid|群)=(?<groupId>\\d+))))$",
                async (c, e, m) =>
                {
                    var cmd = m.Groups["cmdId"].Value;
                    var uid = string.IsNullOrEmpty(m.Groups["userId"].Value) ? (int?)null : int.Parse(m.Groups["userId"].Value);
                    var gid = string.IsNullOrEmpty(m.Groups["groupId"].Value) ? (int?)null : int.Parse(m.Groups["groupId"].Value);
                    StringBuilder sb = new();
                    try
                    {
                        data.GlobalLock.EnterReadLock();
                        foreach(var role in data.CommandBlockRoles)
                            foreach(var target in role.Value)
                                if(string.IsNullOrEmpty(cmd)
                                    || role.Key == cmd
                                    || !uid.HasValue
                                    || (target.TargetType == BlockRoleTargetType.User && target.TargetId == uid)
                                    || !gid.HasValue
                                    || (target.TargetType == BlockRoleTargetType.Group && target.TargetId == gid))
                                    sb.AppendLine($"指令{role.Key}，{target.TargetType switch{
                                        BlockRoleTargetType.Group => "群",
                                        BlockRoleTargetType.User => "用户",
                                        _ => ""
                                    }}{target.TargetId}");
                    }
                    finally
                    {
                        data.GlobalLock.ExitReadLock();
                    }
                    await c.SendMessageAsync(e, new(sb.ToString()));
                }, null, 0, false, true),
            new StandardCommand(
                "删除黑名单",
                [],
                "从黑名单中移除指定条目。用法：{命令前缀}删除黑名单 [指令ID 或 \"全局\"] [(用户/群)ID 或 \"所有\"]。例：删除黑名单 全局 用户114514",
                "^{0}{1}\\s+(?:全局|(?<cmdId>.+))\\s+(?:(?:(?<targetType>用户|群)(?<targetId>\\d+))|所有)$",
                async (c, e, res) =>
                {
                    var cid = res.Groups["cmdId"].Value;
                    var tid = string.IsNullOrEmpty(res.Groups["targetId"].Value) ? long.Parse(res.Groups["targetId"].Value) : (long?)null;
                    var tt = res.Groups["targetType"].Value switch
                    {
                        "用户" => BlockRoleTargetType.User,
                        "群" => BlockRoleTargetType.Group,
                        _ => (BlockRoleTargetType?)null
                    };
                    int removed = 0;
                    bool badCommandName = !string.IsNullOrEmpty(cid);
                    try
                    {
                        data.GlobalLock.EnterWriteLock();
                        if(!string.IsNullOrEmpty(cid))
                            if(tt.HasValue && tid.HasValue)
                                removed = data.GlobalBlockRoles.RemoveAll(r => r.TargetType == tt && r.TargetId == tid);
                            else
                            {
                                removed = data.GlobalBlockRoles.Count;
                                data.CommandBlockRoles.Clear();
                            }
                        else if(data.CommandBlockRoles.TryGetValue(cid, out var rules))
                        {
                            badCommandName = false;
                            if(tt.HasValue && tid.HasValue)
                                removed = rules.RemoveAll(r => r.TargetType == tt && r.TargetId == tid);
                            else
                            {
                                removed = rules.Count;
                                data.CommandBlockRoles.Remove(cid);
                            }
                        }
                        data.Save();
                    }
                    finally
                    {
                        data.GlobalLock?.ExitWriteLock();
                    }
                    if(badCommandName)
                        await c.SendMessageAsync(e, new("指令不存在！"));
                    else if(removed == 0)
                        await c.SendMessageAsync(e, new("找不到符合条件的规则！"));
                    else
                        await c.SendMessageAsync(e, new($"成功移除了{removed}条规则。"));
                }, null, 0, false, true),
            new StandardCommand(
                "添加别名",
                ["alias"],
                "添加指定命令的别名。用法：{命令前缀}添加别名 [命令ID] \"[别名]\"",
                "^{0}{1}\\s+(?<commandId>.+)\\s+\"(?<newAlias>.+)\"$",
                async (c, e, rs) =>
                {
                    var cid = rs.Groups["commandId"].Value;
                    var alias = rs.Groups["newAlias"].Value;
                    if(Array.Exists([..commands], cmd => cmd.Id == cid))
                    {
                        try
                        {
                            data.GlobalLock.EnterWriteLock();
                            if(!data.CommandAliases.ContainsKey(cid))
                                data.CommandAliases.Add(cid, new(){ UseDefaultAlias = true, UseDrfaultName = true, NewAliases = [] });
                            data.CommandAliases[cid].NewAliases.Add(alias);
                        }
                        finally
                        {
                            data.GlobalLock.ExitWriteLock();
                        }
                        await c.SendMessageAsync(e, new($"已经添加了命令{cid}的别名{alias}"));
                    }
                    else
                        await c.SendMessageAsync(e, new("指令不存在"));
                })
        ];
}
