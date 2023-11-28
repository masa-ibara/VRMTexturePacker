using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;

namespace MsaI.Runtime.UI
{
    [RequireComponent(typeof(Button))]
    public class CanvasSaveFile : MonoBehaviour, IPointerDownHandler
    {
        TextSetter textSetter => FindObjectOfType<TextSetter>();

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    // Broser plugin should be called in OnPointerDown.
    public void OnPointerDown(PointerEventData eventData) {
        var result = TexturePacker.Bridge.Export();
        DownloadFile(gameObject.name, "OnFileDownload", result.Item2, result.Item1, result.Item1.Length);
    }

    // Called from browser
    public void OnFileDownload() {
        textSetter.SetText("File Successfully Downloaded");
    }
#else
        //
        // Standalone platforms & editor
        //
        public void OnPointerDown(PointerEventData eventData) { }

        // Listen OnClick event in standlone builds
        void Start()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            Core.ExportVrm();
            textSetter.SetText("File Successfully Downloaded");
        }
#endif
    }
}