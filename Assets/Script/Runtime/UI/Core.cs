using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UIElements;

namespace MsaI.Runtime.UI
{
    public class Core : MonoBehaviour
    {
        [SerializeField] UIDocument uiDocument;
        
        void Start()
        {
            var root = uiDocument.rootVisualElement;
                
            var selectVrm = root.Q<Button>("SelectVRM");
            selectVrm.clicked += LoadVrm;
            
            var exportVrm = root.Q<Button>("ExportVRM");
            exportVrm.clicked += ExportVrm;
        }

        static void LoadVrm()
        {
            var path = Core.GetFilePath();
            var loadVrm = TexturePacker.Bridge.LoadVrm(path);
            if (loadVrm.Result)
            {
                TexturePacker.Bridge.Pack();
            }
        }
        
        internal static void ExportVrm()
        {
            var path = GetFolderPath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            var result = TexturePacker.Bridge.Export();
            var dest = Path.Combine(path, result.Item2);
            File.WriteAllBytes(dest, result.Item1);
        }

        internal static string GetFilePath()
        {
            var extensions = new [] {
                new ExtensionFilter("VRM Files", "vrm"),
            };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
            return paths[0];
        }
        
        static string GetFolderPath()
        {
            var paths = StandaloneFileBrowser.OpenFolderPanel("Save File", "", false);
            return paths[0];
        }
    }
}

