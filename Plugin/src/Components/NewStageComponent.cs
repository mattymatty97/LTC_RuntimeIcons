using System;
using System.Collections.Generic;
using System.Linq;
using RuntimeIcons.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using VertexLibrary;

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
            layer = 1,
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
            layer = 1,
            transform =
            {
                parent = lightsGo.transform,
                localPosition = new Vector3(0, 3, 0),
                rotation = Quaternion.LookRotation(Vector3.down)
            }
        };

        var light = lightGo1.AddComponent<Light>();
        light.type = LightType.Spot;
        light.shape = LightShape.Cone;
        light.color = Color.white;
        light.colorTemperature = 6901;
        light.useColorTemperature = true;
        light.shadows = LightShadows.Hard;
        light.spotAngle = 50.0f;
        light.innerSpotAngle = 21.8f;
        light.range = 7.11f;
        
        var lightData = lightGo1.AddComponent<HDAdditionalLightData>();
        lightData.affectDiffuse = true;
        lightData.affectSpecular = true;
        lightData.affectsVolumetric = true;
        lightData.applyRangeAttenuation = true;
        lightData.color = Color.white;
        lightData.colorShadow = true;
        lightData.shadowDimmer = 0.8f;
        lightData.customSpotLightShadowCone = 30f;
        lightData.distance = 150000000000;
        lightData.fadeDistance = 10000;
        lightData.innerSpotPercent = 82.7f;
        lightData.intensity = 500f;
        
        // add front light ( similar to ceiling one but facing a 45 angle )
        var lightGo2 = new GameObject("SpotLight 2")
        {
            hideFlags = hideFlags,
            layer = 1,
            transform =
            {
                parent = lightsGo.transform,
                localPosition = new Vector3(-2.7f, 0, -2.7f),
                rotation = Quaternion.Euler(0, 45, 0)
            }
        };

        var light2 = lightGo2.AddComponent<Light>();
        light2.type = LightType.Spot;
        light2.shape = LightShape.Cone;
        light2.color = Color.white;
        light2.colorTemperature = 6901;
        light2.useColorTemperature = true;
        light2.shadows = LightShadows.Hard;
        light2.spotAngle = 50.0f;
        light2.innerSpotAngle = 21.8f;
        light2.range = 7.11f;
        
        var lightData2 = lightGo2.AddComponent<HDAdditionalLightData>();
        lightData2.affectDiffuse = true;
        lightData2.affectSpecular = true;
        lightData2.affectsVolumetric = true;
        lightData2.applyRangeAttenuation = true;
        lightData2.color = Color.white;
        lightData2.colorShadow = true;
        lightData2.shadowDimmer = 0.6f;
        lightData2.customSpotLightShadowCone = 30f;
        lightData2.distance = 150000000000;
        lightData2.fadeDistance = 10000;
        lightData2.innerSpotPercent = 82.7f;
        lightData2.intensity = 300f;
        lightData2.shapeRadius = 0.5f;
        
        // add a second front light ( similar to the other one but does not have Specular )
        var lightGo3 = new GameObject("SpotLight 3")
        {
            hideFlags = hideFlags,
            layer = 1,
            transform =
            {
                parent = lightsGo.transform,
                localPosition = new Vector3(2.7f, 0, -2.7f),
                rotation = Quaternion.Euler(0, -45, 0)
            }
        };

        var light3 = lightGo3.AddComponent<Light>();
        light3.type = LightType.Spot;
        light3.shape = LightShape.Cone;
        light3.color = Color.white;
        light3.colorTemperature = 6901;
        light3.useColorTemperature = true;
        light3.shadows = LightShadows.Hard;
        light3.spotAngle = 50.0f;
        light3.innerSpotAngle = 21.8f;
        light3.range = 7.11f;
        
        var lightData3 = lightGo3.AddComponent<HDAdditionalLightData>();
        lightData3.affectDiffuse = true;
        lightData3.affectSpecular = false;
        lightData3.affectsVolumetric = true;
        lightData3.applyRangeAttenuation = true;
        lightData3.color = Color.white;
        lightData3.colorShadow = true;
        lightData3.shadowDimmer = 0.4f;
        lightData3.customSpotLightShadowCone = 30f;
        lightData3.distance = 150000000000;
        lightData3.fadeDistance = 10000;
        lightData3.innerSpotPercent = 82.7f;
        lightData3.intensity = 75f;
        
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
        cam.aspect = 1;
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
        
        PivotTransform.parent = null;
        PivotTransform.position = transform.position;
        PivotTransform.rotation = Quaternion.identity;
        SceneManager.MoveGameObjectToScene(PivotGo, grabbableObject.gameObject.scene);
        
        StagedObject = grabbableObject;

        var objectToAdjust = grabbableObject.NetworkObject.gameObject;
        
        StagedTransform = objectToAdjust.transform;

        var rotation = Quaternion.Euler(grabbableObject.itemProperties.restingRotation.x, grabbableObject.itemProperties.floorYOffset + 90f, grabbableObject.itemProperties.restingRotation.z);
        
        var matrix = Matrix4x4.TRS(Vector3.zero, rotation, StagedTransform.localScale);
        
        var executionOptions = new ExecutionOptions()
        {
            VertexCache = VertexesExtensions.GlobalPartialCache,
            FilteredComponents = new HashSet<Type> { typeof(ScanNodeProperties) },
            LogHandler = RuntimeIcons.VerboseMeshLog,
            OverrideMatrix = matrix
        };
        
        if (!objectToAdjust.TryGetBounds(out var bounds, executionOptions))
            throw new InvalidOperationException("This object has no Renders!");

        Memory = new TransformMemory(StagedTransform);
        
        StagedTransform.SetParent(PivotTransform, false);
        StagedTransform.SetLocalPositionAndRotation(-bounds.center, rotation);
        
        RuntimeIcons.Log.LogInfo($"Stage Anchor offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
    }

    public void SetObjectOnStage(GameObject targetGameObject)
    {
        SetObjectOnStage(targetGameObject.transform);
    }
    
    public void SetObjectOnStage(Transform targetTransform)
    {
        if (StagedTransform && StagedTransform != targetTransform)
            throw new InvalidOperationException("An Object is already on stage!");
        
        RuntimeIcons.Log.LogInfo($"Setting stage for {targetTransform.name}");
        
        PivotTransform.parent = null;
        PivotTransform.position = targetTransform.position;
        PivotTransform.rotation = Quaternion.identity;
        SceneManager.MoveGameObjectToScene(PivotGo, targetTransform.gameObject.scene);
        
        StagedTransform = targetTransform;
        
        var matrix = Matrix4x4.TRS(Vector3.zero, targetTransform.rotation, targetTransform.localScale);
        
        var executionOptions = new ExecutionOptions()
        {
            VertexCache = VertexesExtensions.GlobalPartialCache,
            FilteredComponents = new HashSet<Type> { typeof(ScanNodeProperties) },
            LogHandler = RuntimeIcons.VerboseMeshLog,
            OverrideMatrix = matrix
        };
        
        if (!targetTransform.gameObject.TryGetBounds(out var bounds, executionOptions))
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
            
            var executionOptions = new ExecutionOptions()
            {
                VertexCache = VertexesExtensions.GlobalPartialCache,
                FilteredComponents = new HashSet<Type> { typeof(ScanNodeProperties) },
                LogHandler = RuntimeIcons.VerboseMeshLog,
                OverrideMatrix = matrix
            };
            
            if (!PivotGo.TryGetBounds(out var bounds, executionOptions))
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
        
        var executionOptions = new ExecutionOptions()
        {
            VertexCache = VertexesExtensions.GlobalPartialCache,
            FilteredComponents = new HashSet<Type> { typeof(ScanNodeProperties) },
            LogHandler = RuntimeIcons.VerboseMeshLog,
            OverrideMatrix = matrix
        };
        
        if (!PivotGo.TryGetBounds(out var bounds, executionOptions))
            throw new InvalidOperationException("This object has no Renders!");
        
        if (bounds.size == Vector3.zero)
            throw new InvalidOperationException("This object has no Bounds!");
        
        // Calculate the visible world area based on the camera's orthographic size and aspect ratio
        var cameraHeight = 2f * _camera.orthographicSize; // Total height in world units (orthographicSize is half the height)
        var cameraWidth = cameraHeight; // assume aspect ratio of 1
        var targetWorldArea = new Vector2(cameraWidth, cameraHeight);

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
        
        LightTransform.position = PivotTransform.position;
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
        
        PivotTransform.parent = null;
        PivotTransform.position = transform.position;
        PivotTransform.rotation = Quaternion.identity;
        LightTransform.localPosition = Vector3.zero;
        LightTransform.rotation = Quaternion.identity;
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

        var destTexture = RenderTexture.GetTemporary(Resolution.x, Resolution.y, 8, GraphicsFormat.R16G16B16A16_SFloat);
        var dummyTexture = RenderTexture.GetTemporary(Resolution.x, Resolution.y, 0, RenderTextureFormat.R8);
        _camera.targetTexture = dummyTexture;
        _cameraPass.targetTexture = destTexture;
        using (new IsolateStageLights(PivotGo))
        {
            //Turn on the stage Lights
            LightGo.SetActive(true);
            
            _camera.Render();
            
            //Turn off the stage Lights
            LightGo.SetActive(false);
        }

        // Activate the temporary render texture
        var previouslyActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = destTexture;

        // Extract the image into a new texture without mipmaps
        var texture = new Texture2D(destTexture.width, destTexture.height, GraphicsFormat.R16G16B16A16_SFloat, 1, TextureCreationFlags.DontInitializePixels)
        {
            name = $"{nameof(RuntimeIcons)}.{StagedTransform.name}Texture",
            filterMode = FilterMode.Point,
        };
        
        texture.ReadPixels(new Rect(0, 0, destTexture.width, destTexture.height), 0, 0);

        // UnPremultiply the texture
        texture.UnPremultiply();

        texture.Apply();
        
        // Reactivate the previously active render texture
        RenderTexture.active = previouslyActiveRenderTexture;
        
        // Clean up after ourselves
        _camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(dummyTexture);
        _cameraPass.targetTexture = null;
        RenderTexture.ReleaseTemporary(destTexture);
        
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