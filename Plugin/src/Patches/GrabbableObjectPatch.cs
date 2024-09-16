using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using RuntimeIcons.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RuntimeIcons.Patches;

[HarmonyPatch]
internal class GrabbableObjectPatch
{

    internal static Sprite BrokenSprite { get; set; }

    internal static bool ItemHasIcon(Item item)
    {
        if (item.itemIcon == null)
            return false;
        if (item.itemIcon.name == "ScrapItemIcon")
            return false;
        if (item.itemIcon.name == "ScrapItemIcon2")
            return false;
        return true;
    }

    private static ConditionalWeakTable<Item, GrabbableObject> _pendingObjects = [];
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static void AfterStart(GrabbableObject __instance)
    {
        if (ItemHasIcon(__instance.itemProperties)) 
            return;

        if (!BrokenSprite && __instance.itemProperties.itemIcon)
        {
            BrokenSprite = Object.Instantiate(__instance.itemProperties.itemIcon);
            BrokenSprite.name = $"{nameof(RuntimeIcons)}.ScrapItemIcon";
        }
        
        if (PluginConfig.Blacklist.Contains(__instance.itemProperties.itemName))
            return;
        
        if (_pendingObjects.TryGetValue(__instance.itemProperties, out var previousObject) && previousObject)
            return;

        _pendingObjects.AddOrUpdate(__instance.itemProperties, __instance);

        __instance.StartCoroutine(ComputeSpriteCoroutine(__instance));
    }

    private static IEnumerator ComputeSpriteCoroutine(GrabbableObject @this)
    {
        //wait two frames for the animations to settle
        yield return null;
        yield return null;
        ComputeSprite(@this);
        _pendingObjects.Remove(@this.itemProperties);
    }
    

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ComputeSprite(GrabbableObject grabbableObject)
    {
        RuntimeIcons.Log.LogWarning($"Computing {grabbableObject.itemProperties.itemName} icon");

        if (PluginConfig.FileOverrides.TryGetValue(grabbableObject.itemProperties.itemName,
                out var filename))
        {
            
            RuntimeIcons.Log.LogWarning($"Assigning {filename} to {grabbableObject.itemProperties.itemName}");
            try
            {
                if (File.Exists(filename))
                {
                    var fileData = File.ReadAllBytes(filename);
                    var texture = new Texture2D(128, 128);
                    texture.LoadImage(fileData);

                    if (texture.width != 128 || texture.height != 128)
                    {
                        Object.Destroy(texture);
                        RuntimeIcons.Log.LogError($"Expected Icon {filename} has the wrong format!");
                    }
                    else if (!texture.IsTransparent())
                    {
                        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            new Vector2(texture.width / 2f, texture.height / 2f));
                        sprite.name = $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}";
                        grabbableObject.itemProperties.itemIcon = sprite;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                RuntimeIcons.Log.LogError($"Failed to read {filename}:\n{ex}");
            }
            finally
            {
                RuntimeIcons.Log.LogWarning($"Fallback to Staged image for {grabbableObject.itemProperties.itemName}");
            }
        }

        try
        {
            RuntimeIcons.NewCameraStage.SetItemOnStage(grabbableObject);

            RuntimeIcons.NewCameraStage.FindOptimalRotation();
                
            RuntimeIcons.NewCameraStage.FindOptimalOffsetAndScale();

            var texture = RuntimeIcons.NewCameraStage.TakeSnapshot();

            texture.SavePNG($"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}",
                Path.Combine(Paths.CachePath, $"{nameof(RuntimeIcons)}.PNG"));
            
            texture.SaveEXR($"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}",
                Path.Combine(Paths.CachePath, $"{nameof(RuntimeIcons)}.EXR"));
            
            if (!texture.IsTransparent())
            {
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(texture.width / 2f, texture.height / 2f));
                sprite.name = $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}";
                grabbableObject.itemProperties.itemIcon = sprite;
            }
            else
            {
                RuntimeIcons.Log.LogError($"{grabbableObject.itemProperties.itemName} Generated Empty Sprite!");
                grabbableObject.itemProperties.itemIcon = BrokenSprite;
            }

            UpdateIconsInHUD(grabbableObject.itemProperties);
        }
        finally
        {
            RuntimeIcons.Log.LogInfo($"{grabbableObject.itemProperties.itemName} now has a new icon");
            //RuntimeIcons.CameraStage.ResetStage();
            RuntimeIcons.NewCameraStage.ResetStage();
        }
    }

    private static void UpdateIconsInHUD(Item item)
    {
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            return;

        var itemSlots = GameNetworkManager.Instance.localPlayerController.ItemSlots;
        var itemSlotIcons = HUDManager.Instance.itemSlotIcons;
        for (var i = 0; i < itemSlots.Length; i++)
        {
            if (i >= itemSlotIcons.Length)
                break;
            if (!itemSlots[i] || itemSlots[i].itemProperties != item)
                continue;
            itemSlotIcons[i].sprite = item.itemIcon;
        }
    }
}