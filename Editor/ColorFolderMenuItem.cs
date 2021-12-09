using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool.ColorFolder
{
    internal class ColorFolderMenuItem
    {
        [MenuItem("Tools/ColorFolder/PingFolderSetting")]
        private static void Select()
        {
            var findGuids = AssetDatabase.FindAssets($"t:{nameof(FolderSettingData)}");
            if (findGuids.Length <= 0)
                return;

            var path = AssetDatabase.GUIDToAssetPath(findGuids[0]);

            var data = AssetDatabase.LoadAssetAtPath<FolderSettingData>(path);
            EditorGUIUtility.PingObject(data);
            Selection.activeObject = data;
        }
        
        /// <summary>
        /// 設定ファイルの生成
        /// </summary>
        [MenuItem("Tools/ColorFolder/CreateFolderSetting")]
        private static void Create()
        {
            var path =EditorUtility.SaveFilePanelInProject("Select Create Asset Path", nameof(FolderSettingData), "asset", "");
            if (string.IsNullOrEmpty(path))
                return;

            var asset = ScriptableObject.CreateInstance<FolderSettingData>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.Refresh();
        }   
    }
}