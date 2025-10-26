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
    
    [Header("Portrait Scaling")]
    public bool enablePortraitScaling = true;
    public float inactiveScale = 0.9f;
    public float hoverScale = 0.95f;
    public float selectedScale = 1.0f;
    
    [Header("Portrait Frames")]
    public Sprite dashedFrameSprite;
    public Sprite solidFrameSprite;
    
    // Selection state
    private bool isSelectionActive = false;
    private int targetSelectionCount = 0;
    private HashSet<int> selectedCharacters = new HashSet<int>();
    private int hoveredCharacter = -1;
    
    // Animation state for completion
    private bool isAnimatingCompletion = false;
    private float completionAnimationTime = 0.5f;
    private float completionTimer = 0f;
    private float[] originalWidths;
    
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
        // Store original widths
        originalWidths = new float[characterPortraits.Length];
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            originalWidths[i] = characterPortraits[i].GetComponent<RectTransform>().rect.width;
        }
        
        // Initialize all portraits as inactive
        SetAllPortraitsInactive();
        
        // Start the first conversation selection
        StartNewRound();
    }
    
    void Update()
    {
        if (isAnimatingCompletion)
        {
            HandleCompletionAnimation();
            return;
        }
        
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
            promptText.color = dialogManager != null ? dialogManager.textUnspoken : Color.white;
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
        
        float targetScale = 1.0f; // Default scale when scaling is disabled
        
        if (selectedCharacters.Contains(characterIndex))
        {
            if (enablePortraitScaling) targetScale = selectedScale;
        }
        else if (characterIndex == hoveredCharacter)
        {
            if (enablePortraitScaling) targetScale = hoverScale;
        }
        else
        {
            if (enablePortraitScaling) targetScale = inactiveScale;
        }
        characterPortraits[characterIndex].transform.localScale = Vector3.one * targetScale;
        
        // Update frame sprite based on selection state
        Image portrait = characterPortraits[characterIndex].GetComponent<Image>();
        if (portrait != null)
        {
            if (selectedCharacters.Contains(characterIndex))
            {
                // Selected: use solid frame
                if (solidFrameSprite != null)
                    portrait.sprite = solidFrameSprite;
            }
            else
            {
                // Not selected: use dashed frame
                if (dashedFrameSprite != null)
                    portrait.sprite = dashedFrameSprite;
            }
        }
        
        // Update character sprite based on hover/selection state
        if (dialogManager != null)
        {
            if (selectedCharacters.Contains(characterIndex) || characterIndex == hoveredCharacter)
            {
                // Selected or hovered: eyes open
                dialogManager.SetCharacterToEyesOpen(characterIndex);
            }
            else
            {
                // Not selected and not hovered: eyes closed
                dialogManager.SetCharacterToEyesClosed(characterIndex);
            }
        }
    }
    
    void SetAllPortraitsInactive()
    {
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i] != null)
            {
                // Set inactive scale (if scaling is enabled)
                float scale = enablePortraitScaling ? inactiveScale : 1.0f;
                characterPortraits[i].transform.localScale = Vector3.one * scale;
                
                // Set dashed frame for inactive state
                Image portrait = characterPortraits[i].GetComponent<Image>();
                if (portrait != null && dashedFrameSprite != null)
                {
                    portrait.sprite = dashedFrameSprite;
                }
            }
        }
        
        // Set all characters to eyes-closed during selection phase
        if (dialogManager != null)
        {
            dialogManager.SetAllCharactersToEyesClosed();
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
        
        // Keep selected portraits active
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (selectedCharacters.Contains(i))
            {
                characterPortraits[i].gameObject.SetActive(true);
            }
            else
            {
                Image portrait = characterPortraits[i].GetComponent<Image>();
                portrait.color = Color.clear; // Make invisible but still interactable during animation
            }
        }
        
        // Start completion animation for unselected portraits
        
        isAnimatingCompletion = true;
        completionTimer = 0f;
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
    
    void HandleCompletionAnimation()
    {
        completionTimer += Time.deltaTime;
        float progress = completionTimer / completionAnimationTime;
        
        if (progress >= 1.0f)
        {
            // Animation complete - disable unselected portraits
            for (int i = 0; i < characterPortraits.Length; i++)
            {
                if (!selectedCharacters.Contains(i))
                {
                    characterPortraits[i].gameObject.SetActive(false);
                }
            }
            
            isAnimatingCompletion = false;
            return;
        }
        
        // Apply easing to the progress (ease-out cubic for smooth deceleration)
        float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
        
        // Animate unselected portraits (width shrinking only)
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (!selectedCharacters.Contains(i))
            {
                RectTransform rectTransform = characterPortraits[i].GetComponent<RectTransform>();
                float currentWidth = Mathf.Lerp(originalWidths[i], -25f, easedProgress);
                rectTransform.sizeDelta = new Vector2(currentWidth, rectTransform.sizeDelta.y);
            }
        }
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
        // Stop any ongoing animations
        isAnimatingCompletion = false;
        completionTimer = 0f;
        
        // Reset DialogManager's portrait scaling system
        if (dialogManager != null)
        {
            dialogManager.ResetPortraitScaling();
        }
        
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
                Image portrait = characterPortraits[i].GetComponent<Image>();
                if (portrait != null)
                {
                    portrait.color = Color.white;
                    // Reset to dashed frame for new selection phase
                    if (dashedFrameSprite != null)
                        portrait.sprite = dashedFrameSprite;
                }
                
                // Reset to original width
                if (originalWidths != null && i < originalWidths.Length)
                {
                    RectTransform rectTransform = characterPortraits[i].GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta = new Vector2(originalWidths[i], rectTransform.sizeDelta.y);
                    }
                }
                
                // Reset scale to inactive state (if scaling is enabled)
                float scale = enablePortraitScaling ? inactiveScale : 1.0f;
                characterPortraits[i].transform.localScale = Vector3.one * scale;
            }
        }
        
        // Reset prompt text color and clear text
        if (promptText != null)
        {
            promptText.color = dialogManager != null ? dialogManager.textUnspoken : Color.white;
            promptText.text = "";
        }
        
        Debug.Log("Selection system reset. Starting new round...");
        
        // Start a new round (selects next conversation and begins selection)
        StartNewRound();
    }
}
