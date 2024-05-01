using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using UnityEngine.Networking;

namespace MsaI.Runtime.UI
{
    [RequireComponent(typeof(Button))]
    public class CanvasOpenFile : MonoBehaviour, IPointerDownHandler
    {
        Console Console => FindObjectOfType<Console>();
        ResultSetter[] ResultSetters => FindObjectsByType<ResultSetter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Toggle Toggle => FindObjectOfType<Toggle>();
        
    #if UNITY_WEBGL && !UNITY_EDITOR
        //
        // WebGL
        //
        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

        public void OnPointerDown(PointerEventData eventData) 
        {
            UploadFile(gameObject.name, "OnFileUpload", ".vrm", false);
        }

        // Called from browser
        public void OnFileUpload(string url) 
        {
            StartCoroutine(OutputRoutine(url));
        }
    #else
        //
        // Standalone platforms & editor
        //
        public void OnPointerDown(PointerEventData eventData) { }

        void Start()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            Console.SetText("Loading...");
            var path = Core.GetFilePath();
            var loadVrm = TexturePacker.Bridge.LoadVrm(path);
            if (loadVrm.Result)
            {
                PostProcess();
            }
            else
            {
                Console.SetText("Failed to load VRM");
            }
        }
    #endif

        IEnumerator OutputRoutine(string url)
        {
            Console.SetText("Loading...");
            var request = new UnityWebRequest(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            var loader = request.downloadHandler;
            var loadBytesVrm = TexturePacker.Bridge.LoadBytesVrm(url, loader.data);
            if (loadBytesVrm.Result)
            {
                PostProcess();
            }
            else
            {
                Console.SetText("Failed to load VRM");
            }
        }
        
        void PostProcess()
        {
            TexturePacker.Bridge.Pack(Toggle.isOn);
            // Clear text and result textures
            Console.SetText("");
            foreach (var resultTextureSetter in ResultSetters)
            {
                resultTextureSetter.ClearResult();
            }
            // Set result textures
            var materials = TexturePacker.Bridge.ReadMaterials();
            for (int i = 0; i < Mathf.Min(ResultSetters.Length, 2, materials.Length); i++)
            {
                var text = $"{materials[i].name}\r\n({materials[i].mainTexture.width}x{materials[i].mainTexture.height})";
                ResultSetters[i].SetResult(materials[i].mainTexture, text);
            }
        }
    }
}