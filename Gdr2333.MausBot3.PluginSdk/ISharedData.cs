// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Gdr2333.MausBot3.PluginSdk;

/// <summary>
/// 共享数据内容
/// </summary>
public interface ISharedData
{
    /// <summary>
    /// 读写锁
    /// </summary>
    public ReaderWriterLockSlim ReaderWriterLock { get; }

    // 没想好写什么，鸽着吧
}
