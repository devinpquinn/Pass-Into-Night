using UnityEngine;
using System;

public class CharacterManager : MonoBehaviour
{
    [Header("Character Arc Progress")]
    [SerializeField] private int[] characterArcs = new int[4]; // Internal growth for each character (0-3)
    
    [Header("Relationship Matrix")]
    [SerializeField] private int[,] relationships = new int[4, 4]; // Relationships between characters
    
    [Header("Relationship Matrix (Inspector View)")]
    [SerializeField] private int[] relationshipMatrix = new int[16]; // Flattened for Inspector viewing
    [Space(10)]
    [SerializeField] private bool showRelationshipLabels = true;
    
    // Character indices
    public enum CharacterID
    {
        Character0 = 0,
        Character1 = 1,
        Character2 = 2,
        Character3 = 3
    }
    
    void Start()
    {
        InitializeRelationships();
        SyncMatrixToArray();
    }
    
    void OnValidate()
    {
        // Sync changes from Inspector back to the matrix
        if (relationshipMatrix != null && relationshipMatrix.Length == 16)
        {
            SyncArrayToMatrix();
        }
    }
    
    void InitializeRelationships()
    {
        // Initialize all relationships to 0 (strangers)
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                relationships[i, j] = 0;
            }
        }
        
        // A character's relationship with themselves is always neutral/not applicable
        for (int i = 0; i < 4; i++)
        {
            relationships[i, i] = 0;
        }
    }
    
    // Sync methods for Inspector visualization
    void SyncMatrixToArray()
    {
        if (relationshipMatrix == null || relationshipMatrix.Length != 16)
            relationshipMatrix = new int[16];
            
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                relationshipMatrix[i * 4 + j] = relationships[i, j];
            }
        }
    }
    
    void SyncArrayToMatrix()
    {
        if (relationships == null)
            relationships = new int[4, 4];
            
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                relationships[i, j] = relationshipMatrix[i * 4 + j];
            }
        }
    }
    
    // Character Arc Methods
    public int GetCharacterArc(CharacterID character)
    {
        return characterArcs[(int)character];
    }
    
    public void SetCharacterArc(CharacterID character, int progress)
    {
        characterArcs[(int)character] = progress;
        Debug.Log($"{character} arc progress set to: {progress}");
    }
    
    public void ModifyCharacterArc(CharacterID character, int change)
    {
        characterArcs[(int)character] += change;
        Debug.Log($"{character} arc progress changed by {change}, now: {characterArcs[(int)character]}");
    }
    
    // Relationship Methods
    public int GetRelationship(CharacterID from, CharacterID to)
    {
        return relationships[(int)from, (int)to];
    }
    
    public void SetRelationship(CharacterID from, CharacterID to, int value)
    {
        relationships[(int)from, (int)to] = value;
        SyncMatrixToArray(); // Update Inspector view
        Debug.Log($"{from}'s relationship toward {to} set to: {value}");
    }
    
    public void ModifyRelationship(CharacterID from, CharacterID to, int change)
    {
        relationships[(int)from, (int)to] += change;
        SyncMatrixToArray(); // Update Inspector view
        Debug.Log($"{from}'s relationship toward {to} changed by {change}, now: {relationships[(int)from, (int)to]}");
    }
    
    // Utility Methods
    public string GetRelationshipDescription(int relationshipValue)
    {
        if (relationshipValue == 0) return "Stranger";
        else if (relationshipValue > 0 && relationshipValue <= 3) return "Acquaintance";
        else if (relationshipValue > 3 && relationshipValue <= 6) return "Friend";
        else if (relationshipValue > 6) return "Close Friend";
        else if (relationshipValue < 0 && relationshipValue >= -3) return "Dislike";
        else if (relationshipValue < -3 && relationshipValue >= -6) return "Hostile";
        else if (relationshipValue < -6) return "Enemy";
        return "Unknown";
    }
    
    public void PrintAllRelationships()
    {
        Debug.Log("=== Character Relationships ===");
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (i != j)
                {
                    CharacterID from = (CharacterID)i;
                    CharacterID to = (CharacterID)j;
                    int value = relationships[i, j];
                    string description = GetRelationshipDescription(value);
                    Debug.Log($"{from} -> {to}: {value} ({description})");
                }
            }
        }
    }
    
    public void PrintAllCharacterArcs()
    {
        Debug.Log("=== Character Arc Progress ===");
        for (int i = 0; i < 4; i++)
        {
            CharacterID character = (CharacterID)i;
            Debug.Log($"{character}: {characterArcs[i]}");
        }
    }
}
