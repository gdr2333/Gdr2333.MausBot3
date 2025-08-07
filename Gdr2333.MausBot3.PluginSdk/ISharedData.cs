// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.PluginSdk;

public interface ISharedData
{
    public ReaderWriterLockSlim ReaderWriterLock { get; }

    // 没想好写什么，鸽着吧
}
