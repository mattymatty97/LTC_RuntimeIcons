using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx;
using HarmonyLib;
using RuntimeIcons.Components;
using RuntimeIcons.Utils;
using UnityEngine;
using VertexLibrary;
using Object = UnityEngine.Object;

namespace RuntimeIcons.Patches;

[HarmonyPatch]
internal static class GrabbableObjectPatch
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
            var rotation = Quaternion.Euler(grabbableObject.itemProperties.restingRotation.x, grabbableObject.itemProperties.floorYOffset + 90f, grabbableObject.itemProperties.restingRotation.z);
            
            RuntimeIcons.CameraStage.SetObjectOnStage(grabbableObject.NetworkObject.gameObject);
            
            RuntimeIcons.CameraStage.CenterObjectOnPivot(rotation);

            RuntimeIcons.CameraStage.FindOptimalRotation(grabbableObject);
                
            RuntimeIcons.CameraStage.PrepareCameraForShot();

            var texture = RuntimeIcons.CameraStage.TakeSnapshot();

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
            RuntimeIcons.CameraStage.ResetStage();
        }
    }
    
    public static void FindOptimalRotation(this StageComponent stage, GrabbableObject grabbable)
    {
        var pivotTransform = stage.PivotTransform;
        
        if (PluginConfig.RotationOverrides.TryGetValue(grabbable.itemProperties.itemName,
                out var rotations))
        {
            pivotTransform.Rotate(rotations, Space.World);
        }
        else
        {
            pivotTransform.rotation = Quaternion.identity;
            var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            
            var executionOptions = new ExecutionOptions()
            {
                VertexCache = stage.VertexCache,
                CullingMask = stage.CullingMask,
                LogHandler = RuntimeIcons.VerboseMeshLog,
                OverrideMatrix = matrix
            };
            
            if (!pivotTransform.TryGetBounds(out var bounds, executionOptions))
                throw new InvalidOperationException("This object has no Renders!");

            if (bounds.size == Vector3.zero)
                throw new InvalidOperationException("This object has no Bounds!");

            if (bounds.extents.y < bounds.extents.x / 2f && bounds.extents.y <  bounds.extents.z / 2f)
            {
                RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated -75 x");
                pivotTransform.Rotate(Vector3.right, -75, Space.World);

                if (bounds.extents.z < bounds.extents.x * 0.5f)
                {
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated 45 z | 1");
                    pivotTransform.Rotate(Vector3.forward, 45, Space.World);
                }
                else if (bounds.extents.z < bounds.extents.x * 0.85f)
                {
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated 90 z | 1");
                    pivotTransform.Rotate(Vector3.forward, 90, Space.World);
                }
                else if (bounds.extents.x < bounds.extents.z * 0.5f)
                {
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated 90 z | 2");
                    pivotTransform.Rotate(Vector3.forward, 45, Space.World);
                }
            }
            else
            {
                RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated -25 x");
                pivotTransform.Rotate(Vector3.right, -25, Space.World);

                if (bounds.extents.x < bounds.extents.z * 0.85f)
                {
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated -45 y");
                    pivotTransform.Rotate(Vector3.up, -45, Space.World);
                }
                else if ((bounds.extents.y - bounds.extents.z) / Math.Abs(bounds.extents.z) < 0.01f)
                {
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated 25 x");
                    pivotTransform.Rotate(Vector3.right, 25, Space.World);
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated 25 z");
                    pivotTransform.Rotate(Vector3.forward, 25, Space.World);
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated -45 y");
                    pivotTransform.Rotate(Vector3.up, -45, Space.World);
                }
                else if (bounds.extents.y < bounds.extents.x / 2f || bounds.extents.x < bounds.extents.y / 2f)
                {
                    RuntimeIcons.Log.LogDebug($"{grabbable.itemProperties.itemName} rotated 45 z");
                    pivotTransform.Rotate(Vector3.forward, 45, Space.World);
                }
            }
        }

        RuntimeIcons.Log.LogInfo($"Stage rotation {pivotTransform.localRotation.eulerAngles}");
    }

    private static void UpdateIconsInHUD(Item item)
    {
        if (!GameNetworkManager.Instance || !GameNetworkManager.Instance.localPlayerController)
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