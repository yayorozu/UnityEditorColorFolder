using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool.ColorFolder
{
    [CustomEditor(typeof(FolderSettingData))]
    public class FolderSettingEditor : Editor
    {
        private static class Styles
        {
            internal static GUIContent Plus; 
            internal static GUIContent Minus;
            internal static GUIContent Delete;
            internal static GUIStyle Style;
            internal static GUILayoutOption Width;
            internal static GUILayoutOption Height;

            static Styles()
            {
                Plus = EditorGUIUtility.TrIconContent("Toolbar Plus");
                Minus = EditorGUIUtility.TrIconContent("Toolbar Minus");
                Delete = EditorGUIUtility.TrIconContent("d_TreeEditor.Trash");
                Style = new GUIStyle("RL FooterButton");
                Width = GUILayout.Width(16f);
                Height = GUILayout.Height(EditorGUIUtility.singleLineHeight);
            }
        }
        
        private FolderSettingData _data;
        private void Awake()
        {
            _data = target as FolderSettingData;
        }

        public override void OnInspectorGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                DrawCustomGUI();
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

        private void DrawCustomGUI()
        {
            if (GUILayout.Button("Add Setting"))
            {
                _data.Settings.Add(new FolderSettingData.Setting());
                GUIUtility.ExitGUI();
            }
            
            for (var i = 0; i < _data.Settings.Count; i++)
            {
                var setting = _data.Settings[i];
                
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(i.ToString(), EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(Styles.Delete, Styles.Style))
                        {
                            _data.Settings.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.Space(15);
                        using (new EditorGUI.DisabledScope(i == 0))
                        {
                            if (GUILayout.Button("▲", Styles.Style, Styles.Width))
                            {
                                var temp = _data.Settings[i - 1];
                                _data.Settings[i - 1] = _data.Settings[i];
                                _data.Settings[i] = temp;
                            }
                        }

                        EditorGUILayout.Space(3);
                        using (new EditorGUI.DisabledScope(i + 1 == _data.Settings.Count))
                        {
                            if (GUILayout.Button("▼", Styles.Style, Styles.Width))
                            {
                                var temp = _data.Settings[i + 1];
                                _data.Settings[i + 1] = _data.Settings[i];
                                _data.Settings[i] = temp;
                            }
                        }
                    }
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (setting.ChangeTexture == null)
                            setting.Color = EditorGUILayout.ColorField("Color", setting.Color);

                        setting.ChangeTexture = (Texture) EditorGUILayout.ObjectField(
                            "Override Texture",
                            setting.ChangeTexture,
                            typeof(Texture), 
                            false,
                             Styles.Height);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Apply Child Folder");
                            GUILayout.FlexibleSpace();
                            setting.ValidChild = EditorGUILayout.Toggle(GUIContent.none, setting.ValidChild);
                        }

                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField(nameof(setting.Pattern), EditorStyles.boldLabel);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(Styles.Plus, Styles.Style, Styles.Width))
                                {
                                    setting.Pattern.Add("");
                                    GUIUtility.ExitGUI();
                                }
                            }

                            for (var j = 0; j < setting.Pattern.Count; j++)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    setting.Pattern[j] = EditorGUILayout.TextField(GUIContent.none, setting.Pattern[j]);
                                    if (GUILayout.Button(Styles.Minus, Styles.Style, Styles.Width))
                                    {
                                        setting.Pattern.RemoveAt(j);
                                        GUIUtility.ExitGUI();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
