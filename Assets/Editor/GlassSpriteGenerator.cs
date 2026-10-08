using System.IO;
using UnityEditor;
using UnityEngine;

namespace GlassUI.EditorTools
{
    /// <summary>
    /// 程序化生成圆角贴图（写进 Assets/Resources/GlassSprites，打成九宫格）。
    ///
    /// 贴图里只有两样东西：
    ///   alpha = 形状覆盖率（含 1 像素抗锯齿）
    ///   RGB   = 纯白（颜色由控件自己的顶点色乘法决定）
    ///
    /// 早先还在 RGB 里塞了"边缘渐变 + 描边带"，双线性过滤时那些暗色会渗到边缘，
    /// 每个圆角形状就多出一圈边框 —— 现在没有了。
    ///
    /// 圆角半径是固定像素的，用 Image.pixelsPerUnitMultiplier 缩放到需要的半径，
    /// 所以任意半径都共用这几张贴图，也不会因为边框比控件大而被挤成椭圆。
    ///
    /// 菜单：Glass Demo / 0. 生成圆角贴图
    /// </summary>
    public static class GlassSpriteGenerator
    {
        const string Dir = "Assets/Resources/GlassSprites";

        static readonly int[] Radii = { 8, 14, 22, 30, 44 };

        [MenuItem("Glass Demo/0. 生成圆角贴图", priority = 0)]
        public static void Generate()
        {
            Directory.CreateDirectory(Dir);
            foreach (var radius in Radii) SaveRounded($"round{radius:00}", radius);
            AssetDatabase.Refresh();
            Debug.Log($"[GlassUI] 圆角贴图已生成到 {Dir}（半径 {string.Join("/", Radii)}，纯白 RGB + 形状 alpha）");
        }

        static void SaveRounded(string name, int radius)
        {
            int size = radius * 2 + 8;
            int border = radius + 2;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float r = Mathf.Max(0.5f, half - 1f);
            float corner = Mathf.Min(radius, r);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - half;
                    float py = y + 0.5f - half;

                    // 圆角矩形有符号距离场：<0 在里面
                    float qx = Mathf.Abs(px) - (r - corner);
                    float qy = Mathf.Abs(py) - (r - corner);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                              + Mathf.Min(Mathf.Max(qx, qy), 0f) - corner;

                    float inside = Mathf.Clamp01(0.5f - d);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(inside * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            var path = $"{Dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
        }
    }
}
