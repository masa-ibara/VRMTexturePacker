using UnityEngine;
using UnityEngine.UIElements;

namespace MsaI.TexturePacker
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
                var path = Core.GetFilePath();
                Core.LoadVrm(path[0]);
                Core.Pack();
            };
        }
    }
}

