using LLMUnity;
using ReadyPlayerMe.Core;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Windows;
using System.Collections;

namespace AiSims
{
    public class LLM_Handler : MonoBehaviour
    {
        public GameObject npc;
        public ConversationManager conversationManager;
        public string voice = "alloy";
        public bool enableEvaluation = false;

        public Talk talk;

        private NpcConnection connection;
        private LLMCharacter llmCharacter;
        private string replyMessage;
        private string userMessage;
        private bool isGenerating = false;

        private Talk currentTalkComp;

        bool addToHistory = false;
        private float llmStartTime;

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
            Debug.Log("REPLY COMPLETED");

            float llmDuration = Time.time - llmStartTime;
            conversationManager.llmTime = llmDuration;
            Debug.Log("LLM TOTAL TIME: " + (Time.time - llmStartTime));
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
                    ""
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

                // Step 8: Pass to conversation manager
                //conversationManager.TalkNpc(sanitized, this, llmCharacter.AIName);

                if (conversationManager != null)
                {
                    conversationManager.TalkNpc(sanitized, this, llmCharacter.AIName);
                }
                else
                {
                    Debug.LogError("ConversationManager is NULL!");
                }

                isGenerating = false;
            }

                //var voiceHandler = npc.GetComponent<VoiceHandler>();
                //currentTalkComp = npc.GetComponent<Talk>();

            //Debug.Log("TALK COMP: " + currentTalkComp);
            //Debug.Log("VOICEHANDLER: " + voiceHandler);
            //Debug.Log("NPC: " + npc);

            //if (currentTalkComp != null && voiceHandler != null)
            //{
            //    Debug.Log("CALLING TALK");
            //    var stt = FindObjectOfType<Speech2Text>();
            //    if (stt != null)
            //    {
            //        stt.isNpcSpeaking = true;
            //        stt.canRecord = false;
            //    }

            //    currentTalkComp.OnSpeechFinished -= OnNpcSpeechFinished;
            //    currentTalkComp.OnSpeechFinished += OnNpcSpeechFinished;

            //    currentTalkComp.Text2Speech(sanitized, voiceHandler, voice);
            //    StartCoroutine(SafetyUnlock());
            //}
            //else
            //{
            //    Debug.LogError("Talk missing on NPC");
            //}
            ////isGenerating = false;

            
            catch (System.Exception ex)
            {
                Debug.LogError($"ReplyCompleted() failed for {llmCharacter?.AIName}: {ex.Message}\n{ex.StackTrace}");
                isGenerating = false;
            }
        }

        void OnNpcSpeechFinished()
        {
            var stt = FindObjectOfType<Speech2Text>();
            if (stt != null)
            {
                stt.isNpcSpeaking = false;
                stt.canRecord = true;
            }

            isGenerating = false;

            if (currentTalkComp != null)
                currentTalkComp.OnSpeechFinished -= OnNpcSpeechFinished;
        }

        public void ProcessMessage(string message, bool addToHist = true)
        {
            llmStartTime = Time.time;
            if (isGenerating)
            {
                Debug.Log("LLM busy, skip input");
                return;
            }
            isGenerating = true;
            if (conversationManager == null)
                return;
            Debug.Log("LLM START | message: " + message);
            if (message == string.Empty || message == " ")
            {
                conversationManager.CancelConversation();
                return;
            }

            Logger.Log(LoggingInfo.LlmProcessing, $"[LlmProcessing] LLM start chat completion", true);
            Debug.Log(message);
            userMessage = message;
            addToHistory = addToHist;
            Debug.Log("LLM CHAT CALLED");
            _ = llmCharacter.Chat(message, HandleReply, ReplyCompleted, addToHistory);
        }

        public void AddMessage(string message, string speaker)
        {
            llmCharacter.AddMessage(speaker, message);
        }

        IEnumerator SafetyUnlock()
        {
            yield return new WaitForSeconds(10f);

            var stt = FindObjectOfType<Speech2Text>();
            if (stt != null)
            {
                stt.canRecord = true;
                stt.isNpcSpeaking = false;
            }

            isGenerating = false;

            Debug.LogWarning("FORCED UNLOCK TRIGGERED");
        }
    }
}
