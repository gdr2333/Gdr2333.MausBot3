// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.PluginSdk;

public abstract class Plugin
{
    public abstract CommandBase[] Commands { get; }

    public abstract string PluginId { get; }

    public abstract string PluginName { get; }
}
