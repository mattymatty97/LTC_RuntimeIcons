using System;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RuntimeIcons.Components;
using UnityEngine;
using Object = System.Object;

namespace RuntimeIcons.Patches;

[HarmonyPatch]
internal class GrabbableObjectPatch
{

    private static Sprite BrokenSrpite;
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static void AfterStart(GrabbableObject __instance)
    {
        if (__instance.itemProperties.itemIcon.name != "ScrapItemIcon" &&
            __instance.itemProperties.itemIcon.name != "ScrapItemIcon2") 
            return;

        if (!BrokenSrpite)
        {
            BrokenSrpite = UnityEngine.Object.Instantiate(__instance.itemProperties.itemIcon);
            BrokenSrpite.name = $"{nameof(RuntimeIcons)}.ScrapItemIcon";
        }
        
        if (PluginConfig.Blacklist.Contains(__instance.itemProperties.itemName))
            return;
            
        ComputeSprite(__instance);
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
                        UnityEngine.Object.Destroy(texture);
                        RuntimeIcons.Log.LogError($"Expected Icon {filename} has the wrong format!");
                    }
                    else if (!IsTransparent(texture))
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
            RuntimeIcons.CameraStage.PrepareStageFor(grabbableObject);

            RuntimeIcons.CameraStage.FindOptimalRotationForCamera(RuntimeIcons.SnapshotCamera.cam);
                
            RuntimeIcons.CameraStage.FindOptimalOffsetAndScaleForCamera(RuntimeIcons.SnapshotCamera.cam,
                new Vector2(128, 128));

            var transform = RuntimeIcons.CameraStage.transform;
            var texture = RuntimeIcons.SnapshotCamera.TakeObjectSnapshot(RuntimeIcons.CameraStage.gameObject, 
                transform.localPosition, transform.rotation, transform.localScale);

            SnapshotCamera.SavePNG(texture, $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}",
                BepInEx.Paths.CachePath);

            if (!IsTransparent(texture))
            {
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(texture.width / 2f, texture.height / 2f));
                sprite.name = $"{nameof(RuntimeIcons)}.{grabbableObject.itemProperties.itemName}";
                grabbableObject.itemProperties.itemIcon = sprite;
            }
            else
            {
                RuntimeIcons.Log.LogError($"{grabbableObject.itemProperties.itemName} Generated Empty Sprite!");
                grabbableObject.itemProperties.itemIcon = BrokenSrpite;
            }
        }
        finally
        {
            RuntimeIcons.CameraStage.ResetStage();
        }
    }
    
    public static bool IsTransparent(Texture2D tex)
    {
        for (var x = 0; x < tex.width; x++)
        for (var y = 0; y < tex.height; y++)
            if (tex.GetPixel(x, y).a != 0)
                return false;
        return true;
    }
}