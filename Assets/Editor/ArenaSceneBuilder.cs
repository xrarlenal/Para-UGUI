using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using GlassUI.Game;

namespace GlassUI.EditorTools
{
    /// <summary>
    /// 生成正式演示场景 Arena.unity：训练场 + 第一人称玩家 + 哨戒炮 + UI 画布。
    /// 菜单：Glass Demo / 4. 生成 Arena 场景
    /// </summary>
    public static class ArenaSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Arena.unity";

        [MenuItem("Glass Demo/4. 生成 Arena 场景（正式演示）", priority = 4)]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 灯光与天空
            var sunGo = new GameObject("Sun", typeof(Light));
            sunGo.transform.rotation = Quaternion.Euler(46f, -34f, 0f);
            var sun = sunGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.97f, 0.93f);
            sun.shadows = LightShadows.Soft;

            // 程序化天空：有渐变和太阳，比空白天空干净得多
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.045f);
                sky.SetFloat("_AtmosphereThickness", 0.85f);
                sky.SetColor("_SkyTint", new Color(0.52f, 0.63f, 0.82f));
                sky.SetColor("_GroundColor", new Color(0.30f, 0.34f, 0.42f));
                RenderSettings.skybox = sky;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.66f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.50f, 0.64f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.23f, 0.30f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 120f;
            RenderSettings.fogColor = new Color(0.62f, 0.70f, 0.84f);

            // 世界（程序化训练场）
            var world = new GameObject("World");
            world.AddComponent<ArenaBuilder>();

            // 玩家
            var playerGo = new GameObject("Player", typeof(CharacterController));
            playerGo.transform.position = new Vector3(0f, 1.2f, 0f);
            var controller = playerGo.GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(playerGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            var camera = camGo.GetComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;

            var player = playerGo.AddComponent<Game.PlayerController>();
            player.head = camGo.transform;

            var weapons = playerGo.AddComponent<Game.WeaponSystem>();
            weapons.viewCamera = camera;

            var stateGo = new GameObject("Game State");
            var state = stateGo.AddComponent<Game.GameState>();

            // 哨戒炮：给 HUD 提供真实的受击数据
            BuildSentry(world.transform, new Vector3(-17f, 2.6f, 52f), new Color(1f, 0.42f, 0.42f));
            BuildSentry(world.transform, new Vector3(17f, 2.6f, 48f), new Color(1f, 0.55f, 0.30f));

            // UI 画布
            var canvasGo = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var ui = canvasGo.AddComponent<GameUi>();
            var volume = Object.FindFirstObjectByType<Volume>();
            ui.Setup(state, weapons, player, camera, volume != null ? volume.sharedProfile : null);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            EditorPrefs.SetBool(AutoOpenKey, true);
            Debug.Log($"[GlassUI] Arena 场景已生成：{ScenePath}");
        }

        static void BuildSentry(Transform parent, Vector3 position, Color color)
        {
            var root = new GameObject("Sentry");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            var baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "Base";
            baseBox.transform.SetParent(root.transform, false);
            baseBox.transform.localScale = new Vector3(1.1f, 0.6f, 1.1f);
            Paint(baseBox, new Color(0.20f, 0.23f, 0.28f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            head.transform.localScale = Vector3.one * 0.8f;
            Paint(head, new Color(0.24f, 0.27f, 0.32f));

            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0.55f, 0.34f);
            eye.transform.localScale = Vector3.one * 0.26f;
            GlassUI.Kill(eye.GetComponent<Collider>());
            Paint(eye, color, true);

            var sentry = root.AddComponent<Game.Sentry>();
            sentry.damage = 7f;
        }

        static void Paint(GameObject go, Color color, bool emissive = false)
        {
            var renderer = go.GetComponent<Renderer>();
            var material = new Material(renderer.sharedMaterial) { color = color };
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 3f);
            }
            renderer.sharedMaterial = material;
        }

        // ==================== 截图（HUD / 背包 / 设置）====================

        [MenuItem("Glass Demo/5. 截 Arena 的图", priority = 5)]
        public static void CaptureDefault() => Capture("/tmp/glass_arena", 1920, 1080);

        public static void CaptureAll()
        {
            var args = System.Environment.GetCommandLineArgs();
            string dir = "/tmp/glass_arena";
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

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            // 编辑器里 OnEnable 不跑，所以显式把程序化场景建出来
            var arena = Object.FindFirstObjectByType<ArenaBuilder>();
            if (arena != null) arena.Build();

            var ui = Object.FindFirstObjectByType<GameUi>();
            var cam = Object.FindFirstObjectByType<Camera>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (ui == null || cam == null || canvas == null)
            {
                Debug.LogError("[GlassUI] Arena 场景不完整，无法截图");
                return;
            }

            ui.BuildUi();
            ui.PrepareForCapture();
            Object.FindFirstObjectByType<WeaponSystem>()?.EnsureBuilt();

            // 第一人称平视机位。画布平面距离设成 1：相机渲染时 UI 走 LEqual 深度测试，
            // 平面越近，能被地面挡住的部分就越少（画布平面 1 米内没有可见地面），
            // 这样截出来等价于 Overlay 画布的真实观感。
            PlaneDistance = 1f;

            PlacePlayer(new Vector3(-3f, 0.1f, 2f), 3f);
            Render(cam, canvas, width, height, $"{dir}/01_gameplay.png");

            ui.OpenInventoryForCapture();
            Render(cam, canvas, width, height, $"{dir}/02_inventory.png");
            ui.ClosePanelsForCapture();

            ui.OpenSettingsForCapture();
            Render(cam, canvas, width, height, $"{dir}/03_settings.png");
            ui.ClosePanelsForCapture();

            // 换霰弹枪 + 靠近平靶区：展示武器条高亮与枪模变化
            var weapons = Object.FindFirstObjectByType<WeaponSystem>();
            if (weapons != null) weapons.Equip(1, true);
            PlacePlayer(new Vector3(-6f, 0.1f, 22f), 6f);
            Render(cam, canvas, width, height, $"{dir}/04_weapon_shotgun.png");

            // 贴近箱堆：看毛玻璃把细节糊掉、和周围清晰画面对比
            if (weapons != null) weapons.Equip(0, true);
            PlacePlayer(new Vector3(-13f, 0.1f, 4f), 12f);
            Render(cam, canvas, width, height, $"{dir}/05_glass_over_detail.png");
            Render(cam, canvas, width, height, $"{dir}/01_hud.png");

            ui.OpenInventoryForCapture();
            Render(cam, canvas, width, height, $"{dir}/02_inventory.png");
            ui.ClosePanelsForCapture();

            ui.OpenSettingsForCapture();
            Render(cam, canvas, width, height, $"{dir}/03_settings.png");
            ui.ClosePanelsForCapture();

            Debug.Log($"[GlassUI] Arena 截图完成，输出 {dir}");
        }

        static bool _dumped;

        static void DumpHudLayout(Camera cam)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            var canvasRt = (RectTransform)canvas.transform;

            Debug.Log($"[DIAG] 画布 rect={canvasRt.rect.size} pivot={canvasRt.pivot} " +
                      $"scaleFactor={canvas.scaleFactor} renderMode={canvas.renderMode} cam={canvas.worldCamera?.name}");

            var hud = canvasRt.Find("HUD");
            if (hud == null) { Debug.Log("[DIAG] 没找到 HUD 节点"); return; }

            foreach (Transform child in hud)
            {
                var rt = child as RectTransform;
                if (rt == null) continue;
                var image = child.GetComponent<Image>();
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                var bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                var tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                Debug.Log($"[DIAG] {child.name}: active={child.gameObject.activeInHierarchy} " +
                          $"rect={rt.rect.size} anchor={rt.anchorMin}-{rt.anchorMax} pos={rt.anchoredPosition} " +
                          $"color={(image != null ? image.color.ToString() : "no-image")} " +
                          $"屏幕矩形=({bl.x:0},{bl.y:0})-({tr.x:0},{tr.y:0})");
            }
        }

        static float PlaneDistance = 1f;

        static void PlacePlayer(Vector3 position, float yaw)
        {
            var player = Object.FindFirstObjectByType<Game.PlayerController>();
            if (player != null)
            {
                player.transform.position = position;
                player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) cam.transform.localRotation = Quaternion.identity;   // 平视
        }

        static void Render(Camera cam, Canvas canvas, int width, int height, string path)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousPlane = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = PlaneDistance;

            Canvas.ForceUpdateCanvases();
            if (!_dumped) { _dumped = true; DumpHudLayout(cam); }
            cam.Render();
            cam.Render();

            var previousActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previousActive;

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousPlane;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Debug.Log($"[GlassUI] 截图 {path}");
        }

        const string AutoOpenKey = "GlassUI.AutoOpened";
    }
}
