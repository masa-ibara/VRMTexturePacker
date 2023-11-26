using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;

namespace MsaI.Runtime.UI
{
    [RequireComponent(typeof(Button))]
    public class CanvasOpenFile : MonoBehaviour, IPointerDownHandler
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        //
        // WebGL
        //
        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

        public void OnPointerDown(PointerEventData eventData) {
            UploadFile(gameObject.name, "OnFileUpload", ".vrm", false);
        }

        // Called from browser
        public void OnFileUpload(string url) {
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
            Core.LoadVrm();
        }
    #endif

        IEnumerator OutputRoutine(string url)
        {
            var loader = new WWW(url);
            yield return loader;
            TexturePacker.Bridge.LoadVrm(loader.url);
            TexturePacker.Bridge.Pack();
        }
    }
}