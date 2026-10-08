using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI.EditorTools
{
    /// <summary>
    /// 工程装配与验证工具（菜单：Glass Demo）。
    /// 1) 把毛玻璃 Feature 插进 URP 的 Renderer 资产；
    /// 2) 生成占位测试场景（方块阵 + 毛玻璃界面），用来肉眼确认「背景真的被糊了」；
    /// 3) 离屏截图，批处理也能跑。
    /// </summary>
    public static class GlassDemoTools
    {
        const string TestScenePath = "Assets/Scenes/GlassTest.unity";

        // ==================== 1. 安装 Renderer Feature ====================

        [MenuItem("Glass Demo/1. 把毛玻璃 Feature 装进 URP Renderer", priority = 1)]
        public static void InstallFeature()
        {
            var guids = AssetDatabase.FindAssets("t:UniversalRendererData");
            if (guids.Length == 0)
            {
                Debug.LogError("[GlassUI] 没找到 UniversalRendererData —— URP 资产没配好？");
                return;
            }

            int touched = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/"))
                {
                    Debug.Log($"[GlassUI] 跳过包内资产（不往包缓存里写）：{path}");
                    continue;
                }

                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) continue;

                FrostedGlassFeature feature = null;
                foreach (var existing in data.rendererFeatures)
                    if (existing is FrostedGlassFeature f) { feature = f; break; }

                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<FrostedGlassFeature>();
                    feature.name = "Frosted Glass Blur";
                    AssetDatabase.AddObjectToAsset(feature, data);
                    data.rendererFeatures.Add(feature);
                    Debug.Log($"[GlassUI] 已把毛玻璃 Feature 加进 {path}");
                }
                else
                {
                    // 已存在就把参数同步成代码里的默认值——不然改了默认值，老资产不会跟着变
                    feature.settings = new FrostedGlassFeature.Settings();
                    Debug.Log($"[GlassUI] {path} 已有 Feature，参数已同步为代码默认值" +
                              $"（降采样 1/{1 << feature.settings.downsample}，高斯 σ {feature.settings.sigma}、迭代 {feature.settings.iterations}）");
                }

                EditorUtility.SetDirty(feature);
                data.SetDirty();                 // URP 靠这个让渲染器重建
                EditorUtility.SetDirty(data);
                touched++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var pipeline = GraphicsSettings.defaultRenderPipeline;
            Debug.Log($"[GlassUI] 处理了 {touched} 个 Renderer；默认管线 = {(pipeline != null ? pipeline.name : "内置（没设 URP！）")}");
        }

        // ==================== 2. 占位测试场景 ====================

        [MenuItem("Glass Demo/2. 生成占位测试场景", priority = 2)]
        public static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment();

            var canvas = BuildCanvas();
            // 界面由 GlassDemoUi 在运行时 / execute-always 搭建（毛玻璃材质运行时创建，烘焙进场景没有意义）
            canvas.gameObject.AddComponent<GlassDemoUi>();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TestScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TestScenePath, true) };
            AssetDatabase.SaveAssets();

            EditorPrefs.SetBool(AutoOpenKey, true);
            Debug.Log($"[GlassUI] 测试场景已生成：{TestScenePath}");
        }

        static void BuildEnvironment()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.transform.position = new Vector3(0f, -0.05f, 8f);

            // 方块阵：颜色和高度都有变化，模糊之后一眼能看出来
            var rng = new System.Random(20261008);
            var palette = new[]
            {
                GlassTheme.Blue, GlassTheme.BlueBright, GlassTheme.Cyan, GlassTheme.WhiteSoft,
                GlassTheme.BlueDeep, GlassTheme.White, GlassTheme.Warn, GlassTheme.Bad,
            };
            var field = new GameObject("Cube Field").transform;

            for (int z = 0; z < 26; z++)
            {
                for (int x = -12; x <= 12; x++)
                {
                    if (rng.NextDouble() < 0.22) continue;

                    float height = 0.6f + (float)rng.NextDouble() * 6.5f;
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Cube {x}_{z}";
                    cube.transform.SetParent(field, false);
                    cube.transform.position = new Vector3(
                        x * 1.15f + (float)rng.NextDouble() * 0.2f - 0.1f,
                        height * 0.5f,
                        z * 1.6f + 4f);
                    cube.transform.localScale = new Vector3(0.9f, height, 0.9f);

                    var mat = new Material(cube.GetComponent<Renderer>().sharedMaterial);
                    mat.color = palette[rng.Next(palette.Length)];
                    cube.GetComponent<Renderer>().sharedMaterial = mat;
                }
            }

            // 三根高对比立柱：位置固定，用来确认面板里采到的画面和身后位置一一对应
            var pillarColors = new[] { GlassTheme.Bad, GlassTheme.Warn, GlassTheme.Cyan };
            for (int i = 0; i < 3; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Beacon {i + 1}";
                pillar.transform.position = new Vector3(-4.5f + i * 4.5f, 5f, 12f + i * 3f);
                pillar.transform.localScale = new Vector3(0.7f, 10f, 0.7f);
                var mat = new Material(pillar.GetComponent<Renderer>().sharedMaterial);
                mat.color = pillarColors[i];
                pillar.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var sunGo = new GameObject("Directional Light", typeof(Light));
            sunGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.shadows = LightShadows.Soft;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 34f;
            RenderSettings.fogEndDistance = 90f;
            RenderSettings.fogColor = new Color(0.72f, 0.80f, 0.92f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.90f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.56f, 0.70f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.30f, 0.38f);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            camGo.transform.position = new Vector3(0f, 7.5f, -13f);
            camGo.transform.rotation = Quaternion.Euler(9f, 0f, 0f);
        }

        static Canvas BuildCanvas()
        {
            var go = new GameObject("Glass Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            return canvas;
        }

        // ==================== 3. 离屏截图 ====================

        [MenuItem("Glass Demo/3. 离屏截图 (Capture)", priority = 3)]
        public static void CaptureDefault() => Capture("/tmp/glass_shots", 1920, 1080);

        public static void CaptureAll()
        {
            var args = System.Environment.GetCommandLineArgs();
            string dir = "/tmp/glass_shots";
            int w = 1920, h = 1080;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-captureArgs") continue;
                var parts = args[i + 1].Split(' ');
                if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0])) dir = parts[0];
                if (parts.Length > 1) int.TryParse(parts[1], out w);
                if (parts.Length > 2) int.TryParse(parts[2], out h);
            }
            Capture(dir, w, h);
        }

        public static void Capture(string dir, int width, int height)
        {
            Directory.CreateDirectory(dir);

            if (EditorSceneManager.GetActiveScene().path != TestScenePath)
                EditorSceneManager.OpenScene(TestScenePath);

            var ui = Object.FindFirstObjectByType<GlassDemoUi>();
            if (ui != null) ui.Build();      // 材质是运行时创建的，截图前确保界面是刚搭好的

            var cam = Object.FindFirstObjectByType<Camera>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Debug.Log($"[GlassUI] 截图准备：相机={(cam != null ? cam.name : "null")} 画布={(canvas != null ? canvas.name : "null")} " +
                      $"管线={GraphicsSettings.currentRenderPipeline?.name ?? "内置"} 毛玻璃Feature数={CountFeatures()}");

            if (cam == null || canvas == null)
            {
                Debug.LogError("[GlassUI] 场景里缺相机或画布，没法截图");
                return;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            // Overlay 画布不会进相机的 target，截图时临时切成 ScreenSpaceCamera
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousPlane = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;

            Canvas.ForceUpdateCanvases();

            // 渲两遍：第一遍把 _FrostedGlassTex 备好，第二遍 UI 才采到当帧的模糊图
            cam.Render();
            cam.Render();

            var previousActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previousActive;

            var path = $"{dir}/glass.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousPlane;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log($"[GlassUI] 截图已输出：{path}");
        }

        static int CountFeatures()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) continue;
                foreach (var feature in data.rendererFeatures)
                    if (feature is FrostedGlassFeature) count++;
            }
            return count;
        }

        const string AutoOpenKey = "GlassUI.AutoOpened";
    }
}
