// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.InternalPlugins;

public class CommandAlias
{
    public required bool UseDefaultAlias { get; set; }

    public required bool UseDrfaultName { get; set; }

    public List<string> NewAliases { get; set; } = [];
}
