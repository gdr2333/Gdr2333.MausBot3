// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace Gdr2333.MausBot3;

// 傻逼微软，讲个笑话：.NET是一个开源的跨平台技术
internal class PluginLoadingContext(string name) : AssemblyLoadContext(name, true), IDisposable
{
    readonly Dictionary<string, IntPtr> _nativeLibs = [];

    public void Dispose()
    {
        foreach (var libHandle in _nativeLibs)
            NativeLibrary.Free(libHandle.Value);
        Unload();
    }

    public void LoadNativeLib(string path)
    {
        var nativePath = $"{path}{Path.DirectorySeparatorChar}runtimes";
        if (Directory.Exists(nativePath))
            RealLoadNativeLib(nativePath);
        foreach (var i in Assemblies)
            try
            {
                NativeLibrary.SetDllImportResolver(i, DllImportResolver);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"无法为托管程序集{i.FullName}设置DLL加载解析器。由于默认P/Invoke解析器的局限性，这很可能导致原生程序集加载失败。\n如果你是这个托管程序集的作者，请不要为此程序集设置DllImportResolver（请参见https://learn.microsoft.com/zh-cn/dotnet/standard/native-interop/native-library-loading）。\n如果你不是作者并且遇到了DllNotFoundException，请尝试将所需程序集放置到本程序根目录下。");
            }
    }

    private void RealLoadNativeLib(string path)
    {
        foreach (var dir in Directory.GetDirectories(path))
            RealLoadNativeLib(dir);
        foreach (var lib in Directory.GetFiles(path))
            try
            {
                _nativeLibs.Add(lib.Split(Path.DirectorySeparatorChar)[^1], LoadUnmanagedDllFromPath(lib));
                Console.WriteLine($"已加载本机程序集{lib}");
            }
            catch (DllNotFoundException)
            {
            }
            catch (BadImageFormatException)
            {
            }
    }

    private IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (_nativeLibs.TryGetValue(libraryName, out var ptr))
            return ptr;
        else
            foreach (var lib in _nativeLibs)
                if (Regex.IsMatch(lib.Key, $"(?:lib)?(?:{libraryName})((.so)|(.dll)|(.dylib))?"))
                    return lib.Value;
        return NativeLibrary.Load(libraryName, assembly, searchPath);
    }
}