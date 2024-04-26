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
        TextSetter textSetter => FindObjectOfType<TextSetter>();
        ResultTextureSetter[] resultTextureSetters => FindObjectsByType<ResultTextureSetter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
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
            textSetter.SetText("Loading...");
            var path = Core.GetFilePath();
            var loadVrm = TexturePacker.Bridge.LoadVrm(path);
            if (loadVrm.Result)
            {
                PostProcess();
            }
            else
            {
                textSetter.SetText("Failed to load VRM");
            }
        }
    #endif

        IEnumerator OutputRoutine(string url)
        {
            textSetter.SetText("Loading...");
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
                textSetter.SetText("Failed to load VRM");
            }
        }
        
        void PostProcess()
        {
            TexturePacker.Bridge.Pack();
            textSetter.SetText("");
            var materials = TexturePacker.Bridge.ReadMaterials();
            for (int i = 0; i < Mathf.Min(resultTextureSetters.Length, 2, materials.Length); i++)
            {
                resultTextureSetters[i].SetResult(materials[i].mainTexture, materials[i].name);
            }
        }
    }
}