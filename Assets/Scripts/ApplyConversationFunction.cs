using AiSims;
using UnityEngine;

public class ApplyConversationFunction : MonoBehaviour
{
    [Header("References")]
    public ConversationManager conversationManager;
    public LLM_Handler targetObject;

    void Start()
    {
        if (conversationManager == null)
        {
            Debug.LogError("ConversationManager is not assigned.", this);
            return;
        }

        if (targetObject == null)
        {
            Debug.LogWarning("No target GameObject assigned. Proceeding anyway.", this);
        }

        conversationManager.SetCurrentNPC(targetObject);
    }
}
