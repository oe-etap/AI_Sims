using System;
using UnityEngine;

namespace AiSims
{
    // Log types extended for your LLM/Voice system
    public enum GameEventType
    {
        DialogueStart = 0,
        DialogueShowButtons = 1, // Inactive in your setup, kept for compatibility
        DialogueEnd = 2,
        ClickedButton = 5,       // Inactive
        ActiveButton = 6,        // Inactive
        SocialScore = 7,
        EnergyLevel = 8,
        QuestStart = 9,
        QuestEnd = 10,
        StressFeatures = 12,
        Emotion = 15,
        CameraPosition = 19,
        LoadScene = 21,
        
        // --- NEW TYPES FOR THE VOICE/LLM SYSTEM ---
        VoiceInputStart = 100,   // When the player starts speaking
        VoiceInputEnd = 101,     // STT finished, text is ready
        LlmProcessing = 102,     // LLM is generating a response
        NpcReply = 103,           // NPC response received

        // --- TTS LOGGING ---
        TtsStart = 104,          // Requesting audio from API
        TtsEnd = 105,            // Audio received successfully
        TtsError = 106,           // API error
    }

    [Serializable]
    public class GameEventLog
    {
        public string dateTime;
        public int Type;
        public string type_as_string;
        public string Log;
        public string timestamp;
        public long timestamp_UTC;

        public GameEventLog(GameEventType type, string logMessage)
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime localNow = DateTime.Now;

            this.dateTime = ((DateTimeOffset)utcNow).ToUnixTimeMilliseconds().ToString();
            this.Type = (int)type;
            this.type_as_string = type.ToString();
            this.Log = logMessage;
            this.timestamp = localNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            
            // Using Ticks for high precision, similar to the provided sample
            this.timestamp_UTC = ((DateTimeOffset)utcNow).Ticks; 
        }
    }
}