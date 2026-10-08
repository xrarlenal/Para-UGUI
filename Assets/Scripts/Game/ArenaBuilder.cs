using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GlassUI.Game
{
    /// <summary>
    /// 战术训练场（blockout）：模块化墙、箱堆、立柱、坡道、发光灯带 + URP 后处理。
    /// 自己搭的原因见 README：官方免费 3D 游戏场景只在 Asset Store（要账号点下载），
    /// 官方开源 demo 是 2020.3 + 全程 Git LFS，升到 2022.3 风险大且是海岛场景，跟"战斗+换武器"不搭。
    /// 这个场景是版本匹配、授权完全干净的，以后想换官方资源包直接替换环境物体即可（UI 不依赖场景）。
    /// </summary>
    public class ArenaBuilder : MonoBehaviour
    {
        public int seed = 20261008;
        public bool buildOnEnable = true;

        static readonly Color WallColor = new Color(0.42f, 0.47f, 0.56f);
        static readonly Color WallColorB = new Color(0.30f, 0.35f, 0.44f);
        static readonly Color CrateColor = new Color(0.62f, 0.36f, 0.20f);
        static readonly Color CrateColorB = new Color(0.30f, 0.52f, 0.66f);

        void OnEnable()
        {
            if (buildOnEnable) Build();
        }

        /// <summary>幂等：先清掉上次生成的子物体。</summary>
        public void Build()
        {
            GlassUI.Clear(transform);

            var rng = new System.Random(seed);
            BuildGround();
            BuildOuterWalls();
            BuildCover(rng);
            BuildPillarsAndRamps();
            BuildLightStrips();
            BuildTargets();
            BuildPostProcessing();
            GlassUI.Refresh();
        }

        // ---------------- 地面与外墙 ----------------

        void BuildGround()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(transform, false);
            floor.transform.position = new Vector3(0f, -0.5f, 20f);
            floor.transform.localScale = new Vector3(60f, 1f, 80f);
            Paint(floor, new Color(0.30f, 0.34f, 0.42f));

            // 地面棋盘格：一是能看出自己在移动，二是磨砂玻璃有东西可糊（纯色地面糊完还是纯色）
            var tiles = new GameObject("Floor Tiles").transform;
            tiles.SetParent(transform, false);
            var tileA = new Color(0.26f, 0.30f, 0.37f);
            var tileB = new Color(0.35f, 0.40f, 0.49f);
            const int tileCount = 14;
            const float tileSize = 4.2f;
            for (int x = -tileCount; x <= tileCount; x++)
            {
                for (int z = 0; z <= 18; z++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile {x}_{z}";
                    tile.transform.SetParent(tiles, false);
                    tile.transform.position = new Vector3(x * tileSize, 0.005f, z * tileSize - 2f);
                    tile.transform.localScale = new Vector3(tileSize * 0.98f, 0.02f, tileSize * 0.98f);
                    Paint(tile, (x + z) % 2 == 0 ? tileA : tileB);
                    DestroyImmediate(tile.GetComponent<Collider>());
                }
            }

            // 地面网格线：用细长条做视觉参考，方便看出移动和透视
            var lineColor = new Color(0.30f, 0.52f, 0.78f, 1f);
            for (int i = -6; i <= 6; i++)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = $"GridLine X{i}";
                line.transform.SetParent(transform, false);
                line.transform.position = new Vector3(i * 5f, 0.01f, 20f);
                line.transform.localScale = new Vector3(0.06f, 0.02f, 80f);
                Paint(line, lineColor);
                DestroyImmediate(line.GetComponent<Collider>());
            }
        }

        void BuildOuterWalls()
        {
            MakeBox("Wall North", new Vector3(0f, 4f, 58f), new Vector3(60f, 8f, 1f), WallColor, true);
            MakeBox("Wall South", new Vector3(0f, 4f, -18f), new Vector3(60f, 8f, 1f), WallColor, true);
            MakeBox("Wall West", new Vector3(-30f, 4f, 20f), new Vector3(1f, 8f, 76f), WallColorB, true);
            MakeBox("Wall East", new Vector3(30f, 4f, 20f), new Vector3(1f, 8f, 76f), WallColorB, true);
        }

        // ---------------- 掩体（箱堆）----------------

        void BuildCover(System.Random rng)
        {
            var cover = new GameObject("Cover").transform;
            cover.SetParent(transform, false);

            // 手工摆几组掩体：位置是设计过的，保证有远近层次和对峙感
            MakeCrateStack(cover, new Vector3(-12f, 0f, 10f), 2, 1, CrateColor);
            MakeCrateStack(cover, new Vector3(10f, 0f, 14f), 2, 2, CrateColorB);
            MakeCrateStack(cover, new Vector3(-4f, 0f, 26f), 3, 1, CrateColor);
            MakeCrateStack(cover, new Vector3(6f, 0f, 30f), 2, 2, CrateColorB);
            MakeCrateStack(cover, new Vector3(-16f, 0f, 34f), 1, 2, CrateColorB);
            MakeCrateStack(cover, new Vector3(16f, 0f, 38f), 2, 1, CrateColor);
            MakeCrateStack(cover, new Vector3(0f, 0f, 44f), 3, 2, CrateColor);
            MakeCrateStack(cover, new Vector3(-22f, 0f, 22f), 2, 3, CrateColor);
            MakeCrateStack(cover, new Vector3(22f, 0f, 26f), 1, 1, CrateColorB);

            // 零散的桶（圆柱）：给画面加一点曲线，模糊之后更好看
            for (int i = 0; i < 14; i++)
            {
                float x = (float)(rng.NextDouble() * 52.0 - 26.0);
                float z = (float)(rng.NextDouble() * 52.0 + 2.0);
                if (Mathf.Abs(x) < 4f && z < 16f) continue;   // 出生点附近留空

                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = $"Barrel {i}";
                barrel.transform.SetParent(cover, false);
                barrel.transform.position = new Vector3(x, 0.6f, z);
                barrel.transform.localScale = new Vector3(0.75f, 0.6f, 0.75f);
                Paint(barrel, rng.NextDouble() < 0.5 ? CrateColor : CrateColorB);
            }
        }

        void MakeCrateStack(Transform parent, Vector3 origin, int wide, int tall, Color color)
        {
            const float size = 1.6f;
            for (int x = 0; x < wide; x++)
            {
                for (int y = 0; y < tall; y++)
                {
                    var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crate.name = $"Crate {origin.x:0}_{x}_{y}";
                    crate.transform.SetParent(parent, false);
                    crate.transform.position = origin + new Vector3(x * size, size * 0.5f + y * size, 0f);
                    crate.transform.localScale = Vector3.one * (size * 0.96f);
                    crate.transform.rotation = Quaternion.Euler(0f, (x + y) * 7f % 25f - 12f, 0f);
                    Paint(crate, color * (0.85f + 0.15f * ((x + y) % 2)));
                }
            }
        }

        // ---------------- 立柱与坡道 ----------------

        void BuildPillarsAndRamps()
        {
            var group = new GameObject("Structures").transform;
            group.SetParent(transform, false);

            MakeBox("Pillar A", new Vector3(-20f, 3f, 14f), new Vector3(1.4f, 6f, 1.4f), WallColorB, true);
            MakeBox("Pillar B", new Vector3(20f, 3f, 18f), new Vector3(1.4f, 6f, 1.4f), WallColorB, true);
            MakeBox("Pillar C", new Vector3(-14f, 3f, 44f), new Vector3(1.4f, 6f, 1.4f), WallColorB, true);
            MakeBox("Pillar D", new Vector3(14f, 3f, 48f), new Vector3(1.4f, 6f, 1.4f), WallColorB, true);

            // 一段高台 + 坡道：给"战斗"提供高低差
            MakeBox("Platform", new Vector3(-18f, 1.5f, 52f), new Vector3(12f, 3f, 10f), WallColor, true);
            var ramp = MakeBox("Ramp", new Vector3(-9f, 1.2f, 52f), new Vector3(7f, 0.5f, 5f), WallColorB, true);
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, 17f);

            // 半高矮墙：可以直接躲在后面射击
            MakeBox("LowWall A", new Vector3(0f, 0.9f, 20f), new Vector3(14f, 1.8f, 0.8f), WallColor, true);
            MakeBox("LowWall B", new Vector3(-8f, 0.9f, 36f), new Vector3(10f, 1.8f, 0.8f), WallColor, true);
            MakeBox("LowWall C", new Vector3(12f, 0.9f, 42f), new Vector3(10f, 1.8f, 0.8f), WallColor, true);
        }

        void BuildLightStrips()
        {
            // 发光灯带：给画面加冷色高光，模糊之后会出现漂亮的光斑
            var group = new GameObject("Light Strips").transform;
            group.SetParent(transform, false);
            var glow = new Color(0.45f, 0.78f, 1f);

            for (int i = 0; i < 6; i++)
            {
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"Strip {i}";
                strip.transform.SetParent(group, false);
                strip.transform.position = new Vector3(-25f + i * 10f, 7.2f, 57f);
                strip.transform.localScale = new Vector3(6f, 0.25f, 0.5f);
                Paint(strip, glow, true);
                DestroyImmediate(strip.GetComponent<Collider>());
            }

            var lightGo = new GameObject("Area Lights", typeof(Light));
            lightGo.transform.SetParent(transform, false);
            int index = 0;
            foreach (var pos in new[] { new Vector3(-18f, 6f, 24f), new Vector3(18f, 6f, 24f), new Vector3(0f, 6f, 48f) })
            {
                var lamp = new GameObject($"Point Light {index++}", typeof(Light));
                lamp.transform.SetParent(lightGo.transform, false);
                lamp.transform.position = pos;
                var light = lamp.GetComponent<Light>();
                light.type = LightType.Point;
                light.range = 26f;
                light.intensity = 2.2f;
                light.color = new Color(0.62f, 0.82f, 1f);
                light.shadows = LightShadows.None;
            }
        }

        void BuildTargets()
        {
            var group = new GameObject("Targets").transform;
            group.SetParent(transform, false);

            for (int i = 0; i < 6; i++)
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = $"Target {i + 1}";
                target.transform.SetParent(group, false);
                target.transform.position = new Vector3(-12f + i * 5f, 1.4f, 34f + (i % 2) * 6f);
                target.transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);
                target.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Paint(target, new Color(0.92f, 0.94f, 0.98f));

                // 可击中的靶子：命中会被打飞再复位
                var hit = target.AddComponent<ShootTarget>();
                hit.accent = new Color(0.28f, 0.55f, 1f);
            }
        }

        void BuildPostProcessing()
        {
            var volumeGo = new GameObject("Global Volume", typeof(Volume));
            volumeGo.transform.SetParent(transform, false);
            var volume = volumeGo.GetComponent<Volume>();
            volume.isGlobal = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Arena Profile";
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.85f);
            bloom.threshold.Override(0.92f);
            bloom.scatter.Override(0.65f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.saturation.Override(8f);
        }

        // ---------------- 小工具 ----------------

        GameObject MakeBox(string name, Vector3 position, Vector3 scale, Color color, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Paint(go, color);
            if (!solid) DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Paint(GameObject go, Color color, bool emissive = false)
        {
            var renderer = go.GetComponent<Renderer>();
            var material = new Material(renderer.sharedMaterial);
            material.color = color;
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f);
            }
            renderer.sharedMaterial = material;
        }
    }
}
