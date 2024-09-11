/*
Code from https://gist.github.com/mickdekkers/5c3c62539c057010d4497f9865060e20
By https://github.com/mickdekkers
As stated in this comment https://gist.github.com/mickdekkers/5c3c62539c057010d4497f9865060e20?permalink_comment_id=3253740#gistcomment-3253740
to be considered MIT licensed
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RuntimeIcons.Components;

// Object rendering code based on Dave Carlile's "Create a GameObject Image Using Render Textures" post
// Link: http://crappycoding.com/2014/12/create-gameobject-image-using-render-textures/

/// <summary>
/// Takes snapshot images of prefabs and GameObject instances, and provides methods to save them as PNG files.
/// </summary>
public class SnapshotCamera : MonoBehaviour {
    // This disables the "never assigned" warning.
    // These fields will be assigned by the factory.
#pragma warning disable 0649
    /// <summary>
    /// The Camera used internally by the SnapshotCamera.
    /// </summary>
    internal Camera cam { get; private set; }
    internal HDAdditionalCameraData camData { get; private set; }
    /// <summary>
    /// The Light used internally by the SnapshotCamera.
    /// </summary>
    internal GameObject lightGo { get; private set; }
    /// <summary>
    /// The layer on which the SnapshotCamera takes snapshots.
    /// </summary>
    internal int layer { get; private set; }

    internal RenderData data { get; } = new RenderData();
#pragma warning restore 0649

    /// <summary>
    /// The default position offset applied to objects when none is specified.
    /// </summary>
    public Vector3 defaultPositionOffset = new Vector3(0, 0, 1);
    /// <summary>
    /// The default rotation applied to objects when none is specified.
    /// </summary>
    public Vector3 defaultRotation = new Vector3(345.8529f, 313.8297f, 14.28433f);
    /// <summary>
    /// The default scale applied to objects when none is specified.
    /// </summary>
    public Vector3 defaultScale = new Vector3(1, 1, 1);

    // This private constructor serves to ensure only the factory can produce new instances.
    private SnapshotCamera () { }

    /// <summary>
    /// Factory method which sets up and configures a new SnapshotCamera, then returns it.
    /// </summary>
    /// <param name="layer">The name of the layer on which to take snapshots.</param>
    /// <param name="name">The name that will be given to the new GameObject the SnapshotCamera will be attached to.</param>
    /// <returns>A new SnapshotCamera, ready for use.</returns>
    public static SnapshotCamera MakeSnapshotCamera (string layer, string name = "Snapshot Camera")
    {
        return MakeSnapshotCamera(LayerMask.NameToLayer(layer), name);
    }

    /// <summary>
    /// Factory method which sets up and configures a new SnapshotCamera, then returns it.
    /// </summary>
    /// <param name="layer">The layer number of the layer on which to take snapshots.</param>
    /// <param name="name">The name that will be given to the new GameObject the SnapshotCamera will be attached to.</param>
    /// <returns>A new SnapshotCamera, ready for use.</returns>
    public static SnapshotCamera MakeSnapshotCamera (int layer = 5, string name = "Snapshot Camera")
    {
        if (layer < 0 || layer > 31)
            throw new ArgumentOutOfRangeException(nameof(layer), "layer argument must specify a valid layer between 0 and 31");

        // Create a new GameObject to hold the camera
        GameObject snapshotCameraGO = new GameObject(name);
        
        GameObject cameraGO = new GameObject("Camera");
        cameraGO.transform.parent = snapshotCameraGO.transform;
        // Add a Camera component to the GameObject
        Camera cam = cameraGO.AddComponent<Camera>();

        // Configure the Camera
        cam.cullingMask = 1 << layer;
        cam.orthographic = true;
        cam.orthographicSize = 1;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 10f;
        cam.enabled = false;
        
        
        // Add a Camera component to the GameObject
        HDAdditionalCameraData camData = cameraGO.AddComponent<HDAdditionalCameraData>();

        camData.clearDepth = true;
        camData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        camData.backgroundColorHDR = Color.clear;
        camData.customRender += CustomRender;
        
        GameObject lightGO = new GameObject("SpotLight");
        lightGO.transform.parent = snapshotCameraGO.transform;
        lightGO.layer = layer;
        lightGO.SetActive(false);
        
        GameObject lightGO1 = new GameObject("SpotLight");
        lightGO1.transform.parent = lightGO.transform;
        lightGO1.layer = layer;
        lightGO1.transform.localPosition = new Vector3(0, 4, 0);
        lightGO1.transform.rotation = Quaternion.LookRotation(Vector3.down);
        
        Light light = lightGO1.AddComponent<Light>();
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
        
        HDAdditionalLightData lightData = lightGO1.AddComponent<HDAdditionalLightData>();
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
        lightData.intensity = 1295.433f;
        
        
        GameObject lightGO2 = Object.Instantiate(lightGO1, lightGO.transform);
        lightGO2.transform.localPosition = new Vector3(0, 0, -4);
        lightGO2.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
        
        // Add a SnapshotCamera component to the GameObject
        SnapshotCamera snapshotCamera = snapshotCameraGO.AddComponent<SnapshotCamera>();
        
        // Set the SnapshotCamera's cam and layer fields
        snapshotCamera.cam = cam;
        snapshotCamera.camData = camData;
        snapshotCamera.lightGo = lightGO;
        snapshotCamera.layer = layer;

        // Return the SnapshotCamera
        return snapshotCamera;
        
        
        void CustomRender(ScriptableRenderContext context, HDCamera hd)
        {
            var camera = hd.camera;

            // Ensure the camera has culling parameters
            if (camera.TryGetCullingParameters(out var culling))
            {
                // Perform culling
                var cull = context.Cull(ref culling);

                // Set up HDRP's rendering features
                using (var cmd = new CommandBuffer())
                {
                    cmd.name = "Clear and Render";
                    cmd.ClearRenderTarget(true, true, Color.clear);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                // Set up camera properties
                context.SetupCameraProperties(camera);

                var sortingSettings = new SortingSettings(camera)
                {
                    criteria = SortingCriteria.CommonTransparent
                };
                
                // Define DrawingSettings with proper shader passes
                DrawingSettings drawingSettings = new DrawingSettings(
                    HDShaderPassNames.s_ForwardName,
                    sortingSettings
                )
                {
                    perObjectData = PerObjectData.LightProbe | PerObjectData.ReflectionProbes | 
                                    PerObjectData.Lightmaps | PerObjectData.ShadowMask | 
                                    PerObjectData.MotionVectors | PerObjectData.LightData | 
                                    PerObjectData.LightIndices | PerObjectData.OcclusionProbe |
                                    PerObjectData.ReflectionProbeData | PerObjectData.LightProbeProxyVolume |
                                    PerObjectData.OcclusionProbeProxyVolume
                };

                // Set shader passes
                drawingSettings.SetShaderPassName(0, HDShaderPassNames.s_DepthOnlyName); // Depth pass
                drawingSettings.SetShaderPassName(1, new ShaderTagId(HDShaderPassNames.s_ShadowCasterStr)); // Shadow caster
                drawingSettings.SetShaderPassName(5, HDShaderPassNames.s_ForwardName); // Forward pass
                drawingSettings.SetShaderPassName(6, HDShaderPassNames.s_ForwardOnlyName); // Forward only
                drawingSettings.SetShaderPassName(7, HDShaderPassNames.s_SRPDefaultUnlitName); // Unlit pass
                drawingSettings.SetShaderPassName(8, HDShaderPassNames.s_DecalMeshForwardEmissiveName); // Decal pass
                drawingSettings.SetShaderPassName(9, HDShaderPassNames.s_TransparentDepthPrepassName); // Transparent depth prepass
                drawingSettings.SetShaderPassName(10, HDShaderPassNames.s_TransparentBackfaceName); // Transparent backface
                drawingSettings.SetShaderPassName(11, HDShaderPassNames.s_TransparentDepthPostpassName); // Transparent depth postpass

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
                
                context.DrawRenderers(cull, ref drawingSettings, ref filteringSettings);
                
            }
            
            context.Submit();
        }
    }

    #region PNG saving
    /// <summary>
    /// Sanitizes a filename string by replacing illegal characters with underscores.
    /// </summary>
    /// <param name="dirty">The unsanitized filename string.</param>
    /// <returns>A sanitized filename string with illegal characters replaced with underscores.</returns>
    private static string SanitizeFilename (string dirty)
    {
        string invalidFileNameChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidFileNameChars);

        return Regex.Replace(dirty, invalidRegStr, "_");
    }

    /// <summary>
    /// Saves a byte array of PNG data as a PNG file.
    /// </summary>
    /// <param name="bytes">The PNG data to write to a file.</param>
    /// <param name="filename">The name of the file. This will be the current timestamp if not specified.</param>
    /// <param name="directory">The directory in which to save the file. This will be the game/Snapshots directory if not specified.</param>
    /// <returns>A FileInfo pointing to the created PNG file</returns>
    public static FileInfo SavePNG (byte[] bytes, string filename = "", string directory = "")
    {
        directory = directory != "" ? Directory.CreateDirectory(directory).FullName : Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Snapshots")).FullName;
        filename = filename != "" ? SanitizeFilename(filename) + ".png" : System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".png";
        string filepath = Path.Combine(directory, filename);

        File.WriteAllBytes(filepath, bytes);

        return new FileInfo(filepath);
    }
    
    /// <summary>
    /// Saves a Texture2D as a PNG file.
    /// </summary>
    /// <param name="tex">The Texture2D to write to a file.</param>
    /// <param name="filename">The name of the file. This will be the current timestamp if not specified.</param>
    /// <param name="directory">The directory in which to save the file. This will be the game/Snapshots directory if not specified.</param>
    /// <returns>A FileInfo pointing to the created PNG file</returns>
    public static FileInfo SavePNG (Texture2D tex, string filename = "", string directory = "")
    {
        return SavePNG(tex.EncodeToPNG(), filename, directory);
    }
    
    #endregion
    #region Object preparation
    /// <summary>
    /// This stores the a state (layers, position, rotation, and scale) of a GameObject, and provides a method to restore it.
    /// </summary>
    private struct GameObjectStateSnapshot
    {
        private GameObject gameObject;
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;
        private Dictionary<GameObject, int> layers;

        /// <summary>
        /// Store the current state (layers, position, rotation, and scale) of a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject whose state to store.</param>
        public GameObjectStateSnapshot (GameObject gameObject)
        {
            this.gameObject = gameObject;
            this.position = gameObject.transform.position;
            this.rotation = gameObject.transform.rotation;
            this.scale = gameObject.transform.localScale;

            this.layers = new Dictionary<GameObject, int>();
            foreach (Transform t in gameObject.GetComponentsInChildren<Transform>(true))
            {
                this.layers.Add(t.gameObject, t.gameObject.layer);
            }
        }

        /// <summary>
        /// Restore the gameObject to the state stored in this GameObjectStateSnapshot.
        /// </summary>
        public void Restore ()
        {
            this.gameObject.transform.position = this.position;
            this.gameObject.transform.rotation = this.rotation;
            this.gameObject.transform.localScale = this.scale;

            foreach (KeyValuePair<GameObject, int> entry in this.layers)
            {
                entry.Key.layer = entry.Value;
            }
        }
    }

    /// <summary>
    /// Set the layers of the GameObject and all its children to the SnapshotCamera's snapshot layer so the SnapshotCamera can see it.
    /// </summary>
    /// <param name="gameObject">The GameObject apply the layer modifications to.</param>
    private void SetLayersRecursively (GameObject gameObject)
    {
        foreach (Transform transform in gameObject.GetComponentsInChildren<Transform>(true))
            transform.gameObject.layer = layer;
    }

    /// <summary>
    /// Prepares an instantiated GameObject for taking a snapshot by setting its layers and applying the specified position offset, rotation, and scale to it.
    /// </summary>
    /// <param name="prefab">The instantiated GameObject to prepare.</param>
    /// <param name="positionOffset">The position offset relative to the SnapshotCamera to apply to the gameObject.</param>
    /// <param name="rotation">The rotation to apply to the gameObject.</param>
    /// <param name="scale">The scale to apply to the gameObject.</param>
    /// <returns>A GameObjectStateSnapshot containing the state of the gameObject prior to modifying its layers, position, rotation, and scale.</returns>
    private GameObjectStateSnapshot PrepareObject (GameObject gameObject, Vector3 positionOffset, Quaternion rotation, Vector3 scale)
    {
        GameObjectStateSnapshot goss = new GameObjectStateSnapshot(gameObject);
        
        gameObject.transform.position = transform.position + positionOffset;
        gameObject.transform.rotation = rotation;
        gameObject.transform.localScale = scale;
        //SetLayersRecursively(gameObject);

        return goss;
    }

    /// <summary>
    /// Prepares a prefab for taking a snapshot by creating an instance, setting its layers and applying the specified position offset, rotation, and scale to it.
    /// </summary>
    /// <param name="prefab">The prefab to prepare.</param>
    /// <param name="positionOffset">The position offset relative to the SnapshotCamera to apply to the prefab.</param>
    /// <param name="rotation">The rotation to apply to the prefab.</param>
    /// <param name="scale">The scale to apply to the prefab.</param>
    /// <returns>A prefab instance ready for taking a snapshot.</returns>
    private GameObject PreparePrefab (GameObject prefab, Vector3 positionOffset, Quaternion rotation, Vector3 scale)
    {
        GameObject gameObject = GameObject.Instantiate(prefab, transform.position + positionOffset, rotation) as GameObject;
        gameObject.transform.localScale = scale;
        SetLayersRecursively(gameObject);

        return gameObject;
    }
    #endregion

    #region TakeObjectSnapshot
    /// <summary>
    /// Takes a snapshot of an instantiated GameObject and returns it as a Texture2D.
    /// 
    /// Uses a completely transparent background and
    /// applies the default position offset, rotation and scale to the gameObject while taking the snapshot, and restores them afterwards.
    /// </summary>
    /// <param name="gameObject">The instantiated GameObject to snapshot.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakeObjectSnapshot (GameObject gameObject, int width = 128, int height = 128)
    {
        return TakeObjectSnapshot(gameObject, Color.clear, defaultPositionOffset, Quaternion.Euler(defaultRotation), defaultScale, width, height);
    }

    /// <summary>
    /// Takes a snapshot of an instantiated GameObject and returns it as a Texture2D.
    /// 
    /// Applies the default position offset, rotation and scale to the gameObject while taking the snapshot, and restores them afterwards.
    /// </summary>
    /// <param name="gameObject">The instantiated GameObject to snapshot.</param>
    /// <param name="backgroundColor">The background color of the snapshot. Can be transparent.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakeObjectSnapshot (GameObject gameObject, Color backgroundColor, int width = 128, int height = 128)
    {
        return TakeObjectSnapshot (gameObject, backgroundColor, defaultPositionOffset, Quaternion.Euler(defaultRotation), defaultScale, width, height);
    }

    /// <summary>
    /// Takes a snapshot of an instantiated GameObject and returns it as a Texture2D.
    /// 
    /// Uses a completely transparent background.
    /// </summary>
    /// <param name="gameObject">The instantiated GameObject to snapshot.</param>
    /// <param name="positionOffset">The position offset relative to the SnapshotCamera that will be applied to the gameObject while taking the snapshot. Its position will be restored after taking the snapshot.</param>
    /// <param name="rotation">The rotation that will be applied to the gameObject while taking the snapshot. Its rotation will be restored after taking the snapshot.</param>
    /// <param name="scale">The scale that will be applied to the gameObject while taking the snapshot. Its scale will be restored after taking the snapshot.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakeObjectSnapshot (GameObject gameObject, Vector3 positionOffset, Quaternion rotation, Vector3 scale, int width = 128, int height = 128)
    {
        return TakeObjectSnapshot(gameObject, Color.clear, positionOffset, rotation, scale, width, height);
    }

    /// <summary>
    /// Takes a snapshot of an instantiated GameObject and returns it as a Texture2D.
    /// </summary>
    /// <param name="gameObject">The instantiated GameObject to snapshot.</param>
    /// <param name="backgroundColor">The background color of the snapshot. Can be transparent.</param>
    /// <param name="positionOffset">The position offset relative to the SnapshotCamera that will be applied to the gameObject while taking the snapshot. Its position will be restored after taking the snapshot.</param>
    /// <param name="rotation">The rotation that will be applied to the gameObject while taking the snapshot. Its rotation will be restored after taking the snapshot.</param>
    /// <param name="scale">The scale that will be applied to the gameObject while taking the snapshot. Its scale will be restored after taking the snapshot.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakeObjectSnapshot (GameObject gameObject, Color backgroundColor, Vector3 positionOffset, Quaternion rotation, Vector3 scale, int width = 128, int height = 128)
    {
        if (gameObject == null)
            throw new ArgumentNullException("gameObject");
        else if (gameObject.scene.name == null)
            throw new ArgumentException("gameObject parameter must be an instantiated GameObject! If you want to use a prefab directly, use TakePrefabSnapshot instead.", "gameObject");
        
        // Prepare the gameObject and save its current state so we can restore it later
        GameObjectStateSnapshot previousState = PrepareObject(gameObject, positionOffset, rotation, scale);
        
        // Take a snapshot
        Texture2D snapshot = TakeSnapshot(backgroundColor, width, height);

        // Restore the gameObject to its previous state
        previousState.Restore();

        // Return the snapshot
        return snapshot;
    }
    #endregion

    #region TakePrefabSnapshot
    /// <summary>
    /// Takes a snapshot of a prefab and returns it as a Texture2D.
    /// 
    /// Uses a completely transparent background and
    /// applies the default position offset, rotation and scale to the prefab while taking the snapshot.
    /// </summary>
    /// <param name="prefab">The prefab to snapshot.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakePrefabSnapshot (GameObject prefab, int width = 128, int height = 128)
    {
        return TakePrefabSnapshot(prefab, Color.clear, defaultPositionOffset, Quaternion.Euler(defaultRotation), defaultScale, width, height);
    }

    /// <summary>
    /// Takes a snapshot of a prefab and returns it as a Texture2D.
    /// 
    /// Applies the default position offset, rotation and scale to the prefab while taking the snapshot.
    /// </summary>
    /// <param name="prefab">The prefab to snapshot.</param>
    /// <param name="backgroundColor">The background color of the snapshot. Can be transparent.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakePrefabSnapshot (GameObject prefab, Color backgroundColor, int width = 128, int height = 128)
    {
        return TakePrefabSnapshot(prefab, backgroundColor, defaultPositionOffset, Quaternion.Euler(defaultRotation), defaultScale, width, height);
    }

    /// <summary>
    /// Takes a snapshot of a prefab and returns it as a Texture2D.
    /// 
    /// Uses a completely transparent background.
    /// </summary>
    /// <param name="prefab">The prefab to snapshot.</param>
    /// <param name="positionOffset">The position offset relative to the SnapshotCamera that will be applied to the prefab while taking the snapshot.</param>
    /// <param name="rotation">The rotation that will be applied to the prefab while taking the snapshot.</param>
    /// <param name="scale">The scale that will be applied to the prefab while taking the snapshot.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakePrefabSnapshot (GameObject prefab, Vector3 positionOffset, Quaternion rotation, Vector3 scale, int width = 128, int height = 128)
    {
        return TakePrefabSnapshot(prefab, Color.clear, positionOffset, rotation, scale, width, height);
    }

    /// <summary>
    /// Takes a snapshot of a prefab and returns it as a Texture2D.
    /// </summary>
    /// <param name="prefab">The prefab to snapshot.</param>
    /// <param name="backgroundColor">The background color of the snapshot. Can be transparent.</param>
    /// <param name="positionOffset">The position offset relative to the SnapshotCamera that will be applied to the prefab while taking the snapshot.</param>
    /// <param name="rotation">The rotation that will be applied to the prefab while taking the snapshot.</param>
    /// <param name="scale">The scale that will be applied to the prefab while taking the snapshot.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    public Texture2D TakePrefabSnapshot (GameObject prefab, Color backgroundColor, Vector3 positionOffset, Quaternion rotation, Vector3 scale, int width = 128, int height = 128)
    {
        if (prefab == null)
            throw new ArgumentNullException("prefab");
        else if (prefab.scene.name != null)
            throw new ArgumentException("prefab parameter must be a prefab! If you want to use an instance, use TakeObjectSnapshot instead.", "prefab");

        // Prepare an instance of the prefab
        GameObject instance = PreparePrefab(prefab, positionOffset, rotation, scale);
        
        // Take a snapshot
        Texture2D snapshot = TakeSnapshot(backgroundColor, width, height);

        // Destroy the instance we created
        DestroyImmediate(instance);

        // Return the snapshot
        return snapshot;
    }
    #endregion

    /// <summary>
    /// Takes a snapshot of whatever is in front of the camera and within the camera's culling mask and returns it as a Texture2D.
    /// </summary>
    /// <param name="backgroundColor">The background color to apply to the camera before taking the snapshot.</param>
    /// <param name="width">The width of the snapshot image.</param>
    /// <param name="height">The height of the snapshot image.</param>
    /// <returns>A Texture2D containing the captured snapshot.</returns>
    private Texture2D TakeSnapshot(Color backgroundColor, int width, int height)
    {
        lightGo.SetActive(true);
        data.width = width;
        data.height = height;
        // Set the background color of the camera
        cam.backgroundColor = backgroundColor;

        // Get a temporary render texture and render the camera
        cam.targetTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        cam.Render();
        
        // Activate the temporary render texture
        RenderTexture previouslyActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;
        
        // Extract the image into a new texture without mipmaps
        Texture2D texture = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.ARGB32,  -1,false);
        
        texture.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        texture.Apply();
        
        // Reactivate the previously active render texture
        RenderTexture.active = previouslyActiveRenderTexture;
        
        // Clean up after ourselves
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(cam.targetTexture);

        lightGo.SetActive(false);
        
        // Return the texture
        return texture;
    }

    public class RenderData
    {
        public int width { get; protected internal set; }
        public int height { get; protected internal set; }
        
        public RenderTexture outputTexture { get; internal set; }
    }
}