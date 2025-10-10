using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CharacterManager))]
public class CharacterManagerEditor : Editor
{
    private SerializedProperty characterArcs;
    private SerializedProperty relationshipMatrix;
    private SerializedProperty showRelationshipLabels;
    
    private string[] characterNames = { "Waif", "Priestess", "Warder", "Pilot" };
    
    void OnEnable()
    {
        characterArcs = serializedObject.FindProperty("characterArcs");
        relationshipMatrix = serializedObject.FindProperty("relationshipMatrix");
        showRelationshipLabels = serializedObject.FindProperty("showRelationshipLabels");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Character Arc Progress Section
        EditorGUILayout.LabelField("Character Arc Progress", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        for (int i = 0; i < 4; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(characterNames[i], GUILayout.Width(100));
            characterArcs.GetArrayElementAtIndex(i).intValue = EditorGUILayout.IntField(characterArcs.GetArrayElementAtIndex(i).intValue);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(15);
        
        // Relationship Matrix Section
        EditorGUILayout.LabelField("Relationship Matrix", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showRelationshipLabels);
        EditorGUILayout.Space(5);
        
        // Draw relationship matrix in a grid format
        for (int from = 0; from < 4; from++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{characterNames[from]}'s relationships:", EditorStyles.boldLabel);
            
            for (int to = 0; to < 4; to++)
            {
                if (from != to) // Skip self-relationships
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"→ {characterNames[to]}", GUILayout.Width(120));
                    
                    int index = from * 4 + to;
                    int oldValue = relationshipMatrix.GetArrayElementAtIndex(index).intValue;
                    int newValue = EditorGUILayout.IntField(oldValue, GUILayout.Width(50));
                    relationshipMatrix.GetArrayElementAtIndex(index).intValue = newValue;
                    
                    if (showRelationshipLabels.boolValue)
                    {
                        string description = GetRelationshipDescription(newValue);
                        EditorGUILayout.LabelField($"({description})", GUILayout.Width(100));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        EditorGUILayout.Space(10);
        
        // Debug buttons
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Print All Relationships"))
        {
            ((CharacterManager)target).PrintAllRelationships();
        }
        if (GUILayout.Button("Print Character Arcs"))
        {
            ((CharacterManager)target).PrintAllCharacterArcs();
        }
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private string GetRelationshipDescription(int relationshipValue)
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
}