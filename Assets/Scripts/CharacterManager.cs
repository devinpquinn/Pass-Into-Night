using UnityEngine;
using System;

public class CharacterManager : MonoBehaviour
{
    [Header("Character Arc Progress")]
    [SerializeField] private int[] characterArcs = new int[4]; // Internal growth for each character (Waif, Priestess, Warder, Pilot)
    
    // Character indices
    public enum CharacterID
    {
        Waif = 0,
        Priestess = 1,
        Warder = 2,
        Pilot = 3
    }
    
    void Start()
    {
        // Character arcs are initialized to 0 by default
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
    
    // Utility Methods
    
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
