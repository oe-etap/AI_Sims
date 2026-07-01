using UnityEngine;
using System;
using System.IO;
using DotNetEnv;

namespace AiSims
{
    public class MqttBootstrapper : MonoBehaviour
    {
        private async void Start()
        {
            DontDestroyOnLoad(gameObject);

            // --- READING .ENV FILE ---
            // Path.Combine ensures that it looks at the project root in the Editor, 
            // and the folder next to the executable in a compiled Build.
            string envPath = Path.Combine(Application.dataPath, "..", ".env");

            if (File.Exists(envPath))
            {
                try
                {
                    // This call reads the file and automatically sets the variables 
                    // in the background for Environment.GetEnvironmentVariable.
                    Env.Load(envPath);
                    Debug.Log("[MQTT] .env file loaded successfully into the runtime environment.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MQTT] Failed to parse .env file. Syntax error? Details: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[MQTT] No .env file found. Falling back to native OS environment variables or defaults.");
            }
            // ----------------------------

            Debug.Log("[MQTT] Reading configuration from environment variables...");

            // 1. Broker address (Fallback: localhost)
            string brokerAddress = Environment.GetEnvironmentVariable("MQTT_BROKER_ADDRESS") ?? "localhost";

            // 2. Broker port (Fallback: 1883, safe parsing)
            int brokerPort = 1883;
            string portEnv = Environment.GetEnvironmentVariable("MQTT_BROKER_PORT");
            if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, out int parsedPort))
            {
                brokerPort = parsedPort;
            }

            // 3. Client ID (Fallback: Random generated ID to avoid collisions)
            string clientId = Environment.GetEnvironmentVariable("MQTT_CLIENT_ID") ?? $"Unity_Client_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // 4. Authentication flag (Fallback: false, safe parsing)
            bool useCredentials = false;
            string useCredsEnv = Environment.GetEnvironmentVariable("MQTT_USE_CREDENTIALS");
            if (!string.IsNullOrWhiteSpace(useCredsEnv) && bool.TryParse(useCredsEnv, out bool parsedCreds))
            {
                useCredentials = parsedCreds;
            }

            // 5. Credentials (If useCredentials is true but these are null, MqttManager will throw an exception, which is correct)
            string username = Environment.GetEnvironmentVariable("MQTT_USERNAME");
            string password = Environment.GetEnvironmentVariable("MQTT_PASSWORD");

            Debug.Log($"[MQTT] Init -> Target: {brokerAddress}:{brokerPort} | ClientID: {clientId} | Auth: {useCredentials}");

            await MqttManager.Instance.InitializeMqttClientAsync(
                clientId,
                brokerAddress,
                useCredentials,
                username,
                password,
                brokerPort
            );
        }

        private async void OnApplicationQuit()
        {
            Debug.Log("[MQTT] Application shutting down, disconnecting from broker...");
            await MqttManager.Instance.DisconnectAsync();
        }
    }
}