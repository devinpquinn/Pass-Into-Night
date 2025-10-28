using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI promptText;
    public Image[] characterPortraits = new Image[4]; // Waif, Priestess, Warder, Pilot (must have CanvasGroup components)
    public DialogManager dialogManager;
    
    [Header("Portrait Sprites")]
    public Sprite[] deselectedSprites = new Sprite[4]; // Default state during selection
    public Sprite[] hoveredSprites = new Sprite[4];    // Mouse hover during selection
    public Sprite[] selectedSprites = new Sprite[4];   // Selected during selection
    public Sprite[] speakingSprites = new Sprite[4];   // Speaking during conversation
    
    // Selection state
    private bool isSelectionActive = false;
    private int targetSelectionCount = 0;
    private HashSet<int> selectedCharacters = new HashSet<int>();
    private int hoveredCharacter = -1;
    
    // Character names for logging
    private string[] characterNames = { "Waif", "Priestess", "Warder", "Pilot" };
    
    string GetNumberWord(int number)
    {
        switch (number)
        {
            case 1: return "one";
            case 2: return "two";
            case 3: return "three";
            default: return number.ToString(); // Fallback to digit if not handled
        }
    }
    
    void Start()
    {
        // Initialize all portraits as inactive
        SetAllPortraitsInactive();
        
        // Start the first conversation selection
        StartNewRound();
    }
    
    void Update()
    {
        if (!isSelectionActive) return;
        
        HandleMouseInput();
    }
    
    public void StartSelection(int characterCount)
    {
        if (characterCount < 1 || characterCount > 3)
        {
            Debug.LogError("Invalid character count. Must be between 1 and 3.");
            return;
        }
        
        isSelectionActive = true;
        targetSelectionCount = characterCount;
        selectedCharacters.Clear();
        hoveredCharacter = -1;
        
        // Set prompt text with written numbers and blurb
        string numberWord = GetNumberWord(characterCount);
        
        // Get blurb from current conversation
        string blurb = "";
        if (dialogManager != null)
        {
            Conversation currentConversation = dialogManager.GetCurrentConversation();
            if (currentConversation != null && !string.IsNullOrEmpty(currentConversation.Blurb))
            {
                blurb = " " + currentConversation.Blurb;
            }
        }
        
        string promptMessage = characterCount == 1 ? 
            $"Select <u>{numberWord}</u> traveler{blurb}." : 
            $"Select <u>{numberWord}</u> travelers{blurb}.";
        
        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
        
        // Set all portraits to inactive state
        SetAllPortraitsInactive();
        
        Debug.Log($"Selection phase started. Target: {characterCount} characters");
    }
    
    void HandleMouseInput()
    {
        Vector2 mousePosition = Input.mousePosition;
        int newHoveredCharacter = -1;
        
        // Check which portrait the mouse is over
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] != null && 
                RectTransformUtility.RectangleContainsScreenPoint(
                    characterPortraits[i].rectTransform, mousePosition, Camera.main))
            {
                newHoveredCharacter = i;
                break;
            }
        }
        
        // Handle hover state changes
        if (newHoveredCharacter != hoveredCharacter)
        {
            int previousHover = hoveredCharacter;
            hoveredCharacter = newHoveredCharacter;
            
            // Update visual for previously hovered character (removes hover state)
            if (previousHover >= 0)
            {
                UpdatePortraitVisual(previousHover);
            }
            
            // Update visual for newly hovered character (applies hover state)
            if (hoveredCharacter >= 0)
            {
                UpdatePortraitVisual(hoveredCharacter);
            }
        }
        
        // Handle mouse clicks
        if (Input.GetMouseButtonDown(0) && hoveredCharacter >= 0)
        {
            ToggleCharacterSelection(hoveredCharacter);
        }
    }
    
    void ToggleCharacterSelection(int characterIndex)
    {
        if (selectedCharacters.Contains(characterIndex))
        {
            // Deselect character
            selectedCharacters.Remove(characterIndex);
            Debug.Log($"Deselected {characterNames[characterIndex]}");
        }
        else
        {
            // Select character (if we haven't reached the limit)
            if (selectedCharacters.Count < targetSelectionCount)
            {
                selectedCharacters.Add(characterIndex);
                Debug.Log($"Selected {characterNames[characterIndex]}");
            }
            else
            {
                Debug.Log($"Cannot select {characterNames[characterIndex]} - already have {targetSelectionCount} characters selected");
                return;
            }
        }
        
        UpdatePortraitVisual(characterIndex);
        
        // Check if selection is complete
        if (selectedCharacters.Count == targetSelectionCount)
        {
            CompleteSelection();
        }
    }
    
    void UpdatePortraitVisual(int characterIndex)
    {
        if (characterPortraits[characterIndex] == null) return;
        
        Image portrait = characterPortraits[characterIndex];
        
        // Determine which sprite to use based on selection state
        if (selectedCharacters.Contains(characterIndex))
        {
            // Selected: use selected sprite
            if (characterIndex < selectedSprites.Length && selectedSprites[characterIndex] != null)
                portrait.sprite = selectedSprites[characterIndex];
        }
        else if (characterIndex == hoveredCharacter)
        {
            // Hovered but not selected: use hovered sprite
            if (characterIndex < hoveredSprites.Length && hoveredSprites[characterIndex] != null)
                portrait.sprite = hoveredSprites[characterIndex];
        }
        else
        {
            // Neither selected nor hovered: use deselected sprite
            if (characterIndex < deselectedSprites.Length && deselectedSprites[characterIndex] != null)
                portrait.sprite = deselectedSprites[characterIndex];
        }
    }
    
    void SetAllPortraitsInactive()
    {
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] != null)
            {
                // Set deselected sprite for inactive state
                if (i < deselectedSprites.Length && deselectedSprites[i] != null)
                {
                    characterPortraits[i].sprite = deselectedSprites[i];
                }
            }
        }
    }
    
    void CompleteSelection()
    {
        isSelectionActive = false;
        hoveredCharacter = -1;
        
        // Log the selected roster
        List<string> selectedNames = new List<string>();
        foreach (int index in selectedCharacters)
        {
            selectedNames.Add(characterNames[index]);
        }
        
        Debug.Log($"Selection complete! Selected characters: {string.Join(", ", selectedNames)}");
        
        // Load the appropriate conversation in DialogManager
        if (dialogManager != null)
        {
            dialogManager.LoadConversationForCharacters(selectedNames);
        }
        
        // Keep selected portraits active and instantly deactivate unselected ones
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (selectedCharacters.Contains(i))
            {
                characterPortraits[i].gameObject.SetActive(true);
                // Set to selected state for conversation phase
                SetCharacterToSelected(i);
            }
            else
            {
                // Instantly deactivate unselected portraits
                characterPortraits[i].gameObject.SetActive(false);
            }
        }
    }
    
    // Public methods for external use
    public bool IsSelectionActive()
    {
        return isSelectionActive;
    }
    
    public HashSet<int> GetSelectedCharacters()
    {
        return new HashSet<int>(selectedCharacters);
    }
    
    public void EndSelection()
    {
        isSelectionActive = false;
        hoveredCharacter = -1;
        selectedCharacters.Clear();
        
        if (promptText != null)
            promptText.text = "";
        
        SetAllPortraitsInactive();
    }
    
    // Method to start a new round (selects next conversation and begins selection)
    public void StartNewRound()
    {
        if (dialogManager == null)
        {
            Debug.LogError("DialogManager not assigned to SelectionManager!");
            return;
        }
        
        // Select the next conversation from the database
        Conversation nextConversation = dialogManager.SelectNextConversation();
        
        if (nextConversation == null)
        {
            Debug.LogError("No conversation available! Cannot start new round.");
            return;
        }
        
        // Start selection with the participant count from the conversation
        StartSelection(nextConversation.ParticipantCount);
    }
    
    // Reset method for starting new selection after dialog completion
    public void ResetForNewSelection()
    {
        // Reset selection state
        isSelectionActive = false;
        selectedCharacters.Clear();
        hoveredCharacter = -1;
        
        // Reset all portraits to visible and inactive state
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] != null)
            {
                // Make sure portraits are active and visible
                characterPortraits[i].gameObject.SetActive(true);
                
                // Reset image color and sprite (in case it was set to clear during animation)
                Image portrait = characterPortraits[i];
                if (portrait != null)
                {
                    portrait.color = Color.white;
                    // Reset to deselected sprite for new selection phase
                    if (i < deselectedSprites.Length && deselectedSprites[i] != null)
                        portrait.sprite = deselectedSprites[i];
                }
                
                // Reset alpha to fully visible
                CanvasGroup canvasGroup = characterPortraits[i].GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1.0f;
                }
            }
        }
        
        // Reset prompt text color and clear text
        if (promptText != null)
        {
            promptText.text = "";
        }
        
        Debug.Log("Selection system reset. Starting new round...");
        
        // Start a new round (selects next conversation and begins selection)
        StartNewRound();
    }
    
    // Methods for managing portrait sprites during conversation phase
    public void SetCharacterToHovered(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < characterPortraits.Length && 
            characterPortraits[characterIndex] != null &&
            characterIndex < hoveredSprites.Length &&
            hoveredSprites[characterIndex] != null)
        {
            characterPortraits[characterIndex].sprite = hoveredSprites[characterIndex];
        }
    }
    
    public void SetCharacterToSelected(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < characterPortraits.Length && 
            characterPortraits[characterIndex] != null &&
            characterIndex < selectedSprites.Length &&
            selectedSprites[characterIndex] != null)
        {
            characterPortraits[characterIndex].sprite = selectedSprites[characterIndex];
        }
    }
    
    public void SetCharacterToSpeaking(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < characterPortraits.Length && 
            characterPortraits[characterIndex] != null &&
            characterIndex < speakingSprites.Length &&
            speakingSprites[characterIndex] != null)
        {
            characterPortraits[characterIndex].sprite = speakingSprites[characterIndex];
        }
    }
    
    public void SetAllCharactersToHovered()
    {
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            SetCharacterToHovered(i);
        }
    }
    
    public void SetAllCharactersToSelected()
    {
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            SetCharacterToSelected(i);
        }
    }
}
