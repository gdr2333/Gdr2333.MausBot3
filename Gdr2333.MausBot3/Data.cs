// Copyright 2025, df1050 and the Gdr2333.MausBot3 contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Gdr2333.MausBot3.InternalPlugins;
using Gdr2333.MausBot3.PluginSdk;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gdr2333.MausBot3;

internal class Data : IInternalData, ISharedData
{
    [JsonRequired]
    public Uri Address { get; set; }

    public string? AccessToken { get; set; }

    [JsonRequired]
    public List<BlockRole> GlobalBlockRoles { get; set; }

    [JsonRequired]
    public Dictionary<string, List<BlockRole>> CommandBlockRoles { get; set; }

    [JsonRequired]
    public Dictionary<string, CommandAlias> CommandAliases { get; set; }

    [JsonRequired]
    public List<long> Admins { get; set; }

    [JsonIgnore]
    public ReaderWriterLockSlim GlobalLock { get; set; } = new();

    [JsonRequired]
    public string Prompt { get; set; }

    ReaderWriterLockSlim ISharedData.ReaderWriterLock => GlobalLock;

    public void Save()
    {
        var jsonso = new JsonSerializerOptions() { WriteIndented = true };
        jsonso.Converters.Add(new JsonStringEnumConverter());
        using var stream = new FileStream("Config.json", FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(stream, this, jsonso);
        stream.Flush();
    }
}
