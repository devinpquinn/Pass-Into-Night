using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ConversationDatabase", menuName = "Pass Into Night/Conversation Database")]
public class ConversationDatabase : ScriptableObject
{
    [Header("Conversation Pool")]
    [SerializeField] private List<Conversation> allConversations = new List<Conversation>();
    
    [Header("Runtime State")]
    [SerializeField] private List<Conversation> usedConversations = new List<Conversation>();
    [SerializeField] private List<Conversation> availableConversations = new List<Conversation>();
    
    [Header("Selection Settings")]
    [SerializeField] private bool prioritizeHigherPriority = true;
    [SerializeField] private bool resetPoolWhenEmpty = true;
    
    public List<Conversation> AllConversations => allConversations;
    public List<Conversation> UsedConversations => usedConversations;
    public List<Conversation> AvailableConversations => availableConversations;
    public int TotalConversations => allConversations.Count;
    public int RemainingConversations => availableConversations.Count;
    
    private void OnEnable()
    {
        RefreshAvailableConversations();
    }
    
    /// <summary>
    /// Refreshes the available conversations list based on current state
    /// </summary>
    public void RefreshAvailableConversations()
    {
        availableConversations.Clear();
        
        foreach (var conversation in allConversations)
        {
            if (conversation != null && conversation.IsAvailable && !usedConversations.Contains(conversation))
            {
                availableConversations.Add(conversation);
            }
        }
        
        // Sort by priority if enabled
        if (prioritizeHigherPriority)
        {
            availableConversations = availableConversations.OrderByDescending(c => c.Priority).ToList();
        }
    }
    
    /// <summary>
    /// Selects the next conversation from the available pool
    /// </summary>
    public Conversation GetNextConversation()
    {
        RefreshAvailableConversations();
        
        if (availableConversations.Count == 0)
        {
            if (resetPoolWhenEmpty && usedConversations.Count > 0)
            {
                Debug.Log("Conversation pool exhausted. Resetting pool.");
                ResetPool();
                RefreshAvailableConversations();
            }
            
            if (availableConversations.Count == 0)
            {
                Debug.LogWarning("No conversations available!");
                return null;
            }
        }
        
        // Select conversation based on priority or random selection
        Conversation selectedConversation = null;
        
        if (prioritizeHigherPriority)
        {
            // Get all conversations with the highest priority
            int highestPriority = availableConversations[0].Priority;
            var highestPriorityConversations = availableConversations
                .Where(c => c.Priority == highestPriority)
                .ToList();
            
            // Random selection from highest priority conversations
            selectedConversation = highestPriorityConversations[Random.Range(0, highestPriorityConversations.Count)];
        }
        else
        {
            // Pure random selection
            selectedConversation = availableConversations[Random.Range(0, availableConversations.Count)];
        }
        
        // Mark as used
        MarkConversationAsUsed(selectedConversation);
        
        Debug.Log($"Selected conversation: {selectedConversation.ConversationName} (Participants: {selectedConversation.ParticipantCount})");
        return selectedConversation;
    }
    
    /// <summary>
    /// Gets conversations filtered by participant count
    /// </summary>
    public List<Conversation> GetConversationsByParticipantCount(int participantCount)
    {
        RefreshAvailableConversations();
        return availableConversations.Where(c => c.ParticipantCount == participantCount).ToList();
    }
    
    /// <summary>
    /// Gets the next conversation with a specific participant count
    /// </summary>
    public Conversation GetNextConversationByParticipantCount(int participantCount)
    {
        var filteredConversations = GetConversationsByParticipantCount(participantCount);
        
        if (filteredConversations.Count == 0)
        {
            Debug.LogWarning($"No available conversations for {participantCount} participants!");
            return null;
        }
        
        // Select from filtered list using same priority logic
        Conversation selectedConversation = null;
        
        if (prioritizeHigherPriority)
        {
            int highestPriority = filteredConversations.Max(c => c.Priority);
            var highestPriorityConversations = filteredConversations
                .Where(c => c.Priority == highestPriority)
                .ToList();
            
            selectedConversation = highestPriorityConversations[Random.Range(0, highestPriorityConversations.Count)];
        }
        else
        {
            selectedConversation = filteredConversations[Random.Range(0, filteredConversations.Count)];
        }
        
        MarkConversationAsUsed(selectedConversation);
        
        Debug.Log($"Selected conversation: {selectedConversation.ConversationName} (Participants: {selectedConversation.ParticipantCount})");
        return selectedConversation;
    }
    
    /// <summary>
    /// Marks a conversation as used
    /// </summary>
    public void MarkConversationAsUsed(Conversation conversation)
    {
        if (conversation != null && !usedConversations.Contains(conversation))
        {
            usedConversations.Add(conversation);
            RefreshAvailableConversations();
        }
    }
    
    /// <summary>
    /// Resets the conversation pool, making all conversations available again
    /// </summary>
    public void ResetPool()
    {
        usedConversations.Clear();
        RefreshAvailableConversations();
        Debug.Log("Conversation pool reset. All conversations are now available.");
    }
    
    /// <summary>
    /// Adds a conversation to the database
    /// </summary>
    public void AddConversation(Conversation conversation)
    {
        if (conversation != null && !allConversations.Contains(conversation))
        {
            allConversations.Add(conversation);
            RefreshAvailableConversations();
        }
    }
    
    /// <summary>
    /// Removes a conversation from the database
    /// </summary>
    public void RemoveConversation(Conversation conversation)
    {
        if (conversation != null)
        {
            allConversations.Remove(conversation);
            usedConversations.Remove(conversation);
            RefreshAvailableConversations();
        }
    }
    
    /// <summary>
    /// Gets statistics about the conversation database
    /// </summary>
    public string GetDatabaseStats()
    {
        RefreshAvailableConversations();
        
        var stats = $"Total Conversations: {allConversations.Count}\n";
        stats += $"Available: {availableConversations.Count}\n";
        stats += $"Used: {usedConversations.Count}\n";
        
        // Breakdown by participant count
        for (int i = 1; i <= 4; i++)
        {
            int total = allConversations.Count(c => c != null && c.ParticipantCount == i);
            int available = availableConversations.Count(c => c.ParticipantCount == i);
            int used = usedConversations.Count(c => c.ParticipantCount == i);
            
            if (total > 0)
            {
                stats += $"{i} Participant{(i > 1 ? "s" : "")}: {available}/{total} available ({used} used)\n";
            }
        }
        
        return stats;
    }
    
    private void OnValidate()
    {
        // Remove null entries
        allConversations.RemoveAll(c => c == null);
        usedConversations.RemoveAll(c => c == null);
        
        // Refresh when modified in inspector
        RefreshAvailableConversations();
    }
}