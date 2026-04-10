using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AiSims
{
    public enum LoggingInfo
    {
        // Dialogue/Interrupt
        DialogueUser,
        DialogueNpc,
        MessageNpc,
        LlmProcessing,
        TTS,
        STT,
        Scene
    }

    // public static class DebugLogger
    public static class Logger
    {
        static string filePath = Application.persistentDataPath;
        static string uniqueId = Guid.NewGuid().ToString();

        static string fullPath = filePath; //  Path.Combine(filePath, uniqueId);
        static bool bShowFilePath = true;

        private static readonly Dictionary<LoggingInfo, string> logMessages = new Dictionary<LoggingInfo, string>
        {
            // Dialogue/Interrupt
            {LoggingInfo.DialogueUser, "DialogueUser" },
            {LoggingInfo.DialogueNpc, "DialogueNpc" },
            {LoggingInfo.MessageNpc, "MessageNpc" },
            {LoggingInfo.LlmProcessing, "LlmProcessing" },
            {LoggingInfo.TTS, "Text2Speech" },
            {LoggingInfo.STT, "Speech2Text" },
            {LoggingInfo.Scene, "Scene" }
        };

        public static string CreateLogEntry(LoggingInfo info, string message, bool useUnixTimestamp = false)
        {
            string timeStamp;

            if (useUnixTimestamp)
            {
                DateTime utcNow = DateTime.UtcNow;
                long unixTimestampMilliseconds = ((DateTimeOffset)utcNow).ToUnixTimeMilliseconds();
                timeStamp = unixTimestampMilliseconds.ToString();
            }
            else
            {
                // Local time in human-readable format
                timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
            }

            var entry = $"[{timeStamp}] [{info}] {message}";
            return entry;
        }

        public static void Log(LoggingInfo info, string message, bool includeInFile = false)
        {
            if (includeInFile)
            {
                if (bShowFilePath)
                {
                    Debug.Log("Filepath: " + fullPath);
                    bShowFilePath = false;
                }

                // Check if directory already esists, if not create it
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                string fileName = Path.Combine(fullPath, $"GameLabLog_{uniqueId}.txt");

                var entry = CreateLogEntry(info, message);
                TextWriter tw = new StreamWriter(fileName, true);
                tw.WriteLine(entry);
                tw.Close();
            }
        }
    }
}
