using UnityEngine;

namespace AiSims
{
    public class NpcMovement_WSound : NpcMovement
    {
        [Header("Audio Settings")]
        public AudioSource audioSource;
        public AudioClip chaseClip;

        private bool isPlayingChaseSound = false;
        private bool isChasing = false;
        private Vector3 initialTargetPosition;

        // Timer for resetting target after x seconds
        private float chaseTimer = 0f;
        public float chaseResetTime = 10f;  // measured in real seconds

        void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (targets.Count > 0)
                initialTargetPosition = targets[0].position;
        }

        public override void StartMovement()
        {
            base.StartMovement();
            StartChasingTarget();
        }

        public override void StartChasingTarget(int speed = 2)
        {
            base.StartChasingTarget(speed);

            isChasing = true;
            chaseTimer = 0f;   // reset timer when chasing starts

            if (audioSource != null && chaseClip != null && !isPlayingChaseSound)
            {
                audioSource.clip = chaseClip;
                audioSource.loop = true;
                audioSource.Play();
                isPlayingChaseSound = true;
            }
        }

        public override void Update()
        {
            CheckUpdate();
            HandleChaseTimer();
            base.Update();
        }

        private void HandleChaseTimer()
        {
            if (!isChasing || targets.Count == 0)
                return;

            // Count up using real seconds
            chaseTimer += Time.deltaTime;

            if (chaseTimer >= chaseResetTime)
            {
                // Reset target position after exactly 20 seconds
                targets[0].position = initialTargetPosition;

                // Reset timer so it only triggers once per cycle
                chaseTimer = 0f;
            }
        }

        private void StopChaseSound()
        {
            if (audioSource != null && isPlayingChaseSound)
            {
                audioSource.Stop();
                isPlayingChaseSound = false;
            }

            isChasing = false;
            chaseTimer = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag(target))
                StopChaseSound();

            base.OnTriggerEnter(other);
        }
    }
}
