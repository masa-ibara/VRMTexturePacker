using UnityEngine;
using UnityEngine.UI;

namespace MsaI.Runtime.UI
{
    public class Console : MonoBehaviour
    {
        [SerializeField] Text output;
        double time;
        void Start()
        {
            output.text = "";
        }
        
        public void SetText(string text)
        {
            SetTimer();
            output.text = text;
        }
        
        void Update()
        {
            if (time + 3 < Time.time)
            {
                output.text = "";
            }
        }
        
        void SetTimer()
        {
            time = Time.time;
        }
    }
}
