using AiSims;
using LLMUnity;
using System;
using System.Collections.Generic;
using UnityEngine;

// Difficulty levels for LLM Characters
public enum DifficultyLevel
{
    Easy,
    Challenging
}

[System.Serializable]
public class NpcEntry
{
    [Header("NPC Reference")]
    public GameObject npc;
    public float delaySeconds = 2f;
    public string startAnimation;
    public bool isActive = false;
}

[System.Serializable]
public class LlmQuestEntry
{
    [Header("LLM Character Reference")]
    public LLMCharacter llmCharacter;
    public LLM_Handler llmHandler;

    [Header("Prompts for this Quest")]
    [TextArea(5, 10), Chat] public List<string> promptTexts = new List<string>();
    [TextArea(5, 10), Chat] public List<string> quests = new List<string>();

    public List<LLM_Handler> questCharacter;

    public float delaySeconds = 0f;
    public bool isActive = false;

    [Header("Quest Success Objects")]
    public List<GameObject> questSuccessfull = new List<GameObject>();
}

[System.Serializable]
public class LlmEntry
{
    [Header("LLM Character Reference")]
    public LLMCharacter llmCharacter;

    [TextArea(5, 10), Chat] public string promptText;
    [TextArea(5, 10), Chat] public string promptTextChallenging;

    public float delaySeconds = 0f;
    public bool isActive = false;
    public DifficultyLevel difficulty = DifficultyLevel.Easy;
}

public class Complexity : MonoBehaviour
{
    [Header("NPC Entries")]
    public List<NpcEntry> npcEntries = new List<NpcEntry>();

    [Header("LLM Character Quest Entries")]
    public List<LlmQuestEntry> llmQuestEntries = new List<LlmQuestEntry>();

    [Header("LLM Character Complexity Entries")]
    public List<LlmEntry> llmEntries = new List<LlmEntry>();

    [Header("Global Difficulty Settings")]
    public bool useGlobalDifficulty = false;
    public DifficultyLevel globalDifficulty = DifficultyLevel.Easy;

    private int currentEntry = -1;
    private int currentQuest = -1;

    // Full transform storage: position, rotation, scale
    private List<List<Vector3>> originalPositions = new List<List<Vector3>>();
    private List<List<Quaternion>> originalRotations = new List<List<Quaternion>>();
    private List<List<Vector3>> originalScales = new List<List<Vector3>>();


    void Start()
    {
        // Save FULL TRANSFORM for each questSuccessfull object
        originalPositions.Clear();
        originalRotations.Clear();
        originalScales.Clear();

        foreach (var entry in llmQuestEntries)
        {
            List<Vector3> posList = new List<Vector3>();
            List<Quaternion> rotList = new List<Quaternion>();
            List<Vector3> scaleList = new List<Vector3>();

            foreach (var obj in entry.questSuccessfull)
            {
                if (obj != null)
                {
                    posList.Add(obj.transform.position);
                    rotList.Add(obj.transform.rotation);
                    scaleList.Add(obj.transform.localScale);
                }
                else
                {
                    posList.Add(Vector3.zero);
                    rotList.Add(Quaternion.identity);
                    scaleList.Add(Vector3.one);
                }
            }

            originalPositions.Add(posList);
            originalRotations.Add(rotList);
            originalScales.Add(scaleList);
        }

        // Start NPC movement if active
        foreach (var entry in npcEntries)
        {
            if (entry.npc != null)
            {
                NpcMovement movement = entry.npc.GetComponent<NpcMovement>();

                if (movement != null && !string.IsNullOrEmpty(entry.startAnimation))
                    movement.SetAnimation(entry.startAnimation);

                if (entry.isActive)
                    StartCoroutine(StartNpcMovementAfterDelay(entry));
            }
        }

        UpdateLlmInstruction();
    }


    public void UpdateLlmInstruction()
    {
        ResetTransforms();

        foreach (var entry in llmEntries)
        {
            if (!entry.isActive) continue;

            if (entry.llmCharacter != null)
                StartCoroutine(SendPromptAfterDelay(entry));
            else
                Debug.LogWarning("LlmEntry missing llmCharacter.");
        }
    }


    private System.Collections.IEnumerator StartNpcMovementAfterDelay(NpcEntry entry)
    {
        yield return new WaitForSeconds(entry.delaySeconds);

        NpcMovement movement = entry.npc.GetComponent<NpcMovement>();
        if (movement != null)
            movement.StartMovement();
        else
            Debug.LogWarning($"NpcMovement missing on {entry.npc.name}");
    }


    private System.Collections.IEnumerator SendPromptAfterDelay(LlmEntry entry)
    {
        yield return new WaitForSeconds(entry.delaySeconds);

        DifficultyLevel effectiveDifficulty = useGlobalDifficulty ? globalDifficulty : entry.difficulty;

        string finalPrompt = effectiveDifficulty == DifficultyLevel.Challenging
            ? entry.promptTextChallenging
            : entry.promptText;

        if (!string.IsNullOrEmpty(finalPrompt))
            entry.llmCharacter.SetPrompt(finalPrompt);
        else
            Debug.LogWarning($"Missing prompt for {entry.llmCharacter.name}");
    }

    public LLM_Handler GetQuestCharacter()
    {
        if ((currentQuest >= 0 && currentQuest <= llmQuestEntries.Count) && llmQuestEntries[0].questCharacter.Count > 0)
            return llmQuestEntries[0].questCharacter[0];
        return null;
    }

    public void SetLlmQuestByIndex(int index, int questIndex)
    {
        ResetTransforms();

        if (index < 0 || index >= llmQuestEntries.Count)
        {
            Debug.LogWarning($"Invalid LLM index {index}");
            return;
        }

        var entry = llmQuestEntries[index];

        if (questIndex < 0 || questIndex >= entry.promptTexts.Count)
        {
            Debug.LogWarning($"Invalid quest index {questIndex}");
            return;
        }

        currentEntry = index;
        currentQuest = questIndex;

        if (entry.isActive &&
            entry.llmCharacter != null &&
            questIndex < entry.promptTexts.Count &&
            questIndex < entry.quests.Count)
        {
            string finalPrompt = entry.promptTexts[questIndex];

            if (!string.IsNullOrEmpty(finalPrompt))
            {
                entry.llmCharacter.SetPrompt(finalPrompt);
                entry.llmHandler.EvaluationString = entry.quests[questIndex];
            }
            else
            {
                Debug.LogWarning($"Missing prompt for {entry.llmCharacter.name}");
            }
        }
    }


    public void SetCurrentQuestSuccessful()
    {
        if (currentEntry >= 0 &&
            currentEntry < llmQuestEntries.Count &&
            currentQuest >= 0 &&
            currentQuest < llmQuestEntries[currentEntry].questSuccessfull.Count)
        {
            llmQuestEntries[currentEntry].questSuccessfull[currentQuest].SetActive(true);
        }
    }


    // ----------------------------------------------------------------------
    // RESET FULL WORLD TRANSFORMS FOR ALL QUEST SUCCESS OBJECTS
    // ----------------------------------------------------------------------
    public void ResetTransforms()
    {
        if (originalPositions.Count != llmQuestEntries.Count)
        {
            Debug.LogWarning("Transform list mismatch — cannot reset.");
            return;
        }

        for (int i = 0; i < llmQuestEntries.Count; i++)
        {
            var entry = llmQuestEntries[i];

            for (int j = 0; j < entry.questSuccessfull.Count; j++)
            {
                GameObject obj = entry.questSuccessfull[j];
                if (obj != null)
                {
                    obj.transform.position = originalPositions[i][j];
                    obj.transform.rotation = originalRotations[i][j];
                    obj.transform.localScale = originalScales[i][j];

                    obj.SetActive(false);
                }
            }
        }
    }
}
