using System;
using System.Collections.Generic;
using HarmonyLib;
using RuntimeIcons.Dependency;

namespace RuntimeIcons.Patches;

[HarmonyPatch]
public class StartOfRoundPatch
{
    internal static int AvailableRenders { get; set; } = 0;
    
    internal static readonly Dictionary<Item, Tuple<string,string>> ItemModMap = [];

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    private static void PrepareItemCache(StartOfRound __instance)
    {
        ItemModMap.Clear();
        
        if (LethalLibProxy.Enabled)
            LethalLibProxy.GetModdedItems(in ItemModMap);

        if (LethalLevelLoaderProxy.Enabled)
            LethalLevelLoaderProxy.GetModdedItems(in ItemModMap);

        foreach (var itemType in __instance.allItemsList.itemsList)
            ItemModMap.TryAdd(itemType, new Tuple<string, string>("Vanilla", ""));
        
    }
    
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
    private static void TrackNewRenders(StartOfRound __instance)
    {
        AvailableRenders = 1;
    }

}