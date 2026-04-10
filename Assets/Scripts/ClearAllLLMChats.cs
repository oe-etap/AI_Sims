using AiSims;
using LLMUnity;
using UnityEngine;

public class ClearAllLLMChats : MonoBehaviour
{
    [Tooltip("Root object whose children will be searched for LLMCharacter components.")]
    public GameObject rootNode;

    [Tooltip("Message decorator.")]
    public MessageDecorator decorator;

    /// <summary>
    /// Finds all LLMCharacter components under the root node and calls ClearChat() on each.
    /// </summary>
    public void ClearAllChats()
    {
        // Get all LLMCharacter components in the root node and its children
        var llmCharacters = rootNode.GetComponentsInChildren<LLMCharacter>(true);

        if (llmCharacters.Length == 0)
        {
            Debug.Log("No LLMCharacter components found under " + rootNode.name);
            return;
        }

        foreach (var character in llmCharacters)
        {
            if (character != null)
            {
                character.ClearChat();
                Debug.Log($"Cleared chat for: {character.name}");
            }
        }

        if(decorator != null)
            decorator.ClearHistory();
    }
}
