# Command Execution Timing Implementation

## Overview
Modified the dialog system to execute relationship and arc commands when dialog lines are actually displayed, rather than preprocessing all commands at the start of conversations.

## Changes Made

### 1. ProcessConversationWithConditions() Method
**Before**: Commands were executed immediately during conversation preprocessing
```csharp
if (line.StartsWith("{") && line.EndsWith("}") && line.Contains(":"))
{
    ProcessCommand(line); // Executed immediately
}
```

**After**: Commands are preserved in the conversation queue for later execution
```csharp
if (line.StartsWith("{") && line.EndsWith("}") && line.Contains(":"))
{
    processedConversation.Add(line); // Preserved for later
}
```

### 2. ShowNextDialog() Method
**New**: Added command detection and execution during dialog display
```csharp
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
        // ... end conversation logic ...
    }
}
```

## Benefits

1. **Natural Progression**: Relationship and arc changes occur as players read through conversations, making the progression feel more organic
2. **Immediate Feedback**: Commands execute right when the relevant dialog is displayed
3. **Better User Experience**: Players can see the exact moment when relationships change

## Command Types Supported
- Arc modifications: `{SET: Character.arc +1}`
- Direct relationship changes: `{SET: Character1->Character2 +1}`
- Mutual relationship changes: `{SET: Character1<->Character2 +1}`

## Testing
The system now processes conversations from `Assets/Resources/Events/Conversations.txt` and executes commands inline during dialog display. Commands are logged for debugging purposes.

## Flow Example
1. Player selects a character
2. Conversation loads and conditions are evaluated
3. Dialog displays line by line
4. When a command line is encountered:
   - Command executes immediately
   - Command line is not displayed to player
   - Next dialog line displays automatically
5. Relationship/arc values are updated in real-time