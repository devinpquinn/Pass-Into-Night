using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CharacterManager))]
public class CharacterManagerEditor : Editor
{
    private SerializedProperty characterArcs;
    
    private string[] characterNames = { "Waif", "Priestess", "Warder", "Pilot" };
    
    void OnEnable()
    {
        characterArcs = serializedObject.FindProperty("characterArcs");
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
        
        // Debug buttons
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
        if (GUILayout.Button("Print Character Arcs"))
        {
            ((CharacterManager)target).PrintAllCharacterArcs();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}