using UnityEngine;

namespace MsaI.Runtime.UI
{
    public class CameraMover : MonoBehaviour
    {
        [SerializeField] Transform mainCamera;
        Vector3 beforeMousePostion;
        void Update()
        {
            var currentMousePosition = Input.mousePosition;
            var scroll = Input.mouseScrollDelta.y * 0.1f;
            mainCamera.position += mainCamera.forward * scroll;
            if (mainCamera.localPosition.z < 0)
            {
                mainCamera.localPosition = Vector3.zero;
            }
            if (Input.GetMouseButton(0))
            {
                var position = (currentMousePosition - beforeMousePostion) * (mainCamera.localPosition.z + 0.01f) / 200;
                position = mainCamera.right * -position.x + mainCamera.up * -position.y;
                transform.position += position;
            }
            if (Input.GetMouseButton(1))
            {
                var eular = (currentMousePosition - beforeMousePostion) / 10;
                eular = new Vector3(eular.y, eular.x, 0);
                transform.Rotate(eular);
            }
            beforeMousePostion = currentMousePosition;
        }
    }
}
