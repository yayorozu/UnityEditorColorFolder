using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            internal Color Color = new Color(1, 1, 1, 1);
            [SerializeField]
            internal Texture ChangeTexture;
            /// <summary>
            /// 子供のディレクトリを有効にするか
            /// </summary>
            [SerializeField] 
            internal bool ValidChild;
            [SerializeField]
            internal List<string> Pattern = new List<string> {""};
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
                foreach (var pattern in Settings[i].Pattern)
                {
                    if (string.IsNullOrEmpty(pattern))
                        continue;

                    // 正規表現入力中はエラーになることがあるので無視する
                    try
                    {
                        if (Regex.IsMatch(fileName, pattern))
                        {
                            findIndex = i;
                            return true;
                        }

                        // 子供のディレクトリも色変える場合は^$を削除して判定する
                        if (Settings[i].ValidChild)
                        {
                            var rootPattern = pattern;
                            if (pattern.StartsWith("^"))
                                rootPattern = rootPattern.Substring(1);
                            if (pattern.EndsWith("$"))
                                rootPattern = rootPattern.Substring(0, rootPattern.Length - 1);
                            if (Regex.IsMatch(path, $"/{rootPattern}/"))
                            {
                                findIndex = i;
                                return true;
                            }
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
                return outTexture;
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
