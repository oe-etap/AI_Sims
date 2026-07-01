using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AiSims
{
    [System.Serializable]
    public class TextResponse
    {
        public string text;
    }

    public class Speech2Text : MonoBehaviour
    {
        public enum STTMode
        {
            OpenAI_API,
            Local_Client
        }

        [Header("Speech-to-Text Mode")]
        [SerializeField] private STTMode sttMode = STTMode.OpenAI_API;

        [Header("OpenAI API Settings")]
        [SerializeField] private string apiKeyFileName = "openai_api_key.txt";
        [SerializeField] private string sttModel = "gpt-4o-mini-transcribe";

        [Header("Local Whisper Server Settings")]
        [SerializeField] private string localServerUrl = "http://127.0.0.1:8000/transcribe"; // FastAPI endpoint

        private LLM_Handler llm_handler;
        private string micDevice;
        private AudioClip recording;
        private string apiKey;

        public bool isNpcSpeaking = false;
        public bool canRecord = true;
        private bool isRecording = false;

        private ConversationManager conversationManager;

        void Start()
        {
#if UNITY_WEBGL
            if (Microphone.devices.Length > 0)
            {
                micDevice = Microphone.devices[0];
                Debug.Log("Using microphone: " + micDevice);
            }
            else
            {
                Debug.LogError("No microphone found!");
            }
#endif
            if (sttMode == STTMode.OpenAI_API)
            {
                LoadApiKey();
            }
            conversationManager = FindObjectOfType<ConversationManager>();
            conversationManager.BeginMeasurement();
        }

        public void Set_LLM_Handler(LLM_Handler handler)
        {
            llm_handler = handler;
        }

        private void LoadApiKey()
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, apiKeyFileName);
                if (File.Exists(path))
                {
                    apiKey = File.ReadAllText(path).Trim();
                    Debug.Log("✅ OpenAI API key loaded from file.");
                }
                else
                {
                    Debug.LogError("❌ API key file not found at: " + path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Failed to load API key: " + e.Message);
            }
        }

        public void StartRecording()
        {
            if (!canRecord)
            {
                Debug.Log("Recording blocked");
                return;
            }

            if (isRecording)
            {
                Debug.Log("Already recording");
                return;
            }

            Debug.Log("Recording started...");

            recording = Microphone.Start(micDevice, false, 80, 16000);
            isRecording = true;
        }

        public void StopRecording()
        {
            Debug.Log("StopRecording ENTER");

            if (!isRecording)
            {
                Debug.Log("Not recording, ignore stop");
                return;
            }

            if (isNpcSpeaking)
            {
                Debug.Log("IGNORE: NPC is speaking");
                return;
            }

            Microphone.End(micDevice);
            isRecording = false;

            Debug.Log("Recording stopped, sending to Whisper...");

            if (recording == null)
            {
                Debug.LogError("Recording is NULL!");
                return;
            }

            byte[] wavData = WavUtility.FromAudioClip(recording);

            if (wavData == null || wavData.Length == 0)
            {
                Debug.LogError("WAV DATA INVALID!");
                return;
            }

            if (sttMode == STTMode.OpenAI_API)
            {
                Debug.Log("Using OpenAI STT");
                StartCoroutine(SendToOpenAI(wavData));
            }
            else if (sttMode == STTMode.Local_Client)
            {
                Debug.Log("Using LOCAL Whisper");
                StartCoroutine(SendToLocalServer(wavData));
            }
        }

        // --- OpenAI Whisper API ---
        private IEnumerator SendToOpenAI(byte[] wavData)
        {
            WWWForm form = new WWWForm();
            form.AddField("model", sttModel);
            form.AddField("language", "en");
            form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");

            using (UnityWebRequest www = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form))
            {
                www.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Whisper STT Error (OpenAI): " + www.error);
                }
                else
                {

                    string extractedText = ExtractText(www.downloadHandler.text);
                    Debug.Log("Whisper Response (OpenAI): " + extractedText);
                    Logger.Log(LoggingInfo.DialogueUser, extractedText, true);
                    Logger.Log(LoggingInfo.STT, "STT stop", true);
                    Logger.LogToMqtt(GameEventType.VoiceInputEnd, extractedText);
                    llm_handler?.ProcessMessage(ExtractText(www.downloadHandler.text));
                }
            }
        }

        public static string ExtractText(string jsonString)
        {
            // Parse the JSON into the TextResponse class
            TextResponse response = JsonUtility.FromJson<TextResponse>(jsonString);

            // Return the value of "text"
            return response.text.Trim();
        }

        // --- Local Whisper Client ---
        private IEnumerator SendToLocalServer(byte[] wavData)
        {

            float whisperStart = Time.time;

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");

            //File.WriteAllBytes(Application.persistentDataPath + "/debug.wav", wavData);
            //Debug.Log("Saved test WAV at: " + Application.persistentDataPath + "/debug.wav");
            using (UnityWebRequest www = UnityWebRequest.Post(localServerUrl, form))
            {
                yield return www.SendWebRequest();

                Debug.Log("STT TIME: " + (Time.time - whisperStart));
                float whisperDuration = Time.time - whisperStart;
                conversationManager.whisperTime = whisperDuration;

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Whisper STT Error (Local Client): " + www.error);
                }
                else
                {
                    // Filter output from whisper a little bit
                    TextResponse response = JsonUtility.FromJson<TextResponse>(www.downloadHandler.text);
                    //string[] parts = response.text.Split(' ');
                    //if (parts.Length > 1)
                    //    response.text = string.Join(" ", parts, 0, parts.Length - 1);
                    response.text = Regex.Replace(response.text, @"\b(\w+)\s+\1\b", "$1");

                    Debug.Log("Whisper Response (Local Client): " + response.text);
                    Logger.Log(LoggingInfo.DialogueUser, response.text, true);
                    Logger.Log(LoggingInfo.STT, "STT stop", true);

                    llm_handler?.ProcessMessage(response.text);
                }
            }
        }

        public bool IsRecording()
        {
            return isRecording;
        }
    }
}
