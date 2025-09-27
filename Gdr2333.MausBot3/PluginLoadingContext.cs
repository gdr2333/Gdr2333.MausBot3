// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Gdr2333.MausBot3;

// 傻逼微软，讲个笑话：.NET是一个开源的跨平台技术
internal class PluginLoadingContext(string name) : AssemblyLoadContext(name), IDisposable
{
    readonly List<IntPtr> _nativeLibs = [];

    public void Dispose()
    {
        foreach (var libHandle in _nativeLibs)
            NativeLibrary.Free(libHandle);
        Unload();
    }

    public void LoadNativeLib(string path)
    {
        var nativePath = $"{path}{Path.DirectorySeparatorChar}runtimes";
        if (Directory.Exists(nativePath))
            RealLoadNativeLib(nativePath);
    }

    private void RealLoadNativeLib(string path)
    {
        foreach (var dir in Directory.GetDirectories(path))
            RealLoadNativeLib(dir);
        foreach (var lib in Directory.GetFiles(path))
            try
            {
                _nativeLibs.Add(LoadUnmanagedDllFromPath(lib));
                Console.WriteLine($"已加载本机程序集{lib}");
            }
            catch (DllNotFoundException)
            {
            }
            catch (BadImageFormatException)
            {
            }
    }
}