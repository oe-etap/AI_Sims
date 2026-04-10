using UnityEngine;

public class DogPettingDetector : MonoBehaviour
{
    public Transform leftController;        // XR Left Controller Transform
    public float strokeSpeedThreshold = 0.2f;  // Minimum movement to count as stroking
    public float petInterval = 0.2f;           // How often a pet event can fire

    private bool handInTrigger = false;
    private Vector3 lastPosition;
    private float timer;

    void Start()
    {
        lastPosition = leftController.position;
    }

    void Update()
    {
        if (!handInTrigger) return;

        // Calculate velocity of controller
        float movement = Vector3.Distance(leftController.position, lastPosition) / Time.deltaTime;

        timer += Time.deltaTime;

        // If controller is moving while inside the trigger
        if (movement > strokeSpeedThreshold && timer > petInterval)
        {
            OnPetStroke();
            timer = 0f;
        }

        lastPosition = leftController.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            handInTrigger = true;
            lastPosition = leftController.position;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            handInTrigger = false;
        }
    }

    void OnPetStroke()
    {
        // Trigger effects here:
        Debug.Log("Dog is being petted!");

        // Example: play animation
        // dogAnimator.SetTrigger("HappyTail");

        // Example: play sound
        // audioSource.PlayOneShot(pettingSound);

        // Example: haptics feedback
        // leftXRController.SendHapticImpulse(0.3f, 0.1f);
    }
}
