using UnityEngine;
using System;
using System.Collections;

namespace AiSims
{
    [RequireComponent(typeof(AudioSource))]
    public class Text2Speech : MonoBehaviour
    {
        [Header("Local Piper TTS")]
        [SerializeField] private PiperTtsService tts;

        private AudioSource audioSource;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        /// <summary>
        /// Piper-based local TTS implementation.
        /// The 'voice' parameter is kept for interface compatibility,
        /// but the actual output is determined by the Piper model and JSON config.
        /// </summary>
        public IEnumerator SpeakToClip(string text, string voice, Action<AudioClip> onReady)
        {
            if (tts == null)
            {
                Debug.LogError("[Text2Speech] PiperTtsService reference is missing.");
                Logger.LogToMqtt(GameEventType.TtsError, "TTS Failed: PiperTtsService reference is missing.");
                onReady?.Invoke(null);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                onReady?.Invoke(null);
                yield break;
            }

            // Wait until the local TTS engine is fully loaded into memory
            yield return new WaitUntil(() => tts.InitializationCompleted);

            if (!tts.IsReady)
            {
                Debug.LogError("[Text2Speech] PiperTtsService initialization failed.");
                Logger.LogToMqtt(GameEventType.TtsError, "TTS Failed: PiperTtsService initialization failed.");
                onReady?.Invoke(null);
                yield break;
            }

            // Start measuring synthesis latency
            float startTime = Time.realtimeSinceStartup;

            // LOG: TTS Request started
            Logger.LogToMqtt(GameEventType.TtsStart, $"Requesting local Piper TTS | Text length: {text.Length}");

            // Generate the audio clip locally
            AudioClip clip = tts.SynthesizeToClip(text);

            // Calculate hardware processing delay
            float latency = Time.realtimeSinceStartup - startTime;

            if (clip == null)
            {
                Debug.LogError("[Text2Speech] Piper synthesis returned null.");

                // LOG: TTS failed
                Logger.LogToMqtt(GameEventType.TtsError, $"TTS Failed after {latency:F2}s: Piper synthesis returned null.");

                onReady?.Invoke(null);
                yield break;
            }

            // LOG: TTS successful, log latency and generated clip duration
            Logger.LogToMqtt(GameEventType.TtsEnd, $"TTS Success (Local). Latency: {latency:F2}s | Audio Length: {clip.length:F2}s");

            // Pass the clip back to the caller (e.g., Talk.cs) to handle playback and events
            onReady?.Invoke(clip);
        }

        /// <summary>
        /// Convenience method: generates and plays audio directly on this GameObject.
        /// </summary>
        public void Speak(string text, string voice = null)
        {
            StartCoroutine(SpeakToClip(text, voice, clip =>
            {
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }));
        }
    }
}