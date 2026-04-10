using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class SetQuest : MonoBehaviour
{
    [Header("Game Complexity")]
    public Complexity complexity;

    public int currentEntry = 0;
    public int currentQuest = 0;

    void Start()
    {
        if (complexity)
        {
            complexity.SetLlmQuestByIndex(currentEntry, currentQuest);
        }
    }
}
