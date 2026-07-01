using System;
using System.Collections;
using ReadyPlayerMe.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AiSims
{
    public class ConversationManager : MonoBehaviour
    {
        public GameObject npcThinkingFeedback;
        public GameObject userTalkingFeedback;

        public Complexity questManager;

        public Speech2Text speech2Text;
        public bool UserVoiceEnable = false;
        public bool NpcVoiceEnable = false;
        public LLM_Handler questHandler;
        public LLM_Handler companionNPC;
        public float chanceNpcTalking = 0.35f;

        private LLM_Handler currentNPC;

        private Talk talk;
        private MessageDecorator messageDecorator = null;

        private bool isUserTalking = false;
        private bool isNpcTalking = false;
        private string currentMessage;
        private bool isEvaluating = false;
        private bool addSystemChat = false;
        private bool addChatToCompanion = false;

        private Quaternion originalRotation;

        public float whisperTime;
        public float llmTime;
        public float ttsTime;

        private float totalStartTime;
        private bool measuring = false;

        public void Awake()
        {
            messageDecorator = GetComponent<MessageDecorator>();

            if (questHandler)
            {
                messageDecorator.SetLlmHandler(questHandler);
            }

            Logger.Log(LoggingInfo.Scene, "Start Scene", true);

            if (npcThinkingFeedback)
                npcThinkingFeedback.SetActive(false);
            if (userTalkingFeedback)
                userTalkingFeedback.SetActive(false);
        }

        // This function will be called when the NPC finishes talking
        private void OnNpcSpeechFinished()
        {
            float total = Time.time - totalStartTime;

            Debug.Log(
                $"TOTAL: {total:F2}s | " +
                $"WHISPER: {whisperTime:F2}s | " +
                $"LLM: {llmTime:F2}s | " +
                $"TTS: {ttsTime:F2}s"
            );

            measuring = false;

            NpcConnection npcConnection = currentNPC.GetNpcConnection();
            float chance = UnityEngine.Random.value; // float between 0.0 and 1.0
            isNpcTalking = false;

            if (npcConnection != null)
            {
                Debug.Log("Num conversation partner #" + npcConnection.GetNumNpcs() + " | chance = " + chance);
                LLM_Handler nextNpc = npcConnection.RandomHandler;
                if (nextNpc != null && chance < chanceNpcTalking)
                {
                    Logger.Log(LoggingInfo.LlmProcessing, $"[LlmProcessing] LLM start nextNpc", true);
                    nextNpc.ProcessMessage(currentMessage);
                }
            }
            else
            {
                Debug.Log("NPC 1:1 conversation. | chance = " + chance);
            }


            if (companionNPC.EvaluateConversation() && messageDecorator.EvaluationString != string.Empty)
            {
                isEvaluating = true;
                Logger.Log(LoggingInfo.LlmProcessing, $"[LlmProcessing] LLM start EvaluateConversation", true);
                messageDecorator.EvaluateConversation();
            }

            isUserTalking = false;

            var stt = FindObjectOfType<Speech2Text>();
            if (stt != null)
            {
                stt.canRecord = true;
                stt.isNpcSpeaking = false;
                Debug.Log("Recording unlocked");
            }
        }

        public void CancelConversation()
        {
            isUserTalking = false;
            isNpcTalking = false;
            npcThinkingFeedback.SetActive(false);
            userTalkingFeedback.SetActive(false);

            string npcName = currentNPC != null ? currentNPC.GetLlm().AIName : "Unknown";
            Logger.LogToMqtt(GameEventType.DialogueEnd, $"{npcName} Talking - End");
        }

        public void SetCurrentNPC(NPCToStoryBridge npc)
        {
            currentNPC = npc.llmHandler;
            Logger.Log(LoggingInfo.Scene, $"[NPC] {currentNPC.name}", true);
            messageDecorator.SetEvaluationInstruction(companionNPC.EvaluationString);
        }

        public void SetCurrentNPC(LLM_Handler npc)
        {
            currentNPC = npc;
            if (companionNPC)
                messageDecorator.SetEvaluationInstruction(companionNPC.EvaluationString);
        }

        public void StartTalkUserTalkingMessage()
        {
            Logger.Log(LoggingInfo.DialogueUser, $"[User] User talk start", true);
        }

        public void StopTalkUserTalkingMessage()
        {
            Logger.Log(LoggingInfo.DialogueUser, $"[User] User talk stop", true);
        }

        public void TalkUser()
        {
            if (isUserTalking) return;

            Debug.Log("CURRENT NPC: " + currentNPC);

            if (userTalkingFeedback)
            {
                userTalkingFeedback.SetActive(true);
                PositionMarkerRightOfNPC(currentNPC.npc.transform, userTalkingFeedback.transform);
            }

            if (UserVoiceEnable && currentNPC != null)
            {
                isUserTalking = true;
                speech2Text.Set_LLM_Handler(currentNPC);
                speech2Text.StartRecording();
            }
        }

        public void TalkUserFinished()
        {
            if (!isUserTalking) return;

            if (userTalkingFeedback)
                userTalkingFeedback.SetActive(false);

            if (UserVoiceEnable && currentNPC != null)
            {
                isNpcTalking = true;

                if (npcThinkingFeedback)
                {
                    npcThinkingFeedback.SetActive(true);
                    PositionMarkerRightOfNPC(currentNPC.npc.transform, npcThinkingFeedback.transform);
                }

                Logger.Log(LoggingInfo.DialogueUser, "[User] User talk stop", true);
                speech2Text.StopRecording();
            }
        }

        public void ProcessMessage(string message)
        {
            totalStartTime = Time.time;
            Debug.Log("=== TOTAL START === " + totalStartTime);

            if (message == string.Empty) return;

            if (userTalkingFeedback)
                userTalkingFeedback.SetActive(false);

            if (currentNPC != null)
            {
                if (npcThinkingFeedback)
                {
                    npcThinkingFeedback.SetActive(true);
                    PositionMarkerRightOfNPC(currentNPC.npc.transform, npcThinkingFeedback.transform);
                }

                Logger.Log(LoggingInfo.DialogueUser, message, true);
                Logger.Log(LoggingInfo.DialogueUser, $"[User] User talk stop", true);
                currentNPC.ProcessMessage(message);
            }
        }

        public void TalkNpc(string replyMessage, LLM_Handler npc, string aiName)
        {
            if (!isEvaluating)
                Logger.Log(LoggingInfo.MessageNpc, replyMessage, true);
            else
            {
                if (int.TryParse(replyMessage, out int number))
                {
                    QuestEvaluation(number);
                }
            }
            Logger.Log(LoggingInfo.LlmProcessing, $"[LlmProcessing] LLM stop chat completion", true);

            StartCoroutine(TalkNpcCoroutine(replyMessage, npc, aiName));
        }

        public void QuestEvaluation(int eval)
        {
            Logger.Log(LoggingInfo.MessageNpc, "Evaluation: " + eval.ToString(), true);
            if (eval == 1 && questManager != null) // Evaluated by LLM
            {
                questManager.SetCurrentQuestSuccessful();
            }
        }

        public void AddMessage(LLM_Handler handler, string message, MessageTypes type)
        {
            if (handler == null || handler.GetLlm() == null)
            {
                Debug.Log("LLM Handler for evaluating quests is not assigned.");
                return;
            }

            // Convert enum to string role
            string role = type.ToString(); // "system", "assistant", "user"

            // Call LLM with role and message
            handler.GetLlm().AddMessage(role, role + ": " + message);
        }

        private IEnumerator TalkNpcCoroutine(string replyMessage, LLM_Handler npcHandler, string aiName)
        {
            if (companionNPC.EvaluateConversation() && !isEvaluating)
            {
                //if (questManager.GetQuestCharacter() == currentNPC)
                {
                    if (addChatToCompanion)
                        messageDecorator.AddChatToHistory(MessageTypes.assistant.ToString() + ": " + replyMessage);
                    messageDecorator.AddChatToHistory(MessageTypes.user.ToString() + ": " + currentNPC.GetUserMessage());
                }
            }

            if (addSystemChat && companionNPC && currentNPC != companionNPC)
            {
                AddMessage(companionNPC, replyMessage, MessageTypes.assistant);
                AddMessage(companionNPC, currentNPC.GetUserMessage(), MessageTypes.user);
            }

            // Add message for other NPCs
            NpcConnection otherNpc = currentNPC.GetNpcConnection();
            if (otherNpc != null)
            {
                var allHandler = otherNpc.GetAllHandler();
                foreach (var handler in allHandler)
                {
                    handler.AddMessage(currentNPC.GetUserMessage(), MessageTypes.user.ToString()); // MessageTypes.user.ToString() + ": " + currentNPC.GetUserMessage(), MessageTypes.user.ToString()
                    handler.AddMessage(replyMessage, currentNPC.GetLlm().AIName); // aiName + ": " + replyMessage
                }
            }

            currentMessage = replyMessage;

            var talkComp = npcHandler.npc.GetComponent<Talk>();

            if (NpcVoiceEnable && talkComp != null && npcHandler.npc != null)
            {
                if (npcThinkingFeedback)
                    npcThinkingFeedback.SetActive(false);

                var voiceHandler = npcHandler.npc.GetComponent<VoiceHandler>();

                //talkComp.Text2Speech(currentMessage, voiceHandler, npcHandler.GetVoiceName());
                if (talkComp != null)
                {
                    talkComp.OnSpeechFinished -= OnNpcSpeechFinished;
                    talkComp.OnSpeechFinished += OnNpcSpeechFinished;

                    talkComp.Text2Speech(currentMessage, voiceHandler, npcHandler.GetVoiceName());
                }
                else
                {
                    Debug.LogError("Talk component missing on NPC!");
                }
            }
            else if (!isEvaluating)
            {
                if (npcThinkingFeedback)
                    npcThinkingFeedback.SetActive(false);

                if (messageDecorator != null)
                {
                    messageDecorator.ProcessMessage(replyMessage, aiName);
                }

                // Estimate reading time: characters * factor
                float readingSpeed = 0.0001f; // seconds per character
                float waitTime = Mathf.Max(1.5f, replyMessage.Length * readingSpeed);
                yield return new WaitForSeconds(waitTime);

                OnNpcSpeechFinished();
            }
            else if (isEvaluating)
            {
                isEvaluating = false;
            }
        }

        public void OrientateNpcToCameraAndStartTalk()
        {
            if (Camera.main == null) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                //Debug.Log("Pointer over UI - not raycasting NPCs.");
                return;
            }

            // Use mouse position instead of always forward
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 50f, Color.green, 2f);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 6f))
            {
                NPCToStoryBridge npcBridge = hit.collider.GetComponentInParent<NPCToStoryBridge>();
                Debug.Log("NPC Bridge: " + npcBridge);
                if (npcBridge != null && npcBridge.isActiveAndEnabled)
                {
                    Debug.Log("Hit NPC: " + hit.collider.name);

                    // Make NPC look at player horizontally
                    Vector3 lookTarget = Camera.main.transform.position;
                    lookTarget.y = hit.collider.transform.position.y;
                    hit.collider.transform.LookAt(lookTarget);
                    SetCurrentNPC(npcBridge);
                    // LOG: Start of the conversation with the targeted NPC
                    Logger.LogToMqtt(GameEventType.DialogueStart, $"{npcBridge.llmHandler.GetLlm().AIName} Talking - Start");
                    StartTalkUserTalkingMessage();
                    TalkUser();
                }
            }
        }

        public void OrientateNpcToCameraAndStartTalkNoRayCast(GameObject selectedObject)
        {
            if (selectedObject == null)
            {
                Debug.LogWarning("No GameObject provided to OrientateNpcToCameraAndStartTalkNoRayCast.");
                return;
            }

            // Get the NPCToStoryBridge component from the object
            NPCToStoryBridge npcBridge = selectedObject.GetComponent<NPCToStoryBridge>();
            if (npcBridge == null)
            {
                Debug.LogWarning("The selected GameObject does not have an NPCToStoryBridge component.");
                return;
            }

            StartTalkUserTalkingMessage();
            SetCurrentNPC(npcBridge);
            TalkUser();

            //Debug.Log("Selected NPC: " + npcBridge.name);
            originalRotation = npcBridge.transform.rotation;

            // Make NPC look at the player horizontally
            if (Camera.main != null)
            {
                Vector3 lookTarget = Camera.main.transform.position;
                lookTarget.y = npcBridge.transform.position.y;
                npcBridge.transform.LookAt(lookTarget);
            }
        }

        public void RestoreNpcOrientation(GameObject selectedObject)
        {
            if (selectedObject == null)
            {
                Debug.LogWarning("No NPC stored to restore orientation.");
                return;
            }

            selectedObject.transform.rotation = originalRotation;
            Debug.Log("Restored orientation for NPC: " + selectedObject.name);
        }

        public bool Talking()
        {
            return isUserTalking;
        }

        /// <summary>
        /// Positions the target transform (e.g., npcThinking) one meter to the right
        /// of the specified NPC, based on the NPC's current world orientation.
        /// Keeps the object independent (not parented) and updates immediately.
        /// </summary>
        /// <param name="npc">The NPC whose position and rotation define the placement.</param>
        /// <param name="marker">The independent transform to position (e.g., npcThinking).</param>
        /// <param name="horizontalDistance">Horizontal offset in meters to the right of the NPC (default 0.3).</param>
        /// <param name="verticalOffset">Vertical offset in meters above the NPC (default 0.1).</param>
        /// <param name="faceCamera">If true, rotates the marker to face the main camera instantly.</param>
        public void PositionMarkerRightOfNPC(Transform npc, Transform marker,
            float horizontalDistance = 0.4f, float verticalOffset = 1.7f, bool faceCamera = true)
        {
            if (npc == null || marker == null)
                return;

            // Calculate right-hand direction based on NPC orientation
            Vector3 rightDir = npc.right;

            // Compute target position: right side + vertical offset
            Vector3 targetPos = npc.position + rightDir * horizontalDistance;
            targetPos.y = npc.position.y + verticalOffset;

            // Instantly set position (no interpolation)
            marker.position = targetPos;

            // Update rotation instantly
            if (faceCamera && Camera.main != null)
            {
                // Make the marker face the camera directly
                marker.LookAt(Camera.main.transform);

                // Optionally, keep upright (prevent tilting)
                Vector3 euler = marker.eulerAngles;
                euler.x = 0f;
                euler.z = 0f;
                marker.eulerAngles = euler;
            }
            else
            {
                // Align rotation with the NPC
                marker.rotation = npc.rotation;
            }
        }

        public void BeginMeasurement()
        {
            whisperTime = 0f;
            llmTime = 0f;
            ttsTime = 0f;

            totalStartTime = Time.time;
            measuring = true;

            Debug.Log("=== NEW MEASUREMENT ===");
        }
    }
}