using UnityEngine;

public class SunMovement : MonoBehaviour
{
    // The target GameObject to rotate
    public GameObject targetObject;

    // Rotation speed in degrees per second
    public float rotationSpeed = 0.1f;

    void Update()
    {
        if (targetObject != null)
        {
            // Get the target's transform
            Transform t = targetObject.transform;

            // Increment the X rotation slightly every frame
            t.Rotate(rotationSpeed * Time.deltaTime, 0f, 0f);
        }
    }
}
