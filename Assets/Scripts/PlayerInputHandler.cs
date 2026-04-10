using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AiSims
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInput playerInput;
        private CharacterController controller;

        private Vector2 moveInput;
        private Vector2 lookInput;

        public float moveSpeed = 5f;
        public float lookSpeed = 100f;
        public float gravity = -9.81f;

        private Vector3 velocity;
        private float pitch = 0f;
        public float cameraHeight = 1.0f;

        public ConversationManager conversationManager;
        public TMP_InputField inputField;
        private bool inputFieldUsed = false;

        private bool allowLook = false;

        // Updated InteractionEntry
        [System.Serializable]
        public class InteractionEntry
        {
            [Header("Interaction Setup")]
            public GameObject targetObject;     // The GameObject to detect (must have collider)
            public UnityEvent onInteract;       // Directly assignable function call
        }

        [Header("Custom Interactions")]
        public InteractionEntry[] interactionEntries;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
            if (inputField != null)
            {
                inputField.onSelect.AddListener(OnSelected);
                inputField.onDeselect.AddListener(OnDeselected);
            }
        }

        public void OnFire()
        {
            if (TryCustomInteraction()) return;

            if (inputField != null && inputFieldUsed) return;

            var stt = conversationManager.speech2Text;

            if (stt == null)
                return;

            if (stt.IsRecording())
            {
                conversationManager.TalkUserFinished();
            }
            else
            {
                conversationManager.OrientateNpcToCameraAndStartTalk();
            }
        }

        private bool TryCustomInteraction()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;

            foreach (var entry in interactionEntries)
            {
                if (entry.targetObject == null) continue;

                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 50f, Color.green, 2f);
                if (Physics.Raycast(ray, out RaycastHit hit, 6f))
                {
                    if (hit.collider.gameObject == entry.targetObject)
                    {
                        entry.onInteract?.Invoke(); // Direct UnityEvent call
                        Debug.Log($"[Interaction] Invoked event on {entry.targetObject.name}");
                        return true;
                    }
                }
            }

            return false;
        }

        void Update()
        {
            if (inputField != null && inputFieldUsed) return;

            Transform camTransform = Camera.main.transform;

            moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            lookInput = playerInput.actions["Look"].ReadValue<Vector2>();

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                allowLook = !allowLook;
            }

            Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = transform.right; right.y = 0f; right.Normalize();

            Vector3 move = forward * moveInput.y + right * moveInput.x;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            if (allowLook)
            {
                transform.Rotate(Vector3.up, lookInput.x * lookSpeed * Time.deltaTime);
                pitch -= lookInput.y * lookSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, -80f, 80f);
                camTransform.rotation = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
            }

            camTransform.position = transform.position + new Vector3(0, cameraHeight, 0);
        }

        void OnSelected(string text) => inputFieldUsed = true;
        void OnDeselected(string text) => inputFieldUsed = false;
    }
}
