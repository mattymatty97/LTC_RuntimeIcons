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
    private StageComponent()
    {
    }

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

    private TransformMemory Memory { get; set; }

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

    public static StageComponent CreateStage(HideFlags hideFlags, int cameraLayerMask = 1, string stageName = "Stage", bool orthographic = true)
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
        cam.orthographic = orthographic;
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

        RuntimeIcons.Log.LogInfo(
            $"StagedObject offset {StagedTransform.localPosition} rotation {StagedTransform.localRotation.eulerAngles}");
    }

    public void PrepareCameraForShot()
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

        var vertexes = PivotGo.GetVertexes(executionOptions);
        
        if (vertexes.Length == 0)
            throw new InvalidOperationException("This object has no Renders!");

        var bounds = vertexes.GetBounds();
        
        if (bounds.size == Vector3.zero)
            throw new InvalidOperationException("This object has no Bounds!");

        var distanceToCamera = Mathf.Max(_camera.nearClipPlane + bounds.size.z, 3f);

        var localOffset = new Vector3(-bounds.center.x, -bounds.center.y, distanceToCamera);

        PivotTransform.position = CameraTransform.position + localOffset;
        
        RuntimeIcons.Log.LogInfo($"Stage offset {localOffset}");

        if (_camera.orthographic)
        {
            var boundsAspect = bounds.size.x / bounds.size.y;
            float orthoHeight;
            if (boundsAspect > _camera.aspect)
            {
                var orthoWidth = bounds.size.x;
                orthoHeight = orthoWidth / _camera.aspect;
            }
            else
            {
                orthoHeight= bounds.size.y;
            }
            _camera.orthographicSize = orthoHeight * 1.1f;
        }
        else
        {

            var inverse = matrix.inverse;

            //get vertexes in local space

            var localVertexes = vertexes.Select(inverse.MultiplyPoint3x4).ToArray();

            //get vertexes in world space

            var worldVertexes = localVertexes.Select(PivotTransform.localToWorldMatrix.MultiplyPoint3x4).ToArray();
            
            float angleMinX, angleMaxX;
            float angleMinY, angleMaxY;
            
            for (var i = 0; i < 5; i++)
            {
                GetCameraAngles(_camera, _camera.transform.right, worldVertexes, out angleMinY, out angleMaxY);
                CameraTransform.Rotate(Vector3.up, (angleMinY + angleMaxY) / 2, Space.World);
    
                GetCameraAngles(_camera, -_camera.transform.up, worldVertexes, out angleMinX, out angleMaxX);
                CameraTransform.Rotate(Vector3.right, (angleMinX + angleMaxX) / 2, Space.Self);
            }
            
            GetCameraAngles(_camera, _camera.transform.right, worldVertexes, out angleMinY, out angleMaxY);
            GetCameraAngles(_camera, -_camera.transform.up, worldVertexes, out angleMinX, out angleMaxX);

            var fovAngleX = Math.Max(-angleMinX, angleMaxX) * 2;
            var fovAngleY = Camera.HorizontalToVerticalFieldOfView(Math.Max(-angleMinY, angleMaxY) * 2, _camera.aspect);
            _camera.fieldOfView = Math.Max(fovAngleX, fovAngleY) * 1.15f;


            RuntimeIcons.Log.LogInfo($"Camera FOV {_camera.fieldOfView}");
        }

        LightTransform.position = PivotTransform.position;
    }

    public void ResetStage()
    {
        if (StagedTransform)
        {
            StagedTransform.SetParent(Memory.Parent, false);

            StagedTransform.localScale = Memory.LocalScale;
            StagedTransform.SetLocalPositionAndRotation(Memory.LocalPosition, Memory.LocalRotation);
        }

        StagedTransform = null;
        Memory = default;

        PivotTransform.parent = null;
        PivotTransform.position = transform.position;
        PivotTransform.rotation = Quaternion.identity;
        CameraTransform.rotation = Quaternion.identity;
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
        var texture = new Texture2D(destTexture.width, destTexture.height, GraphicsFormat.R16G16B16A16_SFloat, 1,
            TextureCreationFlags.DontInitializePixels)
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

            var globalLights = FindObjectsOfType<Light>().Where(l => !localLights.Contains(l)).Where(l => l.enabled)
                .ToArray();

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
    
    private static void GetCameraAngles(Camera camera, Vector3 direction, IEnumerable<Vector3> vertices, out float angleMin, out float angleMax)
    {
        var position = camera.transform.position;
        var forwardPlane = new Plane(camera.transform.forward, position);
        var directionPlane = new Plane(direction, position);
        angleMin = float.PositiveInfinity;
        angleMax = float.NegativeInfinity;

        foreach (var vertex in vertices)
        {
            var distance = forwardPlane.GetDistanceToPoint(vertex);
            var projected = Mathf.Atan(directionPlane.GetDistanceToPoint(vertex) / distance) * Mathf.Rad2Deg;

            angleMin = Math.Min(projected, angleMin);
            angleMax = Math.Max(projected, angleMax);
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