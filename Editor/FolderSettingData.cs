using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool
{
    public class FolderSettingData : ScriptableObject
    {
        [SerializeField]
        private Texture2D _baseTexture;

        [NonSerialized]
        private Dictionary<int, Texture> _cacheTexture = new Dictionary<int, Texture>();

        [NonSerialized]
        private Texture2D _bgTexture;

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
            internal List<string> Pattern;
        }
        
        [SerializeField]
        private List<Setting> _settings = new List<Setting>();
        
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
            if (_bgTexture != null) 
                return;
            
            var type = typeof(EditorGUIUtility);
            var backColor = type.InvokeMember(
                "GetDefaultBackgroundColor", 
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                null,
                null,
                null);

            var bgColor = backColor is Color ? (Color) backColor : default;
            var white = EditorGUIUtility.whiteTexture;
            var dst = new Texture2D(white.width, white.height, white.format, false);
            Graphics.CopyTexture(white, dst);
            try
            {
                for (var x = 0; x < dst.width; x++)
                    for (var y = 0; y < dst.height; y++)
                        dst.SetPixel(x, y, bgColor);

                dst.Apply();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            _bgTexture = dst;
        }
        
        /// <summary>
        /// フォルダ画像の上書き処理
        /// </summary>
        internal void DrawTexture(Rect rect, string path)
        {
            SetUp();
            
            if (!Match(path, out var index)) 
                return;
            
            // OneRow もしくは Two Row Left
            if (Math.Abs(rect.height - RowHeight) < float.Epsilon)
            {
                if (rect.x <= TwoColumnRightX) 
                    rect.xMin += 2f;
                
                rect.size = new Vector2(rect.height + 1, rect.height + 1);
            }
            else
            {
                rect.size = new Vector2(rect.width, rect.width);
            }

            // テクスチャ置き換えの場合
            if (_settings[index].ChangeTexture != null)
            {
                // もともとあるやつを非表示にはできないのでぽい色で上書き
                GUI.DrawTexture(rect, _bgTexture, ScaleMode.ScaleToFit);
            }

            GUI.DrawTexture(rect, GetCacheTexture(index, _settings[index]), ScaleMode.ScaleToFit);
        }

        private bool Match(string path, out int findIndex)
        {
            var fileName = System.IO.Path.GetFileName(path);
            for (var i = 0; i < _settings.Count; i++)
            {
                foreach (var pattern in _settings[i].Pattern)
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
                        if (_settings[i].ValidChild)
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
        private Texture GetCacheTexture(int index, Setting setting)
        {
            if (setting.ChangeTexture != null)
                return setting.ChangeTexture;
            
            if (_cacheTexture.TryGetValue(index, out var outTexture))
            {
                return outTexture;
            }

            if (_baseTexture == null)
                return null;

            // テクスチャのコピー
            var dst = new Texture2D(_baseTexture.width, _baseTexture.height, _baseTexture.format, false);
            Graphics.CopyTexture(_baseTexture, dst);
            try
            {
                for (var x = 0; x < dst.width; x++)
                {
                    for (var y = 0; y < dst.height; y++)
                    {
                        var baseColor = dst.GetPixel(x, y);

                        dst.SetPixel(x, y, baseColor * setting.Color);
                    }
                }
                
                dst.Apply();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            _cacheTexture.Add(index, dst);
            return dst;
        }
    }
}
