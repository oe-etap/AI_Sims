using UnityEngine;
using UnityEngine.UI;
using static AiSims.PlayerInputHandler;

namespace AiSims
{
    /// <summary>
    /// Controls global difficulty changes and updates the displayed value.
    /// </summary>
    public class ChangeComplexity : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The Text component used to display the current difficulty state.")]
        private Text m_Text;

        [Header("Game Complexity")]
        public Complexity complexity;

        [Header("Current Difficulty")]
        public DifficultyLevel difficultyLevel = DifficultyLevel.Easy;

        [Header("Clear Chat")]
        public ClearAllLLMChats clearChat;

        public Text text
        {
            get => m_Text;
            set => m_Text = value;
        }

        private int m_Count = 0;

        protected void Awake()
        {
            if (m_Text == null)
                Debug.LogWarning("Missing required Text component reference. Assign a Text element in the Inspector.", this);
        }

        protected void Start()
        {
            // Randomize m_Count between 0 and 1
            m_Count = Random.Range(0, 2); // returns 0 or 1

            // Update difficulty based on the random value
            UpdateDifficultyFromCount();

            // Update the UI display
            if (m_Text != null)
                m_Text.text = m_Count.ToString();

            // Apply the difficulty to the Complexity system
            if (complexity != null)
            {
                complexity.globalDifficulty = difficultyLevel;
                complexity.UpdateLlmInstruction();
            }

            Debug.Log($"[Complexity] Randomized start: m_Count = {m_Count}, Difficulty = {difficultyLevel}");
            Logger.Log(LoggingInfo.Scene, $"[Complexity] Difficulty = {difficultyLevel}", true);
        }

        /// <summary>
        /// Toggles the difficulty (0 - 1) and updates the Complexity system.
        /// </summary>
        public void Increment()
        {
            // Toggle between 0 and 1
            m_Count = (m_Count == 0) ? 1 : 0;

            // Update difficulty according to m_Count
            UpdateDifficultyFromCount();

            // Update the displayed value
            if (m_Text != null)
                m_Text.text = m_Count.ToString();

            // Apply to Complexity
            if (complexity != null)
            {
                complexity.globalDifficulty = difficultyLevel;
                complexity.UpdateLlmInstruction();
            }

            Debug.Log($"[Complexity] Toggled difficulty: m_Count = {m_Count}, Difficulty = {difficultyLevel}");
            Logger.Log(LoggingInfo.Scene, $"[Complexity] Difficulty = {difficultyLevel}", true);

            if (clearChat != null)
                clearChat.ClearAllChats();
        }

        /// <summary>
        /// Updates the difficulty level based on the current count value.
        /// </summary>
        private void UpdateDifficultyFromCount()
        {
            difficultyLevel = (m_Count == 0)
                ? DifficultyLevel.Easy
                : DifficultyLevel.Challenging;
        }
    }
}
