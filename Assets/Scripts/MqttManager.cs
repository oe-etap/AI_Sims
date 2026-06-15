using MQTTnet.Client;
using MQTTnet;
using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Text;
using System.Collections.Concurrent;

namespace AiSims
{
    /// <summary>
    /// Thread-safe Singleton manager for MQTT connections, subscriptions, and message routing.
    /// </summary>  
    internal class MqttManager
    {
        // Lazy, thread-safe Singleton instantiation
        private static readonly Lazy<MqttManager> _instance = new Lazy<MqttManager>(() => new MqttManager());
        public static MqttManager Instance => _instance.Value;

        private readonly IMqttClient _mqttClient;

        // 0 = false, 1 = true. Protects initialization from parallel calls.
        private int _isInitializing = 0;

        // Thread-safe dictionary to route incoming messages to the appropriate handler (Topic -> Handler func)
        private readonly ConcurrentDictionary<string, Func<string, Task>> _topicHandlers = new ConcurrentDictionary<string, Func<string, Task>>();

        // Private constructor
        private MqttManager()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // Bind basic network events
            _mqttClient.ConnectedAsync += async e =>
            {
                Debug.Log("[MQTT] Successfully connected to the broker.");
                await Task.CompletedTask;
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                Debug.Log("[MQTT] Disconnected from the broker.");
                await Task.CompletedTask;
            };

            // CENTRAL MESSAGE ROUTING: Bind only once during the lifetime!
            _mqttClient.ApplicationMessageReceivedAsync += RouteIncomingMessageAsync;
        }

        public bool IsConnected => _mqttClient.IsConnected;

        /// <summary>
        /// Routing incoming messages and invoking the registered handlers.
        /// </summary>
        private async Task RouteIncomingMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;

            // If we have a registered handler for the topic, invoke it
            if (_topicHandlers.TryGetValue(topic, out var handler))
            {
                try
                {
                    string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    await handler(payload);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MQTT] Error executing handler for topic {topic}: {ex.Message}");
                }
            }
        }

        public async Task InitializeMqttClientAsync(
            string clientId,
            string brokerAddress,
            bool withCredentials = false,
            string username = null,
            string password = null,
            int brokerPort = 1883,
            CancellationToken cancellationToken = default)
        {
            if (_mqttClient.IsConnected)
            {
                Debug.LogWarning("[MQTT] The client is already connected. Initialization aborted.");
                return;
            }

            // Atomic check: Prevents parallel init calls (Reconnection Storm protection)
            if (Interlocked.CompareExchange(ref _isInitializing, 1, 0) == 1)
            {
                Debug.LogWarning("[MQTT] Initialization is already in progress. Duplicate call dropped.");
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client ID cannot be empty.", nameof(clientId));
                if (string.IsNullOrWhiteSpace(brokerAddress)) throw new ArgumentException("Broker address cannot be empty.", nameof(brokerAddress));
                if (withCredentials && string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required for authentication.", nameof(username));
                if (withCredentials && string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required for authentication.", nameof(password));

                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithTcpServer(brokerAddress, brokerPort)
                    .WithCleanSession()         // MQTT < 5.0.0
                    .WithCleanStart();          // MQTT >= 5.0.0

                if (withCredentials)
                {
                    optionsBuilder.WithCredentials(username, password);
                }

                await _mqttClient.ConnectAsync(optionsBuilder.Build(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MQTT] Connection error: {ex.Message}");
            }
            finally
            {
                // Release the flag, regardless of success or error
                Interlocked.Exchange(ref _isInitializing, 0);
            }
        }

        public async Task SubscribeAsync(
            string topic,
            Func<string, Task> handler,
            MQTTnet.Protocol.MqttQualityOfServiceLevel qualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic cannot be empty.", nameof(topic));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // Store or update the handler in the Dictionary (if the topic already existed, overwrite it for safety)
            _topicHandlers.AddOrUpdate(topic, handler, (key, oldValue) => handler);

            await _mqttClient.SubscribeAsync(topic, qualityOfServiceLevel, cancellationToken).ConfigureAwait(false);
            Debug.Log($"[MQTT] Subscribed to topic: {topic}");
        }

        public async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic cannot be empty.", nameof(topic));

            // Remove the handler from memory
            _topicHandlers.TryRemove(topic, out _);

            await _mqttClient.UnsubscribeAsync(topic, cancellationToken).ConfigureAwait(false);
            Debug.Log($"[MQTT] Unsubscribed from topic: {topic}");
        }

        public async Task PublishAsync(
            string topic,
            string payload,
            MQTTnet.Protocol.MqttQualityOfServiceLevel qualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
            bool retain = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic cannot be empty.", nameof(topic));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            if (!_mqttClient.IsConnected)
            {
                Debug.LogWarning("[MQTT] Cannot publish because the client is not connected to the broker.");
                return;
            }

            try
            {
                await _mqttClient.PublishStringAsync(topic, payload, qualityOfServiceLevel, retain, cancellationToken).ConfigureAwait(false);
                Debug.Log($"[MQTT] Message published to: {topic}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MQTT] Error publishing message to: {topic}. Details: {ex.Message}");
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (_mqttClient.IsConnected)
            {
                try
                {
                    var disconnectOptions = new MqttClientDisconnectOptions
                    {
                        Reason = MqttClientDisconnectOptionsReason.NormalDisconnection
                    };

                    await _mqttClient.DisconnectAsync(disconnectOptions, cancellationToken).ConfigureAwait(false);

                    // Security cleanup: Clear handlers on disconnect
                    _topicHandlers.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MQTT] Error during disconnection: {ex.Message}");
                }
            }
        }
    }
}