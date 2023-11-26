using System.IO;
using UniGLTF;
using UnityEngine;
using VRM;
using VRMShaders;

namespace MsaI.Runtime.TexturePacker
{
    public static class Bridge
    {
        static RuntimeGltfInstance gltfInstance;
        async internal static void LoadVrm(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Path is empty");
                return;
            }
            if (gltfInstance != null)
            {
                gltfInstance.Dispose();
            }
            var instance = await VrmUtility.LoadAsync(path);
            gltfInstance = instance;
            gltfInstance.name = Path.GetFileNameWithoutExtension(path);
        }
        
        async internal static void LoadBytesVrm(string path, byte[] bytes)
        {
            if (gltfInstance != null)
            {
                gltfInstance.Dispose();
            }
            var instance = await VrmUtility.LoadBytesAsync(path, bytes);
            gltfInstance = instance;
            gltfInstance.name = Path.GetFileNameWithoutExtension(path);
        }

        internal static void Pack()
        {
            Packer.PackAssets(gltfInstance.Root);
        }
        
        internal static (byte[], string) Export()
        {
            var fileName = gltfInstance.name + "_atlased.vrm";
            var vrm = VRMExporter.Export(new UniGLTF.GltfExportSettings(), gltfInstance.gameObject, new RuntimeTextureSerializer());
            var bytes = vrm.ToGlbBytes();
            return (bytes, fileName);
        }
    }
}
