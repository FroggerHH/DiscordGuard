using System;
using HarmonyLib;
using YamlDotNet.Serialization;

namespace DiscordMessenger;

[HarmonyPatch]
[Serializable]
public class Provider
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [YamlMember(Alias = "url")]
    public string Url { get; set; }
}