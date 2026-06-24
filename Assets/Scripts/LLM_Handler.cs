using LLMUnity;
using ReadyPlayerMe.Core;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Windows;

namespace AiSims
{
    public class LLM_Handler : MonoBehaviour
    {
        public GameObject npc;
        public ConversationManager conversationManager;
        public string voice = "alloy";
        public bool enableEvaluation = false;

        private NpcConnection connection;
        private LLMCharacter llmCharacter;
        private string replyMessage;
        private string userMessage;

        bool addToHistory = false;

        [TextArea(5, 10), Chat] public string EvaluationString = string.Empty;        

        private void Start()
        {
            llmCharacter = GetComponent<LLMCharacter>();
            connection = GetComponent<NpcConnection>();
        }

        public bool EvaluateConversation()
        {
            return enableEvaluation;
        }

        public string GetVoiceName()
        {
            return voice;
        }

        public string GetUserMessage()
        {
            return userMessage;
        }

        public NpcConnection GetNpcConnection()
        {
            return connection;
        }

        public LLMCharacter GetLlm()
        {
            return llmCharacter;
        }

        void HandleReply(string reply)
        {
            replyMessage = reply;
        }

        void ReplyCompleted()
        {
            try
            {
                // Step 1: Null / empty guard
                if (string.IsNullOrEmpty(replyMessage))
                {
                    Debug.LogWarning($"{llmCharacter.AIName}: Reply message was empty or null.");
                    return;
                }

                // Step 2: Log original message safely
                string safeOriginal = replyMessage.Replace("\n", "\\n").Replace("\r", "");
                Debug.Log($"{llmCharacter.AIName}: {safeOriginal}");

                // Step 3: Remove parentheses and their contents
                string noBrackets = Regex.Replace(replyMessage, @"\([^)]*\)", "");

                // Step 4: Remove unsafe characters (KEEP { } " ' )
                string sanitized = Regex.Replace(
                    noBrackets,
                    @"[^a-zA-Z0-9äöüÄÖÜß\s\?\.\!\-,'"":\{\}]",
                    " "
                );

                // Step 5: Trim and normalize whitespace
                sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

                // Step 6: Prevent very long strings (just in case)
                if (sanitized.Length > 2000)
                {
                    sanitized = sanitized.Substring(0, 2000);
                    Debug.LogWarning($"{llmCharacter.AIName}: Message truncated to 2000 chars.");
                }

                // Step 7: Final safety cleanup for logs / APIs
                sanitized = sanitized.Replace("\"", "'"); // Replace double quotes to avoid JSON errors
                sanitized = sanitized.Replace("\\", "");  // Remove backslashes if any remain

                Debug.Log($"{llmCharacter.AIName}: {sanitized}");
                Logger.LogToMqtt(GameEventType.NpcReply, sanitized);

                // Step 8: Pass to conversation manager
                conversationManager.TalkNpc(sanitized, this, llmCharacter.AIName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ReplyCompleted() failed for {llmCharacter?.AIName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void ProcessMessage(string message, bool addToHist = true)
        {
            if (conversationManager == null)
                return;

            if(message == string.Empty || message == " ") 
            {
                conversationManager.CancelConversation();
                return;
            }

            Logger.Log(LoggingInfo.LlmProcessing, $"[LlmProcessing] LLM start chat completion", true);
            Debug.Log(message);
            userMessage = message;
            addToHistory = addToHist;
            _ = llmCharacter.Chat(message, HandleReply, ReplyCompleted, addToHistory);
        }

        public void AddMessage(string message, string speaker)
        {
            llmCharacter.AddMessage(speaker, message);
        }
    }
}
