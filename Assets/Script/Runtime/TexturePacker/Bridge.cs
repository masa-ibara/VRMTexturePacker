using System.IO;
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
