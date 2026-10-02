#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CodeDrive.EditorTools
{
    /// <summary>
    /// Editor tool to automatically adjust Game View aspect/scale in Unity Editor
    /// to eliminate the "Screen position out of view frustum" letterbox warning.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewAspectFixer
    {
        static GameViewAspectFixer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                FixGameViewWindow();
            }
        }

        [MenuItem("Tools/Fix Game View Letterbox (Fix Frustum Warnings)")]
        public static void FixGameViewWindow()
        {
            try
            {
                Type gameViewType = Type.GetType("UnityEditor.GameView, UnityEditor");
                if (gameViewType == null) return;

                EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null) return;

                MethodInfo sizeToFitMethod = gameViewType.GetMethod("SizeToFit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sizeToFitMethod != null)
                {
                    sizeToFitMethod.Invoke(gameView, null);
                }

                gameView.Repaint();
            }
            catch
            {
                // Ignore silent editor window adjustments
            }
        }
    }
}
#endif
