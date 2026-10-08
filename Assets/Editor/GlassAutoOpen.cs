using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlassUI.EditorTools
{
    /// <summary>
    /// 打开工程时兜底：恢复到空场景（没有路径的 Untitled）就自动打开主场景。
    /// 原因：批处理脚本 NewScene 出来的临时空场景会留在 Library 的「上次打开的场景」里，
    /// 下次用界面打开工程就落到空场景上，按 Play 什么都不发生。
    /// </summary>
    [InitializeOnLoad]
    public static class GlassAutoOpen
    {
        const string ScenePath = "Assets/Scenes/GlassTest.unity";
        static string Key => "GlassUI.AutoOpened." + Application.dataPath.GetHashCode();

        static GlassAutoOpen()
        {
            EditorApplication.delayCall += TryOpen;
        }

        static void TryOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetBool(Key, false)) return;
            if (!File.Exists(ScenePath)) return;

            var active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path)) return;

            EditorPrefs.SetBool(Key, true);
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log($"[GlassUI] 当前是空场景，已自动打开：{ScenePath}");
        }
    }
}
