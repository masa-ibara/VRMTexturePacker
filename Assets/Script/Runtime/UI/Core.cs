using SFB;
using UnityEngine;
using UnityEngine.UIElements;

namespace MsaI.Runtime.UI
{
    public class UI : MonoBehaviour
    {
        [SerializeField] UIDocument uiDocument;
        
        void Start()
        {
            var root = uiDocument.rootVisualElement;
                
            var selectVrm = root.Q<Button>("SelectVRM");
            selectVrm.clicked += () =>
            {
                var path = GetFilePath();
                TexturePacker.Bridge.LoadVrm(path);
                TexturePacker.Bridge.Pack();
            };
            
            var exportVrm = root.Q<Button>("ExportVRM");
            exportVrm.clicked += () =>
            {
                var path = GetFolderPath();
                TexturePacker.Bridge.Export(path);
            };
        }
        
        static string GetFilePath()
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

