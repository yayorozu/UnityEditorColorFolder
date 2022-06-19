using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Yorozu.EditorTool.ColorFolder
{
    /// <summary>
    /// どこか行ったときようにPing
    /// </summary>
    public class FolderSettingData : ScriptableObject
    {
        [NonSerialized]
        private Dictionary<int, Texture> _cacheTexture = new Dictionary<int, Texture>();
        private Dictionary<int, Texture> _cacheLargeTexture = new Dictionary<int, Texture>();

        [NonSerialized]
        private Texture2D _bgTexture;
        [NonSerialized]
        private Texture2D _folderTexture;
        [NonSerialized]
        private Texture2D _folderLargeTexture;

        [Serializable]
        internal class Setting
        {
            [SerializeField]
            internal Color Color = new Color(1, 1, 1, 0.6f);
            [SerializeField]
            internal Texture ChangeTexture;
            /// <summary>
            /// 子供のディレクトリを有効にするか
            /// </summary>
            [SerializeField] 
            internal bool ValidChild;
            /// <summary>
            /// フォルダ以外のアセットの背景色を変更するか？
            /// </summary>
            [SerializeField] 
            internal bool ValidOtherAsset;
            [SerializeField] 
            internal float BackgroundColorAlpha = 0.05f;
            [SerializeField]
            internal List<string> Pattern = new List<string> {""};

            [NonSerialized]
            internal List<string> RootPatterns;
            
            [NonSerialized]
            private Color? _bgColor;

            internal Color BGColor
            {
                get
                {
                    _bgColor ??= new Color(Color.r, Color.g, Color.b, BackgroundColorAlpha);
                    return _bgColor.Value;
                }
            }

            /// <summary>
            /// パラメータが変わったらデータを消す
            /// </summary>
            internal void Reset()
            {
                _bgColor = null;
                RootPatterns = null;
            }
        }
        
        [FormerlySerializedAs("_settings")]
        [SerializeField]
        internal List<Setting> Settings = new List<Setting>();
        
        private const float RowHeight = 16f;
        /// <summary>
        /// Two Column Right の Item の座標は Xが14から始まる
        /// Left は16
        /// </summary>
        private const float TwoColumnRightX = 14f;

        internal void Reset()
        {
            _bgTexture = null;
            _cacheTexture.Clear();   
            _cacheLargeTexture.Clear();
            for (var i = 0; i < Settings.Count; i++)
            {
                Settings[i].Reset();
            }
        }

        /// <summary>
        /// 現在のモードの背景色のテクスチャを作成しておく
        /// </summary>
        private void SetUp()
        {
            if (_bgTexture == null) 
                _bgTexture = Utility.LoadBackgroundTexture();

            if (_folderTexture == null) 
                _folderTexture = Utility.LoadFolderTexture(false);
                
            if (_folderLargeTexture == null) 
                _folderLargeTexture = Utility.LoadFolderTexture(true);
        }

        
        /// <summary>
        /// フォルダ画像の上書き処理
        /// </summary>
        internal void DrawTexture(Rect rect, string path)
        {
            SetUp();
            
            if (!Match(path, out var index)) 
                return;

            if (!AssetDatabase.IsValidFolder(path))
            {
                if (Settings[index].ValidOtherAsset)
                {
                    EditorGUI.DrawRect(rect, Settings[index].BGColor);
                }
                return;
            }

            var isLarge = false;
            // OneRow もしくは Two Row Left
            if (Math.Abs(rect.height - RowHeight) < float.Epsilon)
            {
                rect.yMin -= 1;
                rect.size = new Vector2(rect.height, rect.height + 0.5f);
                if (rect.x <= TwoColumnRightX)
                {
                    rect.xMin += 2f;
                    rect.xMax += 3f;
                }
                else
                {
                    rect.xMin -= 0.5f;
                    rect.xMax += 0.8f;
                }
            }
            else
            {
                isLarge = true;
                rect.size = new Vector2(rect.width, rect.width);
            }

            // テクスチャ置き換えの場合
            if (Settings[index].ChangeTexture != null)
            {
                // もともとあるやつを非表示にはできないのでぽい色で上書き
                GUI.DrawTexture(rect, _bgTexture, ScaleMode.StretchToFill);
            }

            var texture = GetCacheTexture(index, Settings[index], isLarge);
            if (texture != null)
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
        }

        private bool Match(string path, out int findIndex)
        {
            var fileName = System.IO.Path.GetFileName(path);
            for (var i = 0; i < Settings.Count; i++)
            {
                // キャッシュ
                if (Settings[i].ValidChild && Settings[i].RootPatterns == null)
                {
                    Settings[i].RootPatterns = new List<string>(Settings[i].Pattern.Count);
                    foreach (var pattern in Settings[i].Pattern)
                    {
                        var rootPattern = pattern;
                        if (pattern.StartsWith("^"))
                            rootPattern = rootPattern.Substring(1);
                        else
                            rootPattern = ".*" + rootPattern;
                            
                        if (pattern.EndsWith("$"))
                            rootPattern = rootPattern.Substring(0, rootPattern.Length - 1);
                        else
                            rootPattern += ".*";
                        
                        Settings[i].RootPatterns.Add(rootPattern);
                    }
                }

                for (var j = 0; j < Settings[i].Pattern.Count; j++)
                {
                    var pattern = Settings[i].Pattern[j];
                    if (string.IsNullOrEmpty(pattern))
                        continue;

                    // 正規表現入力中はエラーになることがあるので無視する
                    try
                    {
                        if (Regex.IsMatch(fileName, pattern) ||
                            // 子供のディレクトリも色変える場合
                            Settings[i].ValidChild && Regex.IsMatch(path, $"/{Settings[i].RootPatterns[j]}/")
                        )
                        {
                            findIndex = i;
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            findIndex = -1;
            return false;
        }
        
        /// <summary>
        /// テクスチャをキャッシュからロード
        /// </summary>
        private Texture GetCacheTexture(int index, Setting setting, bool isLarge)
        {
            if (setting.ChangeTexture != null)
                return setting.ChangeTexture;

            var targetDic = isLarge ? _cacheLargeTexture : _cacheTexture;
            if (targetDic.TryGetValue(index, out var outTexture))
            {
                if (outTexture != null)
                    return outTexture;

                targetDic.Remove(index);
            }

            var src = isLarge ? _folderLargeTexture : _folderTexture; 
            if (src == null)
                return null;

            // テクスチャのコピー
            try
            {
                var dst = src.Copy();
                for (var x = 0; x < dst.width; x++)
                {
                    for (var y = 0; y < dst.height; y++)
                    {
                        var baseColor = dst.GetPixel(x, y);

                        dst.SetPixel(x, y, baseColor * setting.Color);
                    }
                }
                
                dst.Apply();
                targetDic.Add(index, dst);
                return dst;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
    }
}
