using ReadyPlayerMe.Core;
using System;
using System.Collections;
using UnityEngine;
using Unity.InferenceEngine;

namespace AiSims
{
    public class Talk : MonoBehaviour
    {
        private AudioSource audioSource;
        private bool isSpeaking = false;

        public event Action OnSpeechFinished;

        public PiperTtsService maleTts;
        public PiperTtsService femaleTts;

        private ConversationManager conversationManager;

        /// <summary>
        /// Start text-to-speech
        /// </summary>
        public void Text2Speech(string text, VoiceHandler voiceHandler, string voice)
        {
            if (isSpeaking)
            {
                Debug.Log("Already speaking, skip");
                return;
            }

            audioSource = voiceHandler.AudioSource;
            StartCoroutine(PlayVoice(text));
        }

        void Awake()
        {
            conversationManager = FindObjectOfType<ConversationManager>();
        }

        private IEnumerator PlayVoice(string text)
        {
            float ttsStart = Time.time;

            if (isSpeaking)
            {
                Debug.Log("Already speaking, skip");
                yield break;
            }

            isSpeaking = true;

            Debug.Log("PLAYVOICE START: " + text);

            var profile = GetComponent<NPCVoiceProfile>();

            PiperTtsService selectedTts;

            if (profile != null && profile.voiceType == NPCVoiceProfile.VoiceType.Male)
            {
                Debug.Log("[TTS] MALE selected");
                selectedTts = maleTts;
            }
            else
            {
                Debug.Log("[TTS] FEMALE selected");
                selectedTts = femaleTts;
            }

            // várjuk hogy ready legyen
            float initTimeout = 5f;
            float initTimer = 0f;

            while (!selectedTts.IsReady && initTimer < initTimeout)
            {
                initTimer += Time.deltaTime;
                yield return null;
            }

            if (!selectedTts.IsReady)
            {
                Debug.LogError("TTS failed to initialize!");
                isSpeaking = false;
                yield break;
            }

            var audio = selectedTts.GetComponent<AudioSource>();

            // kis hang tuning
            audio.pitch = (profile != null && profile.voiceType == NPCVoiceProfile.VoiceType.Male)
                ? 0.9f
                : 1.05f;
            //audio.pitch = 1f;

            selectedTts.Speak(text);

            yield return new WaitForSeconds(0.1f);

            yield return new WaitWhile(() => audio.isPlaying);

            float ttsDuration = Time.time - ttsStart;
            conversationManager.ttsTime = ttsDuration;

            Debug.Log("TTS TIME: " + (Time.time - ttsStart));

            Debug.Log("Speech finished");

            OnSpeechFinished?.Invoke();

            isSpeaking = false;

        }
    }
}