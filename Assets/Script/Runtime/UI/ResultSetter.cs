using UnityEngine;
using UnityEngine.UI;

namespace MsaI.Runtime.UI
{
    public class ResultSetter : MonoBehaviour
    {
        [SerializeField] RawImage outputImage;
        [SerializeField] Text outputText;
        
        void Start()
        {
            outputImage.texture = null;
        }
        
        public void SetResult(Texture texture, string text)
        {
            outputImage.texture = texture;
            outputText.text = text;
        }
        
        public void ClearResult()
        {
            outputImage.texture = null;
            outputText.text = "";
        }
    }
}
