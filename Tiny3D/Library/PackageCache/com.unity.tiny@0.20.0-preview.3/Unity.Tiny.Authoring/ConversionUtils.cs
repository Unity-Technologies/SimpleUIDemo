using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine.Assertions;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.TinyConversion
{
    public static partial class ConversionUtils
    {
        public static unsafe void SetBufferAsString<T>(DynamicBuffer<T> buf, string s) where T : struct
        {
            Assert.AreEqual(sizeof(char), TypeManager.GetTypeInfo<T>().ElementSize);

            buf.ResizeUninitialized(s.Length);
            fixed (char* ptr = s)
            {
                UnsafeUtility.MemCpy(buf.GetUnsafePtr(), ptr, s.Length * sizeof(char));
            }
        }

        public static Tiny.Color ToTiny(this UnityEngine.Color c)
        {
            return new Tiny.Color(c.r, c.g, c.b, c.a);
        }

        public static Tiny.Rect ToTiny(this UnityEngine.Rect rect)
        {
            return new Tiny.Rect(rect.x, rect.y, rect.width, rect.height);
        }

        public static void ExportSource(Stream writer, DirectoryInfo root, UnityEngine.Object uobject)
        {
            //Export source
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(uobject);
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new NullReferenceException($"Asset path for object '{uobject.name}' not found in AssetDatabase.");
            }

            var srcFile = new FileInfo(Path.Combine(root.FullName, assetPath));
            if (!srcFile.Exists)
            {
                throw new FileNotFoundException(srcFile.FullName);
            }
            FileStream fs = writer as FileStream;
            writer.Close();
            srcFile.CopyTo(fs.Name, true);
            fs.Close();
        }
    }
}
