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
                async (c, m) =>
                {
                    long target;
                    if(m.Message[1] is AtPart at)
                        target = at.UserId;
                    else
                        target = long.Parse(m.Message.ToString().Split(' ')[^1]);
                    try
                    {
                        data.GlobalLock.EnterUpgradeableReadLock();
                        if(data.Admins.Contains(target))
                            await c.SendMessageAsync(m, new([new TextPart($"{target}已经是管理员了！")]));
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
                            await c.SendMessageAsync(m, new([new TextPart($"添加了{target}到管理员列表")]));
                        }
                    }
                    finally
                    {
                        data.GlobalLock.ExitUpgradeableReadLock();
                    }
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
                    var capchaInput = (await mp.ReadMessageAsync(ct)).ToString();
                    await mp.SendMessageAsync(new([new TextPart($"用户{se.UserId}，请输入验证码：")]));
                    if(capchaInput == capcha)
                    {
                        try
                        {
                            data.GlobalLock.EnterWriteLock();
                            data.Admins.Add(se.UserId);
                        }
                        finally
                        {
                            data.GlobalLock.ExitWriteLock();
                        }
                        await mp.SendMessageAsync(new([new TextPart("添加管理员成功！")]), ct);
                        data.Save();
                    }
                    else
                        await mp.SendMessageAsync(new([new TextPart("管理员验证码错误！")]), ct);
                }
                ).Commands,
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
                    var rs = Regex.Match(se.Message.ToString(), "(群|用户)\\s*(\\d+)\\s*(\\S*)$");
                    if(rs.Success)
                    {
                        isGroup = rs.Groups[0].Value == "群";
                        if(!long.TryParse(rs.Groups[1].Value, out targetId))
                        {
                            await mp.SendMessageAsync(new($"无法解析的{(isGroup ? "群" : "用户")}"), ct);
                            return;
                        }
                        targetCommand = rs.Groups[2].Value;
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
                        await mp.SendMessageAsync(new("你想将屏蔽规则用于什么指令？请输入指令的ID。输入\"<搜索>\"来寻找可以被屏蔽的指令，输入\"<全局>\"来使该规则全局启用。"), ct);
                        // 在用户输入内容的时候，让我们计算指令列表
                        (string Id, string Name, string[] Alias)[] cmds = Array.ConvertAll<CommandHelper, (string Id, string Name, string[] Alias)>([..commands], (cmd) => (cmd.Id, cmd.Command.CommandName, cmd.Command.CommandAlias));
                        bool hasres = false;
                        while(!hasres)
                        {
                            var res1 = await mp.ReadMessageAsync(ct);
                            switch(res1.ToString())
                            {
                                case "<搜索>":
                                    await mp.SendMessageAsync(new("请输入你要搜索的关键词："), ct);
                                    var keyword = await mp.ReadMessageAsync(ct);
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
                                            sb.AppendLine("=====标识符，模糊普配=====");
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
                                    await mp.SendMessageAsync(new(sb.ToString()));
                                    break;
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
                            data.GlobalBlockRoles.Add(new(){ Target = targetId, TargetType = isGroup ? BlockRoleTargetType.Group : BlockRoleTargetType.User});
                    }
                    finally
                    {
                        data.GlobalLock.ExitWriteLock();
                    }
                    await mp.SendMessageAsync(new($"已经设置了对{(isGroup ? "群" : "用户")}{targetId}的{targetCommand ?? "全局"}禁用"));
                    return;
         WrongInput:await mp.SendMessageAsync(new("无法解析的输入，退出......"));
                    return;
                }
                ).Commands
        ];
}
