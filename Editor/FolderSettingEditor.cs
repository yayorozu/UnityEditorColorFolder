using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool.Folder
{
    [CustomEditor(typeof(FolderSettingData))]
    public class FolderSettingEditor : Editor
    {
        private FolderSettingData _data;
        private void Awake()
        {
            _data = target as FolderSettingData;
        }

        public override void OnInspectorGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                base.OnInspectorGUI();
                if (check.changed)
                {
                    _data.Reset();
                    var projectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                    var projects = Resources.FindObjectsOfTypeAll(projectBrowserType);
                    foreach (EditorWindow project in projects)
                    {
                        project.Repaint();
                    }
                }
            }
        }
    }
}
