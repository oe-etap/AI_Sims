using System.Collections.Generic;
using UnityEngine;

namespace AiSims
{
    public class NpcConnection : MonoBehaviour
    {
        [Header("Link NPC to one or more LLM Handlers")]
        public List<LLM_Handler> llmHandlers = new List<LLM_Handler>();

        /// <summary>
        /// Returns a random LLM_Handler from the list, or null if empty.
        /// </summary>
        public LLM_Handler RandomHandler
        {
            get
            {
                if (llmHandlers == null || llmHandlers.Count == 0)
                {
                    Debug.LogWarning("NpcConnection: No LLM_Handler assigned!");
                    return null;
                }

                int index = Random.Range(0, llmHandlers.Count);
                return llmHandlers[index];
            }
        }

        public int GetNumNpcs()
        {
            return llmHandlers.Count;
        }

        public List<LLM_Handler> GetAllHandler() 
        {
            return llmHandlers;
        }
    }
}
