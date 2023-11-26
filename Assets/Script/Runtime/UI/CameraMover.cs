using UnityEngine;

namespace MsaI.Runtime.UI
{
    public class CameraMover : MonoBehaviour
    {
        Vector3 beforeMousePostion;
        void Update()
        {
            var currentMousePosition = Input.mousePosition;
            if (Input.GetMouseButton(0))
            {
                var position = (currentMousePosition - beforeMousePostion) / 200;
                position = new Vector3(position.x, position.y * -1, 0);
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
