using UnityEngine;

public class WorldCanvasFollow : MonoBehaviour
{
    public Camera targetCamera;
    public float distance = 2f;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // Place canvas in front of the camera
        transform.position = targetCamera.transform.position + targetCamera.transform.forward * distance;

        // Make it face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position);
    }
}
