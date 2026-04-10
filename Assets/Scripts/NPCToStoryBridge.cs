using UnityEngine;

namespace AiSims
{
    public class NPCToStoryBridge : MonoBehaviour
    {
        [Header("Link NPC to its LLM Handler")]
        public LLM_Handler llmHandler;

        // Optional: quick getter
        public LLM_Handler GetHandler()
        {
            return llmHandler;
        }

        public void SetHandler(LLM_Handler handler)
        {
            llmHandler = handler;
        }
    }
}