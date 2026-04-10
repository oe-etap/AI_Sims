using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace AiSims
{
    public enum MessageTypes
    {
        system = 0,
        assistant = 1,
        user = 2
    }

    public class MessageDecorator : MonoBehaviour
    {
        public TMP_Text text;
        private bool processMessage = false;
        private LLM_Handler llmHandler;

        private string chatHistory = string.Empty;

        public bool ProcessedEvaluation { get; private set; } = false;

        public string EvaluationString { get; private set; } = string.Empty;

        public void ProcessMessage(string message, string aiName)
        {
            if (processMessage)
            {
                message = Regex.Match(message, @"^\d").Value; // match first digit
                Console.WriteLine(message);  // Output: 0
            }
            text.text = aiName + ": " + message;
        }

        public void SetLlmHandler(LLM_Handler handler)
        {
            this.llmHandler = handler;
        }

        public LLM_Handler GetLlmHandler()
        {
            return llmHandler;
        }

        public void SetEvaluationInstruction(string instruction)
        {
            EvaluationString = instruction;
        }

        public void AddChatToHistory(string message)
        {
            chatHistory += message + "\n\n";
        }

        public void ClearHistory()
        {
            chatHistory = string.Empty;
        }

        public void EvaluateConversation()
        {
            if (llmHandler == null || llmHandler.GetLlm() == null || EvaluationString == string.Empty)
            {
                //Debug.Log("LLM Handler for evaluating quests is not assigned.");
                return;
            }

            llmHandler.GetLlm().ClearChat();

            //string message = llmHandler.GetLlm().prompt;
            string instruction = "\nBased on the chat history evaluate the question: \n " + EvaluationString + " \nAfter evaluation respond with exactly one character: 1 if Yes, 0 if No. If the outcome is unclear, respond with 0.";
            llmHandler.GetLlm().SetPrompt(instruction);

            // Now pass it to your function
            llmHandler.ProcessMessage(chatHistory, false);
            ProcessedEvaluation = true;
        }
    }
}
