using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yorozu.EditorTool.ColorFolder
{
    public static class Utility
    {
        /// <summary>
        /// Temp ディレクトリ内の保存パスを取得
        /// </summary>
        private static string TempPath()
        {
            var path = FileUtil.GetUniqueTempPathInProject();
            var tempPath = Directory.GetParent(path);
            var tempSavePath = Path.Combine(tempPath.FullName, "ColorFolder");
            if (!Directory.Exists(tempSavePath))
                Directory.CreateDirectory(tempSavePath);
            
            return tempSavePath;
        }
        
        
        internal static Texture2D LoadFolderTexture(bool isLarge)
        {
            var path = TempPath();
            var fileName = isLarge ? "FolderLargeIcon.png" : "FolderIcon.png";
            var filePath = Path.Combine(path, fileName);
            // なかったら作る
            if (!File.Exists(filePath))
            {
                var tempTexture = isLarge ? 
                    CreateFolderLargeTexture() : 
                    CreateFolderTexture();
                File.WriteAllBytes(filePath, tempTexture.EncodeToPNG());
            }
            var texture = new Texture2D(0, 0, TextureFormat.ARGB32, true);
            texture.LoadImage(File.ReadAllBytes(filePath));
            
            return texture;
        }

        /// <summary>
        /// フォルダ用の画像を作成する
        /// </summary>
        private static Texture2D CreateFolderTexture()
        {
            try
            {
                var foldContent = EditorGUIUtility.IconContent("Folder On Icon");
                var fold2Content = EditorGUIUtility.IconContent("d_FolderOpened Icon");
                var folderTexture = Copy(foldContent.image);
                var folderOpenTexture = Copy(fold2Content.image);

                var sizeDiff = new Vector2(
                    folderTexture.width / (float)folderOpenTexture.width, 
                    folderTexture.height / (float)folderOpenTexture.height
                );

                for (var x = 0; x < folderTexture.width; x++)
                {
                    for (var y = 0; y < folderTexture.height; y++)
                    {
                        var baseColor = folderTexture.GetPixel(x, y);
                        var nextColor = folderOpenTexture.GetPixel(
                            Mathf.FloorToInt(x / sizeDiff.x), 
                            Mathf.FloorToInt(y / sizeDiff.y)
                        );
                        
                        //baseColor[3] = Mathf.Max(nextColor[3], baseColor[3]);
                        for (var i = 0; i < 3; i++)
                            baseColor[i] = 1f;

                        folderTexture.SetPixel(x, y, baseColor);
                    }
                }
                
                folderTexture.Apply();
                return folderTexture;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
        
        /// <summary>
        /// フォルダ用の画像を作成する
        /// </summary>
        private static Texture2D CreateFolderLargeTexture()
        {
            try
            {
                var foldContent = EditorGUIUtility.IconContent("Folder On Icon");
                var folderTexture = Copy(foldContent.image);
                
                for (var x = 0; x < folderTexture.width; x++)
                {
                    for (var y = 0; y < folderTexture.height; y++)
                    {
                        var baseColor = folderTexture.GetPixel(x, y);
                        for (var i = 0; i < 3; i++)
                            baseColor[i] = 1f;

                        folderTexture.SetPixel(x, y, baseColor);
                    }
                }
                
                folderTexture.Apply();
                return folderTexture;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
        
        /// <summary>
        /// テクスチャをコピーする
        /// Require Try Catch
        /// </summary>
        internal static Texture2D Copy(this Texture src)
        {
            var dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, src.mipmapCount > 1);
            Graphics.CopyTexture(src, dst);
            return dst;
        }

        /// <summary>
        /// テクスチャを表示する際にアルファがあると下が見えるのでそれ用に用意
        /// </summary>
        internal static Texture2D LoadBackgroundTexture()
        {
            // 現在のスキンの背景色を取得
            var type = typeof(EditorGUIUtility);
            var backColor = type.InvokeMember(
                "GetDefaultBackgroundColor",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                null,
                null,
                null);

            var bgColor = backColor is Color ? (Color) backColor : default;
            try
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
                for (var x = 0; x < texture.width; x++)
                    for (var y = 0; y < texture.height; y++)
                        texture.SetPixel(x, y, bgColor);

                texture.Apply();
                return texture;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
    }
}