using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool.ColorFolder
{
    public static class ColorFolderGUI
    {
        [InitializeOnLoadMethod]
        private static void ProjectWindow()
        {
            EditorApplication.projectWindowItemOnGUI -= ProjectWindowGUI;
            EditorApplication.projectWindowItemOnGUI += ProjectWindowGUI;
        }

        /// <summary>
        /// 後で作成したらコールバックないので
        /// </summary>
        internal static void Refresh()
        {
            if (_setting != null) 
                return;
            
            ProjectWindow();
        }

        private static FolderSettingData _setting;

        private static void Initialize()
        {
            if (_setting != null)
                return;
            
            var findGuids = AssetDatabase.FindAssets($"t:{nameof(FolderSettingData)}");
            // データないなら描画処理を消す
            if (findGuids.Length <= 0)
            {
                EditorApplication.projectWindowItemOnGUI -= ProjectWindowGUI;
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(findGuids[0]);
            _setting = AssetDatabase.LoadAssetAtPath<FolderSettingData>(path);
        }

        private static void ProjectWindowGUI(string guid, Rect rect)
        {
            Initialize();
            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (_setting == null)
            {
                EditorApplication.projectWindowItemOnGUI -= ProjectWindowGUI;
                return;
            }
            
            _setting.DrawTexture(rect, path);
        }
    }
}