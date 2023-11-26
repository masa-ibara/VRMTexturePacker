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
                
            var selectFrontButton = root.Q<Button>("SelectVRM");
            selectFrontButton.clicked += () =>
            {
                var path = TexturePacker.Core.GetFilePath();
                TexturePacker.Core.LoadVrm(path[0]);
                TexturePacker.Core.Pack();
            };
        }
    }
}

