
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
    [Header("Conversation Settings")]
    public string conversationFileName = "Events/Conversations";
    public SelectionManager selectionManager;
    

    
    [Header("Dialog Timing")]
    public float dialogStartDelay = 0.8f; // Delay before starting dialog after selection
    public string waitingPlaceholderText = "...";
    
    // Dialog delay state
    private bool isWaitingToStartDialog = false;
    private float dialogDelayTimer = 0f;
    private Queue<string> pendingDialogQueue = new Queue<string>();
    
    public RectTransform dialogPanel;
    private float shrinkAmount = 0.967f;
    private Vector3 dialogPanelOriginalScale;
    private bool isDialogPanelPunching = false;
    private float dialogPanelPunchTimer = 0f;
    private Camera uiCamera;
    public TextMeshProUGUI dialogText;
    public GameObject speechBubble;

    public Color textSpoken;
    public Color textUnspoken;

    public Image[] characterPortraits;
    public Sprite[] characterSpritesInactive;
    public Sprite[] characterSpritesActive;

    [Header("Portrait Scaling")]
    public bool enablePortraitScaling = true;
    private float activePortraitScale = 1.125f;
    private float portraitScaleTime = 0.1f;

    private Queue<string> dialogQueue = new Queue<string>();
    private int currentSpeaker = -1;
    private int previousSpeaker = -1;

    private bool isScaling = false;
    private float[] portraitScaleTimers;
    private Vector2[] portraitStartSizes;
    private Vector2[] portraitTargetSizes;
    private Vector2[] portraitBaseSizes;

    // Character name to index mapping
    private int GetCharacterIndex(string characterName)
    {
        switch (characterName.ToLower())
        {
            case "waif": return 0;
            case "priestess": return 1;
            case "warder": return 2;
            case "pilot": return 3;
            default: return -1; // Unknown character
        }
    }

    void Start()
    {
        if (dialogPanel != null)
        {
            dialogPanelOriginalScale = dialogPanel.localScale;
        }
        uiCamera = Camera.main;

        portraitScaleTimers = new float[characterPortraits.Length];
        portraitStartSizes = new Vector2[characterPortraits.Length];
        portraitTargetSizes = new Vector2[characterPortraits.Length];
        portraitBaseSizes = new Vector2[characterPortraits.Length];
        
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] != null && characterPortraits[i].transform.parent != null)
            {
                RectTransform parentRectTransform = characterPortraits[i].transform.parent.GetComponent<RectTransform>();
                if (parentRectTransform != null)
                {
                    portraitBaseSizes[i] = parentRectTransform.sizeDelta;
                    portraitStartSizes[i] = parentRectTransform.sizeDelta;
                    portraitTargetSizes[i] = parentRectTransform.sizeDelta;
                }
            }
            portraitScaleTimers[i] = 0f;
        }
    }

    void Update()
    {
        // Handle dialog panel punch effect (scale)
        if (isDialogPanelPunching && dialogPanel != null)
        {
            dialogPanelPunchTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dialogPanelPunchTimer / portraitScaleTime);
            float punchT = Mathf.Sin(t * Mathf.PI * 0.5f);
            dialogPanel.localScale = Vector3.Lerp(dialogPanelOriginalScale * shrinkAmount, dialogPanelOriginalScale, punchT);
            if (t >= 1f)
            {
                dialogPanel.localScale = dialogPanelOriginalScale;
                isDialogPanelPunching = false;
            }
        }
        // Handle portrait sizing
        if (isScaling)
        {
            bool allDone = true;
            for (int i = 0; i < characterPortraits.Length; i++)
            {
                if (portraitScaleTimers[i] < portraitScaleTime && characterPortraits[i] != null)
                {
                    portraitScaleTimers[i] += Time.deltaTime;
                    float t = Mathf.Clamp01(portraitScaleTimers[i] / portraitScaleTime);
                    Vector2 currentSize = Vector2.Lerp(portraitStartSizes[i], portraitTargetSizes[i], t);
                    
                    if (characterPortraits[i].transform.parent != null)
                    {
                        RectTransform parentRectTransform = characterPortraits[i].transform.parent.GetComponent<RectTransform>();
                        if (parentRectTransform != null)
                            parentRectTransform.sizeDelta = currentSize;
                    }
                        
                    if (t < 1f) allDone = false;
                }
            }
            if (allDone)
            {
                isScaling = false;
            }
        }

        // Handle dialog start delay
        if (isWaitingToStartDialog)
        {
            dialogDelayTimer += Time.deltaTime;
            if (dialogDelayTimer >= dialogStartDelay)
            {
                // Delay complete - start the actual dialog
                isWaitingToStartDialog = false;
                dialogQueue.Clear();
                foreach (string line in pendingDialogQueue)
                {
                    dialogQueue.Enqueue(line);
                }
                pendingDialogQueue.Clear();
                Debug.Log("Dialog delay complete. Starting conversation.");
                StartDialog();
            }
            return; // Don't process input while waiting
        }

        // Only allow dialog advancement if we're not in selection phase
        bool canAdvanceDialog = selectionManager == null || !selectionManager.IsSelectionActive();
        
        bool advance = false;
        if (canAdvanceDialog && !isScaling && Input.GetKeyDown(KeyCode.Space))
        {
            advance = true;
        }
        else if (canAdvanceDialog && !isScaling && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (RectTransformUtility.RectangleContainsScreenPoint(dialogPanel, mousePos, uiCamera))
            {
                advance = true;
            }
        }
        if (advance)
        {
            ShowNextDialog();
        }
    }

    void ShowNextDialog()
    {
        // Start punch effect on dialog panel (scale)
        if (dialogPanel != null)
        {
            dialogPanel.localScale = dialogPanelOriginalScale * shrinkAmount;
            dialogPanelPunchTimer = 0f;
            isDialogPanelPunching = true;
        }
        if (dialogQueue.Count == 0)
        {
            dialogText.text = "";
            HighlightSpeaker(-1);
            speechBubble.SetActive(false);
            // Smoothly scale last speaker's portrait parent back to 1
            StartPortraitScale(currentSpeaker, -1);
            currentSpeaker = -1;
            
            // Reset the selection system for a new selection phase
            if (selectionManager != null)
            {
                selectionManager.ResetForNewSelection();
            }
            
            return;
        }

        string line = dialogQueue.Dequeue();
        int colonIndex = line.IndexOf(":");
        if (colonIndex > 0)
        {
            string speakerStr = line.Substring(0, colonIndex).Trim();
            string dialog = line.Substring(colonIndex + 1).Trim();
            
            // Try to parse as number first (for backwards compatibility)
            int speakerIndex = -1;
            if (int.TryParse(speakerStr, out speakerIndex))
            {
                // Using numeric index
            }
            else
            {
                // Try to parse as character name
                speakerIndex = GetCharacterIndex(speakerStr);
            }
            
            if (speakerIndex >= 0 && speakerIndex < 4)
            {
                previousSpeaker = currentSpeaker;
                currentSpeaker = speakerIndex;
                dialogText.text = $"\"{dialog}\""; // Add quotation marks
                dialogText.color = textSpoken;
                HighlightSpeaker(currentSpeaker);
                if (speechBubble != null) speechBubble.SetActive(true);
                StartPortraitScale(previousSpeaker, currentSpeaker);
            }
            else
            {
                // Unknown speaker: treat as unspoken text
                dialogText.text = line;
                dialogText.color = textUnspoken;
                HighlightSpeaker(-1);
                if (speechBubble != null) speechBubble.SetActive(false);
                StartPortraitScale(currentSpeaker, -1);
            }
        }
        else
        {
            // Descriptive/unspoken text
            dialogText.text = line;
            dialogText.color = textUnspoken;
            HighlightSpeaker(-1);
            if (speechBubble != null) speechBubble.SetActive(false);
            StartPortraitScale(currentSpeaker, -1);
        }
    }

    void HighlightSpeaker(int speakerIndex)
    {
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (i == speakerIndex && i < characterSpritesActive.Length)
            {
                characterPortraits[i].sprite = characterSpritesActive[i];
            }
            else if (i < characterSpritesInactive.Length)
            {
                characterPortraits[i].sprite = characterSpritesInactive[i];
            }
        }
    }

    void StartPortraitScale(int prev, int curr)
    {
        if (!enablePortraitScaling)
        {
            isScaling = false;
            return;
        }

        // Reset all timers and set targets
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] != null && characterPortraits[i].transform.parent != null)
            {
                RectTransform parentRectTransform = characterPortraits[i].transform.parent.GetComponent<RectTransform>();
                if (parentRectTransform != null)
                {
                    portraitStartSizes[i] = parentRectTransform.sizeDelta;
                    
                    if (i == curr)
                    {
                        // Scale up by activePortraitScale factor
                        portraitTargetSizes[i] = portraitBaseSizes[i] * activePortraitScale;
                        portraitScaleTimers[i] = 0f;
                    }
                    else
                    {
                        // Return to base size
                        portraitTargetSizes[i] = portraitBaseSizes[i];
                        portraitScaleTimers[i] = 0f;
                    }
                }
            }
        }
        isScaling = true;
    }
    
    // Conversation Loading Methods
    public void LoadConversationForCharacters(List<string> selectedCharacterNames)
    {
        List<string> conversation = new List<string>();
        string foundSectionName = "";
        
        // Try multiple name orderings to find the conversation
        List<List<string>> namePermutations = GenerateNamePermutations(selectedCharacterNames);
        
        foreach (List<string> nameOrder in namePermutations)
        {
            string sectionName = "[" + string.Join("-", nameOrder) + "]";
            Debug.Log($"Trying conversation section: {sectionName}");
            
            conversation = LoadConversationFromFile(sectionName);
            if (conversation.Count > 0)
            {
                foundSectionName = sectionName;
                break;
            }
        }
        
        if (conversation.Count > 0)
        {
            // Store conversation in pending queue for delayed start
            pendingDialogQueue.Clear();
            foreach (string line in conversation)
            {
                pendingDialogQueue.Enqueue(line);
            }
            Debug.Log($"Successfully loaded conversation from section: {foundSectionName}");
            
            // Start the delay timer and show placeholder text
            StartDialogWithDelay();
        }
        else
        {
            Debug.LogWarning($"No conversation found for character combination: {string.Join(", ", selectedCharacterNames)}");
        }
    }
    
    private List<List<string>> GenerateNamePermutations(List<string> names)
    {
        List<List<string>> permutations = new List<List<string>>();
        
        if (names.Count == 1)
        {
            // Single character - only one possibility
            permutations.Add(new List<string>(names));
        }
        else if (names.Count == 2)
        {
            // Two characters - use character hierarchy order (Waif, Priestess, Warder, Pilot)
            List<string> orderedNames = OrderByCharacterHierarchy(names);
            permutations.Add(orderedNames);
            
            // Also try the reverse order as backup
            List<string> reversedNames = new List<string>(orderedNames);
            reversedNames.Reverse();
            permutations.Add(reversedNames);
        }
        else if (names.Count == 3)
        {
            // Three characters - try hierarchy order and alphabetical
            List<string> hierarchyOrder = OrderByCharacterHierarchy(names);
            permutations.Add(hierarchyOrder);
            
            List<string> alphabeticalOrder = new List<string>(names);
            alphabeticalOrder.Sort();
            if (!AreListsEqual(hierarchyOrder, alphabeticalOrder))
            {
                permutations.Add(alphabeticalOrder);
            }
        }
        
        return permutations;
    }
    
    private List<string> OrderByCharacterHierarchy(List<string> names)
    {
        // Define the character hierarchy order
        string[] characterOrder = { "Waif", "Priestess", "Warder", "Pilot" };
        
        List<string> orderedNames = new List<string>();
        
        foreach (string character in characterOrder)
        {
            if (names.Contains(character))
            {
                orderedNames.Add(character);
            }
        }
        
        return orderedNames;
    }
    
    private bool AreListsEqual(List<string> list1, List<string> list2)
    {
        if (list1.Count != list2.Count) return false;
        
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i]) return false;
        }
        
        return true;
    }
    
    private void StartDialogWithDelay()
    {
        // Clear any existing dialog and show placeholder
        dialogQueue.Clear();
        dialogText.text = waitingPlaceholderText;
        dialogText.color = textUnspoken;
        
        // Hide speech bubble during waiting
        if (speechBubble != null) 
            speechBubble.SetActive(false);
        
        // Start delay timer
        isWaitingToStartDialog = true;
        dialogDelayTimer = 0f;
        
        Debug.Log($"Starting dialog delay ({dialogStartDelay}s) with placeholder text: '{waitingPlaceholderText}'");
    }
    
    public void StartDialog()
    {
        if (dialogQueue.Count > 0)
        {
            ShowNextDialog();
        }
    }
    
    private List<string> LoadConversationFromFile(string sectionName)
    {
        List<string> conversationLines = new List<string>();
        
        // Load text file from Resources folder
        TextAsset conversationFile = Resources.Load<TextAsset>(conversationFileName);
        
        if (conversationFile == null)
        {
            Debug.LogError($"Conversation file not found in Resources folder at: '{conversationFileName}'");
            return conversationLines;
        }
        
        Debug.Log($"Successfully loaded conversation file: '{conversationFileName}'");
        
        try
        {
            string[] allLines = conversationFile.text.Split('\n');
            bool inTargetSection = false;
            
            foreach (string line in allLines)
            {
                string trimmedLine = line.Trim();
                
                // Check if we're starting a new section
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    // If this is our target section, start capturing
                    if (trimmedLine.Equals(sectionName))
                    {
                        inTargetSection = true;
                        Debug.Log($"Found section: {sectionName}");
                    }
                    else
                    {
                        // We've hit a different section, stop capturing if we were in target section
                        inTargetSection = false;
                    }
                }
                else if (inTargetSection && !string.IsNullOrEmpty(trimmedLine))
                {
                    // We're in the target section and this is a dialog line
                    conversationLines.Add(trimmedLine);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading conversation file: {e.Message}");
        }
        
        return conversationLines;
    }
    
    // Helper method to get character names from indices (for SelectionManager integration)
    public List<string> GetCharacterNames(HashSet<int> selectedIndices)
    {
        string[] characterNames = { "Waif", "Priestess", "Warder", "Pilot" };
        List<string> names = new List<string>();
        
        foreach (int index in selectedIndices)
        {
            if (index >= 0 && index < characterNames.Length)
            {
                names.Add(characterNames[index]);
            }
        }
        
        return names;
    }
    
    // Reset portrait scaling system (called when transitioning back to selection)
    public void ResetPortraitScaling()
    {
        // Stop any ongoing scaling animations
        isScaling = false;
        
        // Reset all portrait sizes to base size
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] != null && characterPortraits[i].transform.parent != null)
            {
                RectTransform parentRectTransform = characterPortraits[i].transform.parent.GetComponent<RectTransform>();
                if (parentRectTransform != null && portraitBaseSizes != null && i < portraitBaseSizes.Length)
                {
                    parentRectTransform.sizeDelta = portraitBaseSizes[i];
                }
            }
            
            // Reset timer values
            if (portraitScaleTimers != null && i < portraitScaleTimers.Length)
            {
                portraitScaleTimers[i] = 0f;
                if (portraitStartSizes != null && i < portraitStartSizes.Length)
                    portraitStartSizes[i] = portraitBaseSizes[i];
                if (portraitTargetSizes != null && i < portraitTargetSizes.Length)
                    portraitTargetSizes[i] = portraitBaseSizes[i];
            }
        }
        
        Debug.Log("DialogManager portrait scaling system reset");
    }
    
    // Test method for delay functionality
    [ContextMenu("Test Dialog Delay")]
    public void TestDialogDelay()
    {
        // Simulate a simple conversation for testing
        pendingDialogQueue.Clear();
        pendingDialogQueue.Enqueue("Waif: This is a test conversation.");
        pendingDialogQueue.Enqueue("Priestess: Testing the delay system.");
        
        StartDialogWithDelay();
    }
}
