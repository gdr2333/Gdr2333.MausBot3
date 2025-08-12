// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.InternalPlugins;

public interface IInternalData
{
    public List<BlockRole> GlobalBlockRoles { get; }

    public Dictionary<string, List<BlockRole>> CommandBlockRoles { get; }

    public Dictionary<string, CommandAlias> CommandAliases { get; }

    public List<long> Admins { get; }

    public ReaderWriterLockSlim GlobalLock { get; }

    public void Save();
}
