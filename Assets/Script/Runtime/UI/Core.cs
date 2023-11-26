using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UIElements;
using System.Runtime.InteropServices;

namespace MsaI.Runtime.UI
{
    public class UI : MonoBehaviour
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
        
#if UNITY_WEBGL && !UNITY_EDITOR
        //
        // WebGL
        //
        [DllImport("__Internal")]
        static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

        void LoadVrm() {
            UploadFile("BtnLoadFile", "OnFileUpload", ".vrm", false);
        }

        // Called from browser
        static void OnFileUpload(string url) {
            TexturePacker.Bridge.LoadVrm(url);
            TexturePacker.Bridge.Pack();
        }
        
        [DllImport("__Internal")]
        static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

        // Browser plugin should be called in OnPointerDown.
        void ExportVrm() {
            var result = TexturePacker.Bridge.Export();
            DownloadFile("BtnSaveFile", "OnFileDownload", result.Item2, result.Item1, result.Item1.Length);
        }

        // Called from browser
        static public void OnFileDownload() {
        }
#else
        //
        // Standalone platforms & editor
        //
        
        void LoadVrm()
        {
            var path = GetFilePath();
            TexturePacker.Bridge.LoadVrm(path);
            TexturePacker.Bridge.Pack();
        }
        
        void ExportVrm()
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
#endif
    }
}

