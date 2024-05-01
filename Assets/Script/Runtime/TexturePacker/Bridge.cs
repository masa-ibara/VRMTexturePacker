using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using VRM;
using VRMShaders;

namespace MsaI.Runtime.TexturePacker
{
    public static class Bridge
    {
        static RuntimeGltfInstance gltfInstance;
        async internal static Task<bool> LoadVrm(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Path is empty");
                return false;
            }
            if (gltfInstance != null)
            {
                gltfInstance.Dispose();
            }
            var instance = await VrmUtility.LoadAsync(path);
            if (instance != null)
            {
                SetupGltfInstance(instance, Path.GetFileNameWithoutExtension(path));
                return true;
            }
            return false;
        }

        async internal static Task<bool> LoadBytesVrm(string path, byte[] bytes)
        {
            if (gltfInstance != null)
            {
                gltfInstance.Dispose();
            }
            var instance = await VrmUtility.LoadBytesAsync(path, bytes);
            if (instance != null)
            {
                SetupGltfInstance(instance, Path.GetFileNameWithoutExtension(path));
                return true;
            }
            return false;
        }
        
        static void SetupGltfInstance(RuntimeGltfInstance instance, string name)
        {
            gltfInstance = instance;
            gltfInstance.name = name;
        }

        internal static void Pack(bool highCompression = false)
        {
            Packer.PackAssets(gltfInstance.Root, highCompression);
        }
        
        internal static (byte[], string) Export()
        {
            var fileName = gltfInstance.name + "_atlased.vrm";
            var vrm = VRMExporter.Export(new UniGLTF.GltfExportSettings(), gltfInstance.gameObject, new RuntimeTextureSerializer());
            var bytes = vrm.ToGlbBytes();
            return (bytes, fileName);
        }
        
        internal static Texture[] ReadTextures()
        {
            var textures = new List<Texture>();
            foreach (var renderer in gltfInstance.Root.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material.mainTexture != null)
                    {
                        textures.Add(material.mainTexture);
                    }
                }
            }
            return textures.Distinct().ToArray();
        }
        
        internal static Material[] ReadMaterials()
        {
            var materials = new List<Material>();
            foreach (var renderer in gltfInstance.Root.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    materials.Add(material);
                }
            }
            return materials.Distinct().ToArray();
        }
    }
}
