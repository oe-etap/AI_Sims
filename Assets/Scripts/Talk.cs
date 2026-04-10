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

        public ModelAsset maleModel;
        public ESpeakTokenizer maleTokenizer;

        public ModelAsset femaleModel;
        public ESpeakTokenizer femaleTokenizer;

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
        private IEnumerator PlayVoice(string text)
        {
            if (isSpeaking)
            {
                Debug.Log("Already speaking, skip");
                yield break;
            }

            isSpeaking = true;

            Debug.Log("PLAYVOICE START: " + text);

            var profile = GetComponent<NPCVoiceProfile>();

            var tts = FindObjectOfType<PiperTtsService>();

            if (tts == null)
            {
                Debug.LogError("No PiperTtsService found in scene!");
                isSpeaking = false;
                yield break;
            }

            if (profile != null && profile.voiceType == NPCVoiceProfile.VoiceType.Male)
            {
                Debug.Log("[TTS] MALE selected");
                tts.SetVoice(maleModel, maleTokenizer);
            }
            else
            {
                Debug.Log("[TTS] FEMALE selected");
                tts.SetVoice(femaleModel, femaleTokenizer);
            }

            float initTimeout = 5f;
            float initTimer = 0f;

            while (!tts.IsReady && initTimer < initTimeout)
            {
                initTimer += Time.deltaTime;
                yield return null;
            }

            if (!tts.IsReady)
            {
                Debug.LogError("TTS failed to initialize!");
                isSpeaking = false;
                yield break;
            }

            var audio = tts.GetComponent<AudioSource>();

            if (profile != null && profile.voiceType == NPCVoiceProfile.VoiceType.Male)
            {
                audio.pitch = 0.85f;
            }
            else
            {
                audio.pitch = 1.1f;
            }

            tts.Speak(text);

            float startTimeout = 3f;
            float timer = 0f;

            while (!audio.isPlaying && timer < startTimeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (!audio.isPlaying)
            {
                Debug.LogError("TTS failed to start audio!");
                isSpeaking = false;
                yield break;
            }

            while (audio.isPlaying)
            {
                yield return null;
            }

            Debug.Log("Speech finished");

            OnSpeechFinished?.Invoke();

            isSpeaking = false;
        }
    }
}