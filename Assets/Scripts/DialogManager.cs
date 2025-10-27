using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
    [Header("Conversation Settings")]
    public ConversationDatabase conversationDatabase;
    public SelectionManager selectionManager;
    public CharacterManager characterManager;
    
    [Header("Current Conversation")]
    [SerializeField] private Conversation currentConversation;
    

    
    [Header("Dialog Timing")]
    public float dialogStartDelay = 0.8f; // Delay before starting dialog after selection
    public string waitingPlaceholderText = "...";
    
    // Dialog delay state
    private bool isWaitingToStartDialog = false;
    private float dialogDelayTimer = 0f;
    private Queue<string> pendingDialogQueue = new Queue<string>();
    
    public RectTransform dialogPanel;
    private Camera uiCamera;
    public TextMeshProUGUI dialogText;
    
    [Header("Dialog Panel Sprites")]
    public Image dialogPanelImage; // The Image component on DialogPanel
    public Sprite narrativeSprite; // Sprite for narration/thoughts
    public Sprite speechSprite;    // Sprite for speaking

    public Color textSpoken;
    public Color textUnspoken;

    public Image[] characterPortraits;
    public Sprite[] characterSpritesInactive;
    public Sprite[] characterSpritesActive;
    
    [Header("Selection Phase Sprites")]
    public Sprite[] characterSpritesEyesClosed; // Used when not selected/hovered during selection
    public Sprite[] characterSpritesEyesOpen;   // Used when hovered during selection

    private Queue<string> dialogQueue = new Queue<string>();
    private int currentSpeaker = -1;
    private int previousSpeaker = -1;

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
        uiCamera = Camera.main;
    }

    void Update()
    {

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
                
                // Reset character sprites to normal dialog sprites
                ResetCharacterSpritesToNormal();
                
                Debug.Log("Dialog delay complete. Starting conversation.");
                StartDialog();
            }
            return; // Don't process input while waiting
        }

        // Only allow dialog advancement if we're not in selection phase
        bool canAdvanceDialog = selectionManager == null || !selectionManager.IsSelectionActive();
        
        bool advance = false;
        if (canAdvanceDialog && Input.GetKeyDown(KeyCode.Space))
        {
            advance = true;
        }
        else if (canAdvanceDialog && Input.GetMouseButtonDown(0))
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
        if (dialogQueue.Count == 0)
        {
            dialogText.text = "";
            HighlightSpeaker(-1);
            SetDialogPanelSprite(false); // Set to narrative sprite
            currentSpeaker = -1;
            
            // Reset the selection system for a new selection phase
            if (selectionManager != null)
            {
                selectionManager.ResetForNewSelection();
            }
            
            return;
        }

        string line = dialogQueue.Dequeue();
        
        // Check if this line is a command that should be executed
        if (line.StartsWith("{") && line.EndsWith("}") && line.Contains(":"))
        {
            Debug.Log($"Executing command during dialog: {line}");
            ProcessCommand(line);
            
            // After executing command, immediately show next dialog (don't display the command)
            if (dialogQueue.Count > 0)
            {
                ShowNextDialog();
                return;
            }
            else
            {
                // No more dialog after command, end conversation
                dialogText.text = "";
                HighlightSpeaker(-1);
                SetDialogPanelSprite(false); // Set to narrative sprite
                currentSpeaker = -1;
                
                if (selectionManager != null)
                {
                    selectionManager.ResetForNewSelection();
                }
                return;
            }
        }
        
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
                SetDialogPanelSprite(true); // Set to speech sprite
            }
            else
            {
                // Unknown speaker: treat as unspoken text
                dialogText.text = line;
                dialogText.color = textUnspoken;
                HighlightSpeaker(-1);
                SetDialogPanelSprite(false); // Set to narrative sprite
            }
        }
        else
        {
            // Descriptive/unspoken text
            dialogText.text = line;
            dialogText.color = textUnspoken;
            HighlightSpeaker(-1);
            SetDialogPanelSprite(false); // Set to narrative sprite
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
    
    void SetDialogPanelSprite(bool isSpeaking)
    {
        if (dialogPanelImage != null)
        {
            if (isSpeaking && speechSprite != null)
            {
                dialogPanelImage.sprite = speechSprite;
            }
            else if (!isSpeaking && narrativeSprite != null)
            {
                dialogPanelImage.sprite = narrativeSprite;
            }
        }
    }


    
    // Conversation Loading Methods
    public void LoadConversationForCharacters(List<string> selectedCharacterNames)
    {
        if (currentConversation == null)
        {
            Debug.LogError("No current conversation selected! Make sure to call SelectNextConversation() first.");
            return;
        }
        
        List<string> conversation = new List<string>();
        string foundSectionName = "";
        
        // Try multiple name orderings to find the conversation
        List<List<string>> namePermutations = GenerateNamePermutations(selectedCharacterNames);
        
        foreach (List<string> nameOrder in namePermutations)
        {
            string sectionName = "[" + string.Join("-", nameOrder) + "]";
            Debug.Log($"Trying conversation section: {sectionName}");
            
            conversation = LoadConversationFromFile(sectionName, currentConversation.ConversationFile);
            if (conversation.Count > 0)
            {
                foundSectionName = sectionName;
                break;
            }
        }
        
        if (conversation.Count > 0)
        {
            // Process conversation for conditional forks
            List<string> processedConversation = ProcessConversationWithConditions(conversation);
            
            // Store processed conversation in pending queue for delayed start
            pendingDialogQueue.Clear();
            foreach (string line in processedConversation)
            {
                pendingDialogQueue.Enqueue(line);
            }
            Debug.Log($"Successfully loaded and processed conversation from section: {foundSectionName} ({processedConversation.Count} lines after processing)");
            
            // Start the delay timer and show placeholder text
            StartDialogWithDelay();
        }
        else
        {
            Debug.LogWarning($"No conversation found for character combination: {string.Join(", ", selectedCharacterNames)} in conversation '{currentConversation.ConversationName}'");
        }
    }
    
    // Method to select the next conversation from the database
    public Conversation SelectNextConversation()
    {
        if (conversationDatabase == null)
        {
            Debug.LogError("ConversationDatabase not assigned to DialogManager!");
            return null;
        }
        
        currentConversation = conversationDatabase.GetNextConversation();
        
        if (currentConversation == null)
        {
            Debug.LogWarning("No conversations available in database!");
            return null;
        }
        
        Debug.Log($"Selected conversation: {currentConversation.ConversationName} (Participants: {currentConversation.ParticipantCount})");
        return currentConversation;
    }
    
    // Method to get the current conversation
    public Conversation GetCurrentConversation()
    {
        return currentConversation;
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
        
        // Set dialog panel to narrative sprite during waiting
        SetDialogPanelSprite(false);
        
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
    
    private List<string> LoadConversationFromFile(string sectionName, TextAsset conversationFile)
    {
        List<string> conversationLines = new List<string>();
        
        if (conversationFile == null)
        {
            Debug.LogError($"Conversation file is null!");
            return conversationLines;
        }
        
        Debug.Log($"Successfully loaded conversation file: '{conversationFile.name}'");
        
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
    
    // Process conversation with conditional forks
    private List<string> ProcessConversationWithConditions(List<string> rawConversation)
    {
        List<string> processedConversation = new List<string>();
        int i = 0;
        
        Debug.Log($"Processing conversation with {rawConversation.Count} lines");
        
        while (i < rawConversation.Count)
        {
            string line = rawConversation[i];
            Debug.Log($"Processing line {i}: '{line}'");
            
            // Check if this is a command line
            if (line.StartsWith("{") && line.EndsWith("}") && line.Contains(":"))
            {
                if (line.StartsWith("{IF:"))
                {
                    // Handle conditional logic
                    Debug.Log($"Found condition: {line}");
                    bool conditionMet = EvaluateCondition(line);
                    Debug.Log($"Condition result: {conditionMet}");
                    
                    i++; // Move to next line after condition
                    
                    // Process the conditional block
                    List<string> conditionalBlock = new List<string>();
                    List<string> elseBlock = new List<string>();
                    bool inElseBlock = false;
                    bool foundEndif = false;
                    
                    // Collect lines until we hit {ENDIF} or {ELSE}
                    while (i < rawConversation.Count)
                    {
                        string currentLine = rawConversation[i];
                        Debug.Log($"  Conditional block line {i}: '{currentLine}'");
                        
                        if (currentLine.Equals("{ENDIF}"))
                        {
                            foundEndif = true;
                            Debug.Log("  Found {ENDIF}");
                            break; // End of conditional block
                        }
                        else if (currentLine.Equals("{ELSE}"))
                        {
                            inElseBlock = true;
                            Debug.Log("  Found {ELSE}, switching to else block");
                            i++;
                            continue;
                        }
                        
                        if (inElseBlock)
                        {
                            elseBlock.Add(currentLine);
                            Debug.Log($"    Added to else block: '{currentLine}'");
                        }
                        else
                        {
                            conditionalBlock.Add(currentLine);
                            Debug.Log($"    Added to if block: '{currentLine}'");
                        }
                        
                        i++;
                    }
                    
                    if (!foundEndif)
                    {
                        Debug.LogWarning("Conditional block missing {ENDIF} - reached end of conversation");
                    }
                    
                    // Add the appropriate block based on condition result
                    if (conditionMet)
                    {
                        Debug.Log($"Condition met, adding IF block ({conditionalBlock.Count} lines)");
                        processedConversation.AddRange(ProcessConversationWithConditions(conditionalBlock));
                    }
                    else if (elseBlock.Count > 0)
                    {
                        Debug.Log($"Condition not met, adding ELSE block ({elseBlock.Count} lines)");
                        processedConversation.AddRange(ProcessConversationWithConditions(elseBlock));
                    }
                    else
                    {
                        Debug.Log("Condition not met, no ELSE block");
                    }
                }
                else
                {
                    // Handle command (relationship/arc modification)
                    // Instead of executing immediately, preserve the command for later execution
                    Debug.Log($"Preserving command for later execution: '{line}'");
                    processedConversation.Add(line);
                }
            }
            else
            {
                // Regular dialog line, add it directly
                Debug.Log($"Adding regular line: '{line}'");
                processedConversation.Add(line);
            }
            
            i++;
        }
        
        Debug.Log($"Finished processing. Result: {processedConversation.Count} lines");
        return processedConversation;
    }
    
    // Process command lines for relationship/arc modifications
    private void ProcessCommand(string commandLine)
    {
        if (characterManager == null)
        {
            Debug.LogWarning("CharacterManager not assigned, cannot process command");
            return;
        }
        
        Debug.Log($"Processing command: '{commandLine}'");
        
        // Remove the curly brackets and trim
        string command = commandLine.Replace("{", "").Replace("}", "").Trim();
        
        try
        {
            // Parse relationship commands: "SET: Waif->Priestess +2" or "SET: Waif->Priestess = 5"
            if (command.StartsWith("SET:"))
            {
                string setCommand = command.Substring(4).Trim(); // Remove "SET:"
                
                if (setCommand.Contains("<->"))
                {
                    ProcessMutualRelationshipCommand(setCommand);
                }
                else if (setCommand.Contains("->"))
                {
                    ProcessRelationshipCommand(setCommand);
                }
                else if (setCommand.Contains(".arc"))
                {
                    ProcessArcCommand(setCommand);
                }
                else
                {
                    Debug.LogWarning($"Unknown SET command format: {setCommand}");
                }
            }
            else
            {
                Debug.LogWarning($"Unknown command type: {command}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing command '{commandLine}': {e.Message}");
        }
    }
    
    private void ProcessRelationshipCommand(string command)
    {
        try
        {
            Debug.Log($"Processing relationship command: '{command}'");
            
            // Parse format: "Waif->Priestess +2" or "Waif->Priestess = 5"
            string[] parts = command.Split(new string[] { "->" }, System.StringSplitOptions.None);
            string fromChar = parts[0].Trim();
            
            string rightPart = parts[1].Trim();
            
            // Find the operation character and split accordingly
            char operation = ' ';
            string toChar = "";
            int value = 0;
            
            if (rightPart.Contains(" ="))
            {
                operation = '=';
                string[] equalParts = rightPart.Split(new char[] { '=' }, 2);
                toChar = equalParts[0].Trim();
                value = int.Parse(equalParts[1].Trim());
            }
            else if (rightPart.Contains(" +"))
            {
                operation = '+';
                string[] plusParts = rightPart.Split(new char[] { '+' }, 2);
                toChar = plusParts[0].Trim();
                value = int.Parse(plusParts[1].Trim());
            }
            else if (rightPart.Contains(" -"))
            {
                operation = '-';
                string[] minusParts = rightPart.Split(new char[] { '-' }, 2);
                toChar = minusParts[0].Trim();
                value = int.Parse(minusParts[1].Trim());
            }
            else
            {
                Debug.LogWarning($"No valid operation found in: {rightPart}");
                return;
            }
            
            Debug.Log($"Parsed: {fromChar} -> {toChar}, operation: {operation}, value: {value}");
            
            CharacterManager.CharacterID fromID = GetCharacterID(fromChar);
            CharacterManager.CharacterID toID = GetCharacterID(toChar);
            
            if (operation == '=')
            {
                characterManager.SetRelationship(fromID, toID, value);
                Debug.Log($"Set relationship {fromID}->{toID} = {value}");
            }
            else if (operation == '+')
            {
                int currentValue = characterManager.GetRelationship(fromID, toID);
                int newValue = currentValue + value;
                characterManager.SetRelationship(fromID, toID, newValue);
                Debug.Log($"Modified relationship {fromID}->{toID}: {currentValue} + {value} = {newValue}");
            }
            else if (operation == '-')
            {
                int currentValue = characterManager.GetRelationship(fromID, toID);
                int newValue = currentValue - value;
                characterManager.SetRelationship(fromID, toID, newValue);
                Debug.Log($"Modified relationship {fromID}->{toID}: {currentValue} - {value} = {newValue}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing relationship command '{command}': {e.Message}");
        }
    }
    
    private void ProcessMutualRelationshipCommand(string command)
    {
        try
        {
            Debug.Log($"Processing mutual relationship command: '{command}'");
            
            // Parse format: "CharacterA<->CharacterB +2" or "CharacterA<->CharacterB = 5"
            string[] parts = command.Split(new string[] { "<->" }, System.StringSplitOptions.None);
            string char1 = parts[0].Trim();
            
            string rightPart = parts[1].Trim();
            
            // Find the operation character and split accordingly
            char operation = ' ';
            string char2 = "";
            int value = 0;
            
            if (rightPart.Contains(" ="))
            {
                operation = '=';
                string[] equalParts = rightPart.Split(new char[] { '=' }, 2);
                char2 = equalParts[0].Trim();
                value = int.Parse(equalParts[1].Trim());
            }
            else if (rightPart.Contains(" +"))
            {
                operation = '+';
                string[] plusParts = rightPart.Split(new char[] { '+' }, 2);
                char2 = plusParts[0].Trim();
                value = int.Parse(plusParts[1].Trim());
            }
            else if (rightPart.Contains(" -"))
            {
                operation = '-';
                string[] minusParts = rightPart.Split(new char[] { '-' }, 2);
                char2 = minusParts[0].Trim();
                value = int.Parse(minusParts[1].Trim());
            }
            else
            {
                Debug.LogWarning($"No valid operation found in mutual relationship: {rightPart}");
                return;
            }
            
            Debug.Log($"Parsed mutual: {char1} <-> {char2}, operation: {operation}, value: {value}");
            
            CharacterManager.CharacterID char1ID = GetCharacterID(char1);
            CharacterManager.CharacterID char2ID = GetCharacterID(char2);
            
            // Apply the operation to both directions
            if (operation == '=')
            {
                characterManager.SetRelationship(char1ID, char2ID, value);
                characterManager.SetRelationship(char2ID, char1ID, value);
                Debug.Log($"Set mutual relationship {char1ID}<->{char2ID} = {value}");
            }
            else if (operation == '+')
            {
                int currentValue1 = characterManager.GetRelationship(char1ID, char2ID);
                int newValue1 = currentValue1 + value;
                characterManager.SetRelationship(char1ID, char2ID, newValue1);
                
                int currentValue2 = characterManager.GetRelationship(char2ID, char1ID);
                int newValue2 = currentValue2 + value;
                characterManager.SetRelationship(char2ID, char1ID, newValue2);
                
                Debug.Log($"Modified mutual relationship {char1ID}<->{char2ID}: {currentValue1}/{currentValue2} + {value} = {newValue1}/{newValue2}");
            }
            else if (operation == '-')
            {
                int currentValue1 = characterManager.GetRelationship(char1ID, char2ID);
                int newValue1 = currentValue1 - value;
                characterManager.SetRelationship(char1ID, char2ID, newValue1);
                
                int currentValue2 = characterManager.GetRelationship(char2ID, char1ID);
                int newValue2 = currentValue2 - value;
                characterManager.SetRelationship(char2ID, char1ID, newValue2);
                
                Debug.Log($"Modified mutual relationship {char1ID}<->{char2ID}: {currentValue1}/{currentValue2} - {value} = {newValue1}/{newValue2}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing mutual relationship command '{command}': {e.Message}");
        }
    }
    
    private void ProcessArcCommand(string command)
    {
        try
        {
            Debug.Log($"Processing arc command: '{command}'");
            
            // Parse format: "Waif.arc +1" or "Waif.arc = 3"
            string[] parts = command.Split(new string[] { ".arc" }, System.StringSplitOptions.None);
            string charName = parts[0].Trim();
            
            string rightPart = parts[1].Trim();
            char operation = rightPart[0];
            
            if (operation == '+' || operation == '-' || operation == '=')
            {
                int value = 0;
                
                if (operation == '=')
                {
                    value = int.Parse(rightPart.Substring(1).Trim());
                }
                else
                {
                    value = int.Parse(rightPart.Substring(1).Trim());
                    if (operation == '-')
                        value = -value;
                }
                
                CharacterManager.CharacterID charID = GetCharacterID(charName);
                
                if (operation == '=')
                {
                    characterManager.SetCharacterArc(charID, value);
                    Debug.Log($"Set arc {charID} = {value}");
                }
                else
                {
                    int currentValue = characterManager.GetCharacterArc(charID);
                    int newValue = currentValue + value;
                    characterManager.SetCharacterArc(charID, newValue);
                    Debug.Log($"Modified arc {charID}: {currentValue} + {value} = {newValue}");
                }
            }
            else
            {
                Debug.LogWarning($"Unknown arc operation: {operation}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing arc command '{command}': {e.Message}");
        }
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
    

    
    // Methods for selection phase sprite management
    public void SetCharacterToEyesClosed(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < characterPortraits.Length && 
            characterPortraits[characterIndex] != null &&
            characterIndex < characterSpritesEyesClosed.Length &&
            characterSpritesEyesClosed[characterIndex] != null)
        {
            characterPortraits[characterIndex].sprite = characterSpritesEyesClosed[characterIndex];
        }
    }
    
    public void SetCharacterToEyesOpen(int characterIndex)
    {
        if (characterIndex >= 0 && characterIndex < characterPortraits.Length && 
            characterPortraits[characterIndex] != null &&
            characterIndex < characterSpritesEyesOpen.Length &&
            characterSpritesEyesOpen[characterIndex] != null)
        {
            characterPortraits[characterIndex].sprite = characterSpritesEyesOpen[characterIndex];
        }
    }
    
    public void SetAllCharactersToEyesClosed()
    {
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            SetCharacterToEyesClosed(i);
        }
    }
    
    public void ResetCharacterSpritesToNormal()
    {
        // Reset all character portraits to their normal dialog sprites (inactive by default)
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (i < characterSpritesInactive.Length && characterSpritesInactive[i] != null &&
                characterPortraits[i] != null)
            {
                characterPortraits[i].sprite = characterSpritesInactive[i];
            }
        }
    }
    
    // Conditional dialog system
    private bool EvaluateCondition(string conditionLine)
    {
        Debug.Log($"Evaluating condition: '{conditionLine}'");
        
        if (characterManager == null)
        {
            Debug.LogWarning("CharacterManager not assigned, condition evaluation failed");
            return false;
        }
        
        // Remove the condition markers and trim
        string condition = conditionLine.Replace("{IF:", "").Replace("}", "").Trim();
        Debug.Log($"Parsed condition: '{condition}'");
        
        // Parse relationship conditions: "Waif->Priestess >= 3"
        if (condition.Contains("->"))
        {
            Debug.Log("Detected relationship condition");
            return EvaluateRelationshipCondition(condition);
        }
        // Parse arc conditions: "Waif.arc >= 2"
        else if (condition.Contains(".arc"))
        {
            Debug.Log("Detected arc condition");
            return EvaluateArcCondition(condition);
        }
        
        Debug.LogWarning($"Unknown condition format: {condition}");
        return false;
    }
    
    private bool EvaluateRelationshipCondition(string condition)
    {
        try
        {
            Debug.Log($"Parsing relationship condition: '{condition}'");
            
            // Parse format: "Waif->Priestess >= 3"
            string[] parts = condition.Split(new string[] { "->" }, System.StringSplitOptions.None);
            string fromChar = parts[0].Trim();
            Debug.Log($"From character: '{fromChar}'");
            
            string rightPart = parts[1].Trim(); // "Priestess >= 3"
            Debug.Log($"Right part: '{rightPart}'");
            
            // Find the operator and extract character name and value
            string op = "";
            string toChar = "";
            int targetValue = 0;
            
            if (rightPart.Contains(" >="))
            {
                op = ">=";
                string[] opParts = rightPart.Split(new string[] { " >=" }, System.StringSplitOptions.None);
                toChar = opParts[0].Trim();
                targetValue = int.Parse(opParts[1].Trim());
            }
            else if (rightPart.Contains(" <="))
            {
                op = "<=";
                string[] opParts = rightPart.Split(new string[] { " <=" }, System.StringSplitOptions.None);
                toChar = opParts[0].Trim();
                targetValue = int.Parse(opParts[1].Trim());
            }
            else if (rightPart.Contains(" =="))
            {
                op = "==";
                string[] opParts = rightPart.Split(new string[] { " ==" }, System.StringSplitOptions.None);
                toChar = opParts[0].Trim();
                targetValue = int.Parse(opParts[1].Trim());
            }
            else if (rightPart.Contains(" !="))
            {
                op = "!=";
                string[] opParts = rightPart.Split(new string[] { " !=" }, System.StringSplitOptions.None);
                toChar = opParts[0].Trim();
                targetValue = int.Parse(opParts[1].Trim());
            }
            else if (rightPart.Contains(" >"))
            {
                op = ">";
                string[] opParts = rightPart.Split(new string[] { " >" }, System.StringSplitOptions.None);
                toChar = opParts[0].Trim();
                targetValue = int.Parse(opParts[1].Trim());
            }
            else if (rightPart.Contains(" <"))
            {
                op = "<";
                string[] opParts = rightPart.Split(new string[] { " <" }, System.StringSplitOptions.None);
                toChar = opParts[0].Trim();
                targetValue = int.Parse(opParts[1].Trim());
            }
            else
            {
                Debug.LogWarning($"Unknown operator in relationship condition: {rightPart}");
                return false;
            }
            
            Debug.Log($"To character: '{toChar}', Operator: '{op}', Target value: {targetValue}");
            
            // Convert character names to IDs
            CharacterManager.CharacterID fromID = GetCharacterID(fromChar);
            CharacterManager.CharacterID toID = GetCharacterID(toChar);
            Debug.Log($"Character IDs: {fromID} -> {toID}");
            
            int currentValue = characterManager.GetRelationship(fromID, toID);
            Debug.Log($"Current relationship value: {currentValue}");
            
            bool result = EvaluateComparison(currentValue, op, targetValue);
            Debug.Log($"Comparison result: {currentValue} {op} {targetValue} = {result}");
            
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing relationship condition '{condition}': {e.Message}");
            return false;
        }
    }
    
    private bool EvaluateArcCondition(string condition)
    {
        try
        {
            Debug.Log($"Parsing arc condition: '{condition}'");
            
            // Parse format: "Waif.arc >= 2"
            string[] parts = condition.Split(new string[] { ".arc" }, System.StringSplitOptions.None);
            string charName = parts[0].Trim();
            Debug.Log($"Character: '{charName}'");
            
            string rightPart = parts[1].Trim(); // " >= 2"
            Debug.Log($"Right part: '{rightPart}'");
            
            // Find the operator and value
            string op = "";
            int targetValue = 0;
            
            if (rightPart.StartsWith(">="))
            {
                op = ">=";
                targetValue = int.Parse(rightPart.Substring(2).Trim());
            }
            else if (rightPart.StartsWith("<="))
            {
                op = "<=";
                targetValue = int.Parse(rightPart.Substring(2).Trim());
            }
            else if (rightPart.StartsWith("=="))
            {
                op = "==";
                targetValue = int.Parse(rightPart.Substring(2).Trim());
            }
            else if (rightPart.StartsWith("!="))
            {
                op = "!=";
                targetValue = int.Parse(rightPart.Substring(2).Trim());
            }
            else if (rightPart.StartsWith(">"))
            {
                op = ">";
                targetValue = int.Parse(rightPart.Substring(1).Trim());
            }
            else if (rightPart.StartsWith("<"))
            {
                op = "<";
                targetValue = int.Parse(rightPart.Substring(1).Trim());
            }
            else
            {
                Debug.LogWarning($"Unknown operator in arc condition: {rightPart}");
                return false;
            }
            
            Debug.Log($"Operator: '{op}', Target value: {targetValue}");
            
            // Convert character name to ID
            CharacterManager.CharacterID charID = GetCharacterID(charName);
            Debug.Log($"Character ID: {charID}");
            
            int currentValue = characterManager.GetCharacterArc(charID);
            Debug.Log($"Current arc value: {currentValue}");
            
            bool result = EvaluateComparison(currentValue, op, targetValue);
            Debug.Log($"Comparison result: {currentValue} {op} {targetValue} = {result}");
            
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing arc condition '{condition}': {e.Message}");
            return false;
        }
    }
    
    private bool EvaluateComparison(int currentValue, string op, int targetValue)
    {
        switch (op)
        {
            case ">=": return currentValue >= targetValue;
            case "<=": return currentValue <= targetValue;
            case ">": return currentValue > targetValue;
            case "<": return currentValue < targetValue;
            case "==": return currentValue == targetValue;
            case "!=": return currentValue != targetValue;
            default:
                Debug.LogWarning($"Unknown operator: {op}");
                return false;
        }
    }
    
    private CharacterManager.CharacterID GetCharacterID(string characterName)
    {
        switch (characterName.ToLower())
        {
            case "waif": return CharacterManager.CharacterID.Waif;
            case "priestess": return CharacterManager.CharacterID.Priestess;
            case "warder": return CharacterManager.CharacterID.Warder;
            case "pilot": return CharacterManager.CharacterID.Pilot;
            default:
                Debug.LogError($"Unknown character name: {characterName}");
                return CharacterManager.CharacterID.Waif; // Default fallback
        }
    }
}
