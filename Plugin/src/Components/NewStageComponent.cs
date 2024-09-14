using System;
using System.Collections.Generic;
using System.Linq;
using MattyFixes.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RuntimeIcons.Components;

public class NewStageComponent : MonoBehaviour
{

    private NewStageComponent(){}

    private GameObject LightGo { get; set; }

    private GameObject PivotGo
    {
        get
        {
            if (!_targetGo)
                _targetGo = CreatePivotGo();
            return _targetGo;
        }
    }

    private GameObject CameraGo { get; set; }

    private Transform LightTransform => LightGo.transform;
    private Transform PivotTransform => PivotGo.transform;
    private Transform CameraTransform => CameraGo.transform;
    
    private Camera _camera;
    private HDAdditionalCameraData _cameraSettings;
    private CustomPassThing _cameraPass;
    private GameObject _targetGo;


    public Vector2Int Resolution { get; set; } = new Vector2Int(128, 128);
    public GrabbableObject StagedObject { get; private set; }
    public Transform StagedTransform { get; private set; }
    
    public TransformMemory Memory { get; private set; }

    private GameObject CreatePivotGo()
    {
        //add the StageTarget
        var targetGo = new GameObject($"{transform.name}.Pivot")
        {
            transform =
            {
                position = transform.position,
                rotation = transform.rotation
            }
        };
        return targetGo;
    }

    public static NewStageComponent CreateStage(HideFlags hideFlags, int layerMask = 1,string stageName = "Stage")
    {
        
        //create the root Object for the Stage
        var stageGo = new GameObject(stageName)
        {
            hideFlags = hideFlags
        };

        //add the component to the stage
        var stageComponent = stageGo.AddComponent<NewStageComponent>();

        //add the stage Lights
        
        var lightsGo = new GameObject("Stage Lights")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = stageGo.transform
            }
        };
        //disable the lights by default
        lightsGo.SetActive(false);
        stageComponent.LightGo = lightsGo;
        
        //add ceiling light!
        var lightGo1 = new GameObject("SpotLight 1")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = lightsGo.transform,
                localPosition = new Vector3(0, 4, 0),
                rotation = Quaternion.LookRotation(Vector3.down)
            }
        };

        var light = lightGo1.AddComponent<Light>();
        light.type = LightType.Spot;
        light.shape = LightShape.Cone;
        light.color = Color.white;
        light.colorTemperature = 6901;
        light.useColorTemperature = true;
        light.shadows = LightShadows.None;
        light.intensity = 250f;
        light.spotAngle = 160.0f;
        light.innerSpotAngle = 21.8f;
        light.range = 7.11f;
        
        var lightData = lightGo1.AddComponent<HDAdditionalLightData>();
        lightData.affectDiffuse = true;
        lightData.affectSpecular = true;
        lightData.affectsVolumetric = true;
        lightData.applyRangeAttenuation = true;
        lightData.color = Color.white;
        lightData.colorShadow = true;
        lightData.customSpotLightShadowCone = 30f;
        lightData.distance = 150000000000;
        lightData.fadeDistance = 10000;
        lightData.innerSpotPercent = 82.7f;
        lightData.intensity = 1000f;
        
        // add front light ( similar to ceiling one but does not have Specular )
        var lightGo2 = new GameObject("SpotLight 2")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = lightsGo.transform,
                localPosition = new Vector3(0, 0, -4),
                rotation = Quaternion.LookRotation(Vector3.forward)
            }
        };

        var light2 = lightGo2.AddComponent<Light>();
        light2.type = LightType.Spot;
        light2.shape = LightShape.Cone;
        light2.color = Color.white;
        light2.colorTemperature = 6901;
        light2.useColorTemperature = true;
        light2.shadows = LightShadows.None;
        light2.intensity = 250f;
        light2.spotAngle = 160.0f;
        light2.innerSpotAngle = 21.8f;
        light2.range = 7.11f;
        
        var lightData2 = lightGo2.AddComponent<HDAdditionalLightData>();
        lightData2.affectDiffuse = true;
        lightData2.affectSpecular = false;
        lightData2.affectsVolumetric = true;
        lightData2.applyRangeAttenuation = true;
        lightData2.color = Color.white;
        lightData2.colorShadow = true;
        lightData2.customSpotLightShadowCone = 30f;
        lightData2.distance = 150000000000;
        lightData2.fadeDistance = 10000;
        lightData2.innerSpotPercent = 82.7f;
        lightData2.intensity = 1000f;
        
        //add Camera
        var cameraGo = new GameObject("Camera")
        {
            hideFlags = hideFlags,
            transform =
            {
                parent = stageGo.transform
            }
        };
        stageComponent.CameraGo = cameraGo;

        // Add a Camera component to the GameObject
        var cam = cameraGo.AddComponent<Camera>();
        stageComponent._camera = cam;

        // Configure the Camera
        cam.cullingMask = layerMask;
        cam.orthographic = true;
        cam.orthographicSize = 1;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 10f;
        cam.enabled = false;
        
        // Add a Camera component to the GameObject
        HDAdditionalCameraData camData = cameraGo.AddComponent<HDAdditionalCameraData>();
        stageComponent._cameraSettings = camData;

        camData.clearDepth = true;
        camData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        camData.backgroundColorHDR = Color.clear;
        camData.customRenderingSettings = true;
        camData.customRenderingSettings = true;
        var overrideMask = camData.renderingPathCustomFrameSettingsOverrideMask;
        overrideMask.mask[(uint)FrameSettingsField.DecalLayers] = true;
        camData.renderingPathCustomFrameSettingsOverrideMask = overrideMask;
        camData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.DecalLayers, false);

        var customPassVolume = cameraGo.AddComponent<CustomPassVolume>();
        customPassVolume.targetCamera = cam;
        
        var customPass = (CustomPassThing)customPassVolume.AddPassOfType<CustomPassThing>();
        stageComponent._cameraPass = customPass;
        
        customPass.targetColorBuffer = CustomPass.TargetBuffer.Custom;
        customPass.targetDepthBuffer = CustomPass.TargetBuffer.Custom;
        customPass.clearFlags = ClearFlag.All;
        
        return stageComponent;
    }
    
    

    public void SetItemOnStage(GrabbableObject grabbableObject)
    {
        if (StagedObject && StagedObject != grabbableObject)
            throw new InvalidOperationException("An Object is already on stage!");
        
        RuntimeIcons.Log.LogInfo($"Setting stage for {grabbableObject.itemProperties.itemName}");
        
        PivotTransform.position = transform.position;
        PivotTransform.rotation = Quaternion.identity;
        
        StagedObject = grabbableObject;

        var objectToAdjust = grabbableObject.NetworkObject.gameObject;
        
        StagedTransform = objectToAdjust.transform;

        var rotation = Quaternion.Euler(grabbableObject.itemProperties.restingRotation.x, grabbableObject.itemProperties.floorYOffset + 90f, grabbableObject.itemProperties.restingRotation.z);
        
        var matrix = Matrix4x4.TRS(Vector3.zero, rotation, StagedTransform.localScale);
        if (!VerticesExtensions.TryGetBounds(objectToAdjust, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");

        Memory = new TransformMemory(StagedTransform);
        
        StagedTransform.SetParent(PivotTransform, false);
        StagedTransform.SetLocalPositionAndRotation(-bounds.center, rotation);
        
        RuntimeIcons.Log.LogInfo($"Stage Anchor offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
    }

    public void SetObjectOnStage(GameObject gameObject)
    {
        SetObjectOnStage(gameObject.transform);
    }
    
    public void SetObjectOnStage(Transform transform)
    {
        if (StagedTransform && StagedTransform != transform)
            throw new InvalidOperationException("An Object is already on stage!");
        
        RuntimeIcons.Log.LogInfo($"Setting stage for {transform.name}");
        
        PivotTransform.localPosition = Vector3.zero;
        PivotTransform.rotation = Quaternion.identity;
        
        StagedTransform = transform;
        
        var matrix = Matrix4x4.TRS(Vector3.zero, transform.rotation, transform.localScale);
        if (!VerticesExtensions.TryGetBounds(transform.gameObject, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");

        Memory = new TransformMemory(StagedTransform);
        
        StagedTransform.SetParent(PivotTransform, false);
        StagedTransform.localPosition = -bounds.center;
        
        RuntimeIcons.Log.LogInfo($"Stage Anchor offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
    }

    public void FindOptimalRotation()
    {
        if (!StagedObject)
            throw new InvalidOperationException("No Object on stage!");
        

        if (PluginConfig.RotationOverrides.TryGetValue(StagedObject.itemProperties.itemName,
                out var rotations))
        {
            PivotTransform.Rotate(rotations, Space.World);
        }
        else
        {
            /*if (StagedObject.itemProperties.twoHandedAnimation)
               PivotTransform.Rotate(_camera.transform.up, 90, Space.World);*/

            var matrix = Matrix4x4.TRS(Vector3.zero, PivotTransform.rotation, Vector3.one);
            if (!VerticesExtensions.TryGetBounds(PivotGo, out var bounds, matrix))
                throw new InvalidOperationException("This object has no Renders!");

            if (bounds.size == Vector3.zero)
                throw new InvalidOperationException("This object has no Bounds!");

            if (bounds.extents.y < bounds.extents.x / 2f && bounds.extents.y <  bounds.extents.z / 2f)
            {
                RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -75 x");
                PivotTransform.Rotate(_camera.transform.right, -75, Space.World);

                if (bounds.extents.z < bounds.extents.x * 0.5f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated 45 z | 1");
                    PivotTransform.Rotate(_camera.transform.forward, 45, Space.World);
                }
                else if (bounds.extents.z < bounds.extents.x * 0.85f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated 90 z | 1");
                    PivotTransform.Rotate(_camera.transform.forward, 90, Space.World);
                }
                else if (bounds.extents.x < bounds.extents.z * 0.5f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated 90 z | 2");
                    PivotTransform.Rotate(_camera.transform.forward, 45, Space.World);
                }
            }
            else
            {
                RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -25 x");
                PivotTransform.Rotate(_camera.transform.right, -25, Space.World);

                if (bounds.extents.x < bounds.extents.z * 0.85f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -45 y");
                    PivotTransform.Rotate(_camera.transform.up, -45, Space.World);
                }
                else if ((bounds.extents.y - bounds.extents.z) / Math.Abs(bounds.extents.z) < 0.01f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated 25 x");
                    PivotTransform.Rotate(_camera.transform.right, 25, Space.World);
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated 25 z");
                    PivotTransform.Rotate(_camera.transform.forward, 25, Space.World);
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated -45 y");
                    PivotTransform.Rotate(_camera.transform.up, -45, Space.World);
                }
                else if (bounds.extents.y < bounds.extents.x / 2f || bounds.extents.x < bounds.extents.y / 2f)
                {
                    RuntimeIcons.Log.LogDebug($"{StagedObject.itemProperties.itemName} rotated 45 z");
                    PivotTransform.Rotate(_camera.transform.forward, 45, Space.World);
                }
            }
        }

        RuntimeIcons.Log.LogInfo($"Stage rotation {PivotTransform.localRotation.eulerAngles}");
    }
    

    public void FindOptimalOffsetAndScale()
    {
        if (!StagedTransform)
            throw new InvalidOperationException("No Object on stage!");
        
        var matrix = Matrix4x4.TRS(Vector3.zero, PivotTransform.rotation, Vector3.one);
        if (!VerticesExtensions.TryGetBounds(PivotGo, out var bounds, matrix))
            throw new InvalidOperationException("This object has no Renders!");
        
        if (bounds.size == Vector3.zero)
            throw new InvalidOperationException("This object has no Bounds!");
        
        // Calculate the scale factor needed to fit within the target area
        var pixelsPerUnit = Resolution.y / (2f * _camera.orthographicSize);
        var targetWorldArea = ((Vector2)Resolution) / pixelsPerUnit;

        // Calculate the scale factor considering the object's distance from the camera
        // also add some padding from the sides
        var scaleFactorX = (targetWorldArea.x - 0.2f) / bounds.size.x;
        var scaleFactorY = (targetWorldArea.y - 0.2f) / bounds.size.y;
        var scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);
        
        // Apply the calculated scale factor
        var scale = Vector3.one * scaleFactor;
        
        // Calculate the new bounds size after applying scale
        var scaledSize = Vector3.Scale(bounds.size, scale);
        var scaledCenter = Vector3.Scale(bounds.center, scale);

        // Calculate the desired Z position to prevent clipping
        var distanceToMove = Mathf.Max(scaledSize.z + _camera.nearClipPlane, 1f);

        // Calculate the offset in the camera's local space
        var localOffset = new Vector3(-scaledCenter.x, -scaledCenter.y, distanceToMove);
        
        RuntimeIcons.Log.LogInfo($"Stage offset {localOffset} scale {scale}");

        PivotTransform.position = transform.position + localOffset;
        PivotTransform.localScale = scale;
    }

    public void ResetStage()
    {
        if (StagedTransform)
        {
            StagedTransform.SetParent(Memory.Parent, false);

            StagedTransform.localScale = Memory.LocalScale;
            StagedTransform.SetLocalPositionAndRotation(Memory.LocalPosition,Memory.LocalRotation);
        }

        StagedObject = null;
        StagedTransform = null;
        Memory = default;
        
        PivotTransform.position = transform.position;
        PivotTransform.rotation = Quaternion.identity;
    }

    public Texture2D TakeSnapshot()
    {
        return TakeSnapshot(Color.clear);
    }
    
    public Texture2D TakeSnapshot(Color backgroundColor)
    {
        // Set the background color of the camera
        _camera.backgroundColor = backgroundColor;

        // Get a temporary render texture and render the camera
        var tempTexture = RenderTexture.GetTemporary(Resolution.x, Resolution.y, 8, RenderTextureFormat.ARGB32);
        var tempTexture2 = RenderTexture.GetTemporary(Resolution.x, Resolution.y, 8, RenderTextureFormat.ARGB32);
        _camera.targetTexture = tempTexture2;
        _cameraPass.targetTexture = tempTexture;
        using (new IsolateStageLights(gameObject))
        {
            //Turn on the stage Lights
            LightGo.SetActive(true);
            
            _camera.Render();
            
            //Turn off the stage Lights
            LightGo.SetActive(false);
        }

        // Activate the temporary render texture
        var previouslyActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = tempTexture;
        
        // Extract the image into a new texture without mipmaps
        var texture = new Texture2D(tempTexture.width, tempTexture.height, TextureFormat.ARGB32,  -1,false)
        {
            name = $"{nameof(RuntimeIcons)}.{StagedTransform.name}Texture"
        };
        
        texture.ReadPixels(new Rect(0, 0, tempTexture.width, tempTexture.height), 0, 0);
        texture.Apply();
        
        // Reactivate the previously active render texture
        RenderTexture.active = previouslyActiveRenderTexture;
        
        // Clean up after ourselves
        _camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(tempTexture2);
        _cameraPass.targetTexture = null;
        RenderTexture.ReleaseTemporary(tempTexture);
        
        RuntimeIcons.Log.LogInfo($"{texture.name} Rendered");
        // Return the texture
        return texture;
    }

    private class IsolateStageLights : IDisposable
    {
        private readonly HashSet<Light> _lightMemory;
        private readonly Color _ambientLight;
        
        public IsolateStageLights(GameObject stage)
        {
            _lightMemory = UnityEngine.Pool.HashSetPool<Light>.Get();

            _ambientLight = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.black;

            var localLights = stage.GetComponentsInChildren<Light>();

            var globalLights = FindObjectsOfType<Light>().Where(l => !localLights.Contains(l)).Where(l => l.enabled).ToArray();

            foreach (var light in globalLights)
            {
                light.enabled = false;
                _lightMemory.Add(light);
            }
        }

        public void Dispose()
        {
            RenderSettings.ambientLight = _ambientLight;
            
            foreach (var light in _lightMemory)
            {
                light.enabled = true;
            }

            UnityEngine.Pool.HashSetPool<Light>.Release(_lightMemory);
        }
    }

    public record struct TransformMemory
    {
        public readonly Transform Parent;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;

        public TransformMemory(Transform target)
        {
            this.Parent = target.parent;
            this.LocalPosition = target.localPosition;
            this.LocalRotation = target.localRotation;
            this.LocalScale = target.localScale;
        }
    }

}