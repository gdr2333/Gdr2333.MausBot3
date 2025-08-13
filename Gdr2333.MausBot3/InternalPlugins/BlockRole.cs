// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.InternalPlugins;

public struct BlockRole
{
    public required BlockRoleTargetType TargetType { get; set; }

    public required long TargetId { get; set; }
}

public enum BlockRoleTargetType
{
    Group,
    User
}
