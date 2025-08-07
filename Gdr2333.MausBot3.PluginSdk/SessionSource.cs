// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.PluginSdk;

internal record struct SessionSource(
    long BotId,
    long GroupId,
    long TargetId
);
