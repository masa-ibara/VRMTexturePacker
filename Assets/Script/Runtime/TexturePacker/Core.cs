using SFB;
using UniGLTF;
using UnityEngine;
using VRM;

namespace MsaI.Runtime.TexturePacker
{
    public static class Core
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
        }
        
        internal static string[] GetFilePath()
        {
            var extensions = new [] {
                new ExtensionFilter("VRM Files", "vrm"),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);
            return paths;
        }

        internal static void Pack()
        {
            Packer.PackAssets(gltfInstance.Root);
        }
    }
}
