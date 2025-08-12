// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Messages;
using Gdr2333.MausBot3.PluginSdk;
using Microsoft.Extensions.Logging;

namespace Gdr2333.MausBot3.InternalPlugins;

public class AdminPlugin(IInternalData data, ILoggerFactory loggerFactory) : Plugin
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
        ];
}
