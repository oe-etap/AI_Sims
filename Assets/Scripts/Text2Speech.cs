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
        /// Piper-alapú lokális TTS.
        /// A voice paraméter kompatibilitás miatt megmaradt,
        /// de a tényleges hangot a Piper modell + json konfiguráció adja.
        /// </summary>
        public IEnumerator SpeakToClip(string text, string voice, Action<AudioClip> onReady)
        {
            Debug.Log("TTS INIT DONE? " + tts.InitializationCompleted);
            Debug.Log("TTS READY? " + tts.IsReady);
            if (tts == null)
            {
                Debug.LogError("[Text2Speech] PiperTtsService reference is missing.");
                onReady?.Invoke(null);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                onReady?.Invoke(null);
                yield break;
            }

            yield return new WaitUntil(() => tts.InitializationCompleted);

            if (!tts.IsReady)
            {
                Debug.LogError("[Text2Speech] PiperTtsService initialization failed.");
                onReady?.Invoke(null);
                yield break;
            }

            //AudioClip clip = tts.SynthesizeToClip(text);
            tts.Speak(text);


            //if (clip == null)
            //{
            //    Debug.LogError("[Text2Speech] Piper synthesis returned null.");
            //    onReady?.Invoke(null);
                yield break;
            //}

            //onReady?.Invoke(clip);
        }

        /// <summary>
        /// Kényelmi metódus: generál + lejátszik ezen a GameObjecten.
        /// </summary>
        public void Speak(string text, string voice = null)
        {
            StartCoroutine(SpeakToClip(text, voice, clip =>
            {
                if (clip == null)
                    return;

                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.Play();
            }));
        }
    }
}