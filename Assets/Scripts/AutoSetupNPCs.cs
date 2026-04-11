using UnityEngine;
using AiSims;
using System.Collections.Generic;

public class AutoSetupNPCs : MonoBehaviour
{
    private HashSet<string> femaleNames = new HashSet<string>()
    {
        "Alex", "Clara", "Christina", "Nina", "Sarah", "Mia",
        "Bea", "Kathi", "Barbara", "Anna", "Lara", "Elly"
    };

    void Start()
    {
        var npcs = FindObjectsOfType<NPCToStoryBridge>();

        foreach (var npc in npcs)
        {
            GameObject go = npc.gameObject;

            var talk = go.GetComponent<Talk>();

            if (talk == null)
                talk = go.AddComponent<Talk>();

            var maleTts = GameObject.Find("TTS_Male")?.GetComponent<PiperTtsService>();
            var femaleTts = GameObject.Find("TTS_Female")?.GetComponent<PiperTtsService>();

            talk.maleTts = maleTts;
            talk.femaleTts = femaleTts;


            var profile = go.GetComponent<NPCVoiceProfile>();

            if (profile == null)
                profile = go.AddComponent<NPCVoiceProfile>();

            string npcName = go.name.Replace("(Clone)", "").Trim();

            if (femaleNames.Contains(npcName))
            {
                profile.voiceType = NPCVoiceProfile.VoiceType.Female;
            }
            else
            {
                profile.voiceType = NPCVoiceProfile.VoiceType.Male;
            }

            Debug.Log($"[VOICE SETUP] {npcName} -> {profile.voiceType}");
        }

        Debug.Log("NPC voice setup COMPLETE");
    }
}