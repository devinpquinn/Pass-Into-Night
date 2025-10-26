using UnityEngine;

[CreateAssetMenu(fileName = "New Conversation", menuName = "Pass Into Night/Conversation")]
public class Conversation : ScriptableObject
{
    [Header("Conversation Settings")]
    [SerializeField] private TextAsset conversationFile;
    [SerializeField] private int participantCount = 1;
    [SerializeField] private string conversationName;
    [SerializeField, TextArea(3, 5)] private string description;
    
    [Header("Metadata")]
    [SerializeField] private bool isAvailable = true;
    
    public TextAsset ConversationFile => conversationFile;
    public int ParticipantCount => participantCount;
    public string ConversationName => conversationName;
    public string Description => description;
    public bool IsAvailable => isAvailable;
    
    public void SetAvailable(bool available)
    {
        isAvailable = available;
    }
    
    private void OnValidate()
    {
        // Ensure participant count is within valid range
        participantCount = Mathf.Clamp(participantCount, 1, 4);
        
        // Auto-generate conversation name from file name if not set
        if (string.IsNullOrEmpty(conversationName) && conversationFile != null)
        {
            conversationName = conversationFile.name;
        }
    }
}