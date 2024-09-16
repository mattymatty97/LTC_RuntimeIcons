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
using VertexLibrary.Caches;

namespace RuntimeIcons.Components;

public class StageComponent : MonoBehaviour
{

    private StageComponent(){}

    public IVertexCache VertexCache { get; set; } = VertexesExtensions.GlobalPartialCache;

    public GameObject LightGo { get; private set; }

    public GameObject PivotGo
    {
        get
        {
            if (!_targetGo)
                _targetGo = CreatePivotGo();
            return _targetGo;
        }
    }

    private GameObject CameraGo { get; set; }

    public Transform LightTransform => LightGo.transform;
    public Transform PivotTransform => PivotGo.transform;
    private Transform CameraTransform => CameraGo.transform;
    
    private Camera _camera;
    private HDAdditionalCameraData _cameraSettings;
    private CustomPassThing _cameraPass;
    private GameObject _targetGo;
    private Vector2Int _resolution = new Vector2Int(128, 128);

    public Vector2Int Resolution
    {
        get => _resolution;
        set
        {
            _resolution = value;
            _camera.aspect = (float)_resolution.x / _resolution.y;
        }
    }

    public int CullingMask => _camera.cullingMask;

    public Transform StagedTransform { get; private set; }
    
    private TransformMemory Memory { get;  set; }

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

    public static StageComponent CreateStage(HideFlags hideFlags, int cameraLayerMask = 1,string stageName = "Stage")
    {
        
        //create the root Object for the Stage
        var stageGo = new GameObject(stageName)
        {
            hideFlags = hideFlags
        };

        //add the component to the stage
        var stageComponent = stageGo.AddComponent<StageComponent>();

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
        cam.cullingMask = cameraLayerMask;
        cam.orthographic = true;
        cam.orthographicSize = 1;
        cam.aspect = (float)stageComponent.Resolution.x / stageComponent.Resolution.y;
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
        camData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.DecalLayers] = true;
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

    public void SetObjectOnStage(GameObject targetGameObject)
    {
        SetObjectOnStage(targetGameObject.transform);
    }
    
    public void SetObjectOnStage(GameObject targetGameObject, Quaternion rotation)
    {
        SetObjectOnStage(targetGameObject.transform);
    }

    public void SetObjectOnStage(Transform targetTransform)
    {
        SetObjectOnStage(targetTransform, targetTransform.rotation);
    }
    
    public void SetObjectOnStage(Transform targetTransform, Quaternion rotation)
    {
        if (StagedTransform && StagedTransform != targetTransform)
            throw new InvalidOperationException("An Object is already on stage!");
        
        RuntimeIcons.Log.LogInfo($"Setting stage for {targetTransform.name}");
        
        PivotTransform.parent = null;
        PivotTransform.position = targetTransform.position;
        PivotTransform.rotation = Quaternion.identity;
        SceneManager.MoveGameObjectToScene(PivotGo, targetTransform.gameObject.scene);
        
        StagedTransform = targetTransform;
        
        Memory = new TransformMemory(StagedTransform);
        
        StagedTransform.SetParent(PivotTransform, false);
        StagedTransform.localPosition = Vector3.zero;
    }
    
    public void CenterObjectOnPivot(Quaternion? overrideRotation = null)
    {
        if (!StagedTransform)
            throw new InvalidOperationException("No Object on stage!");

        if (overrideRotation.HasValue)
            StagedTransform.rotation = overrideRotation.Value;
        
        var matrix = Matrix4x4.TRS(Vector3.zero, StagedTransform.rotation, StagedTransform.localScale);
        
        var executionOptions = new ExecutionOptions()
        {
            VertexCache = VertexCache,
            CullingMask = CullingMask,
            LogHandler = RuntimeIcons.VerboseMeshLog,
            OverrideMatrix = matrix
        };
        
        if (!StagedTransform.gameObject.TryGetBounds(out var bounds, executionOptions))
            throw new InvalidOperationException("This object has no Renders!");
        
        StagedTransform.localPosition = -bounds.center;
        
        RuntimeIcons.Log.LogInfo($"StagedObject offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
        
    }
    
    public void FindOptimalOffsetAndScale()
    {
        if (!StagedTransform)
            throw new InvalidOperationException("No Object on stage!");
        
        var matrix = Matrix4x4.TRS(Vector3.zero, PivotTransform.rotation, Vector3.one);
        
        var executionOptions = new ExecutionOptions()
        {
            VertexCache = VertexCache,
            CullingMask = CullingMask,
            LogHandler = RuntimeIcons.VerboseMeshLog,
            OverrideMatrix = matrix
        };
        
        if (!PivotGo.TryGetBounds(out var bounds, executionOptions))
            throw new InvalidOperationException("This object has no Renders!");
        
        if (bounds.size == Vector3.zero)
            throw new InvalidOperationException("This object has no Bounds!");
        
        // Calculate the visible world area based on the camera's orthographic size and aspect ratio
        var cameraHeight = 2f * _camera.orthographicSize; // Total height in world units (orthographicSize is half the height)
        var cameraWidth = cameraHeight * _camera.aspect;
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
        
        public IsolateStageLights(GameObject stagePivot)
        {
            _lightMemory = UnityEngine.Pool.HashSetPool<Light>.Get();

            _ambientLight = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.black;

            var localLights = stagePivot.GetComponentsInChildren<Light>();

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