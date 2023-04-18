using System;
using HarmonyLib;
using YamlDotNet.Serialization;

namespace DiscordMessenger;

[HarmonyPatch]
[Serializable]
public class Field
{
    [YamlMember(Alias = "name")] public string Key { get; set; }

    [YamlMember(Alias = "value")] public string Value { get; set; }

    [YamlMember(Alias = "inline")] public bool Inline { get; set; }
}