// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.BotLib.OnebotV11.Clients;
using Gdr2333.BotLib.OnebotV11.Events;
using Gdr2333.MausBot3.InternalPlugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Gdr2333.MausBot3;

internal class BotService(ReverseWebSocketClient client, Data data, PluginData plugins, ILogger<BotService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        client.OnEventOccurrence += (c, e) =>
        {
            try
            {
                logger.LogInformation($"接收到事件{JsonSerializer.Serialize(e)}");

                data.GlobalLock.EnterReadLock();
                var gid = e is IGroupEventArgs ge ? ge.GroupId : (long?)null;
                var uid = e is IUserEventArgs ue ? ue.UserId : (long?)null;

                foreach (var role in data.GlobalBlockRoles)
                {
                    switch (role.TargetType)
                    {
                        case BlockRoleTargetType.Group:
                            if (gid.HasValue && gid.Value == role.TargetId)
                                return;
                            break;
                        case BlockRoleTargetType.User:
                            if (uid.HasValue && uid.Value == role.TargetId)
                                return;
                            break;
                    }
                }

                foreach (var cmd in plugins.Comamnds)
                {
                    if (!cmd.TypeTest(e))
                        continue;
                    if (cmd.Command.AdminRequired && (!uid.HasValue || !data.Admins.Contains(uid.Value)))
                        continue;
                    if (data.CommandBlockRoles.TryGetValue(cmd.Id, out var roles))
                        foreach (var role in roles)
                            switch (role.TargetType)
                            {
                                case BlockRoleTargetType.Group:
                                    if (gid.HasValue && gid.Value == role.TargetId)
                                        goto NotThis;
                                    break;
                                case BlockRoleTargetType.User:
                                    if (uid.HasValue && uid.Value == role.TargetId)
                                        goto NotThis;
                                    break;
                            }
                    if (cmd.Command.CheckHandle(e))
                    {
                        logger.LogInformation($"事件被{(cmd.Command.IsExclusiveHandler ? "独占" : "非独占")}命令{cmd.Id}触发。");
                        Task.Run(() => cmd.Command.Handle(c, e));
                        if (cmd.Command.IsExclusiveHandler)
                            return;
                    }
                NotThis:;
                }
            }
            finally
            {
                data.GlobalLock.ExitReadLock();
            }
        };
        client.OnExceptionOccurrence += (c, e) =>
        {
            logger.LogWarning($"Onebot服务器异常：{e}");
        };
        await Task.Delay(-1, stoppingToken);
        client.Stop();
    }
}
