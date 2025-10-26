using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(Conversation))]
public class ConversationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Conversation conversation = (Conversation)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Conversation Analysis", EditorStyles.boldLabel);
        
        if (conversation.ConversationFile != null)
        {
            EditorGUILayout.BeginVertical("box");
            
            // Analyze the conversation file
            string content = conversation.ConversationFile.text;
            string[] lines = content.Split('\n');
            
            int sectionCount = 0;
            int maxFoundParticipants = 1;
            string[] participantCountLabels = { "", "Solo", "Duo", "Trio", "Quartet" };
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    sectionCount++;
                    string sectionName = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    int participantCount = sectionName.Split('-').Length;
                    maxFoundParticipants = Mathf.Max(maxFoundParticipants, participantCount);
                }
            }
            
            EditorGUILayout.LabelField($"File: {conversation.ConversationFile.name}");
            EditorGUILayout.LabelField($"Conversation Sections: {sectionCount}");
            EditorGUILayout.LabelField($"Max Participants Found: {maxFoundParticipants} ({(maxFoundParticipants <= 4 ? participantCountLabels[maxFoundParticipants] : "Unknown")})");
            
            if (conversation.ParticipantCount != maxFoundParticipants)
            {
                EditorGUILayout.HelpBox($"Warning: Participant count ({conversation.ParticipantCount}) doesn't match max found in file ({maxFoundParticipants})", MessageType.Warning);
                
                if (GUILayout.Button("Auto-Fix Participant Count"))
                {
                    // Use reflection to set the private field
                    var participantCountField = typeof(Conversation).GetField("participantCount", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    participantCountField?.SetValue(conversation, maxFoundParticipants);
                    EditorUtility.SetDirty(conversation);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("No conversation file assigned!", MessageType.Error);
        }
        
        EditorGUILayout.Space();
        
        // Quick actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Preview Text File"))
        {
            if (conversation.ConversationFile != null)
            {
                Selection.activeObject = conversation.ConversationFile;
            }
        }
        
        if (GUILayout.Button("Find in Database"))
        {
            // Find all ConversationDatabase assets that contain this conversation
            string[] guids = AssetDatabase.FindAssets("t:ConversationDatabase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ConversationDatabase database = AssetDatabase.LoadAssetAtPath<ConversationDatabase>(path);
                
                if (database != null && database.AllConversations.Contains(conversation))
                {
                    Selection.activeObject = database;
                    Debug.Log($"Found conversation in database: {database.name}");
                    break;
                }
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
}

[CustomEditor(typeof(ConversationDatabase))]
public class ConversationDatabaseEditor : Editor
{
    private ConversationDatabase database;
    private Vector2 scrollPosition;
    
    private void OnEnable()
    {
        database = (ConversationDatabase)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Database Management", EditorStyles.boldLabel);
        
        // Statistics
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Statistics:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(database.GetDatabaseStats(), EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Management buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Refresh Available"))
        {
            database.RefreshAvailableConversations();
            EditorUtility.SetDirty(database);
        }
        
        if (GUILayout.Button("Reset Pool"))
        {
            database.ResetPool();
            EditorUtility.SetDirty(database);
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Auto-Populate from Resources"))
        {
            AutoPopulateFromResources();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Test conversation selection
        EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Test Get Next"))
        {
            var next = database.GetNextConversation();
            if (next != null)
            {
                Debug.Log($"Selected: {next.ConversationName} ({next.ParticipantCount} participants)");
            }
        }
        
        for (int i = 1; i <= 4; i++)
        {
            if (GUILayout.Button($"Test {i}P"))
            {
                var next = database.GetNextConversationByParticipantCount(i);
                if (next != null)
                {
                    Debug.Log($"Selected {i}P: {next.ConversationName}");
                }
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Available conversations list
        if (database.AllConversations.Count > 0)
        {
            EditorGUILayout.LabelField("Conversations Overview", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            foreach (var conversation in database.AllConversations.Where(c => c != null))
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // Color code based on availability
                GUI.color = database.AvailableConversations.Contains(conversation) ? Color.green : 
                           database.UsedConversations.Contains(conversation) ? Color.yellow : Color.red;
                
                EditorGUILayout.LabelField($"[{conversation.ParticipantCount}P]", GUILayout.Width(40));
                EditorGUILayout.LabelField(conversation.ConversationName, GUILayout.ExpandWidth(true));
                
                GUI.color = Color.white;
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = conversation;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // Legend
            EditorGUILayout.BeginHorizontal();
            
            GUI.color = Color.green;
            EditorGUILayout.LabelField("■ Available", GUILayout.Width(80));
            
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("■ Used", GUILayout.Width(60));
            
            GUI.color = Color.red;
            EditorGUILayout.LabelField("■ Unavailable");
            
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void AutoPopulateFromResources()
    {
        // Find all TextAssets in Resources that could be conversations
        string[] guids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Resources" });
        
        int addedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            
            if (textAsset != null)
            {
                // Check if this text asset looks like a conversation file
                if (IsConversationFile(textAsset))
                {
                    // Create conversation scriptable object
                    string conversationPath = path.Replace(".txt", ".asset")
                                                 .Replace("Resources/", "");
                    
                    // Check if conversation already exists in database
                    bool alreadyExists = database.AllConversations.Any(c => 
                        c != null && c.ConversationFile == textAsset);
                    
                    if (!alreadyExists)
                    {
                        // Determine participant count from the file
                        int participantCount = DetermineParticipantCount(textAsset);
                        
                        // Create the conversation asset
                        Conversation newConversation = CreateInstance<Conversation>();
                        newConversation.name = textAsset.name;
                        
                        // Use reflection to set private fields
                        var conversationFileField = typeof(Conversation).GetField("conversationFile", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var participantCountField = typeof(Conversation).GetField("participantCount", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var conversationNameField = typeof(Conversation).GetField("conversationName", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var descriptionField = typeof(Conversation).GetField("description", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        conversationFileField?.SetValue(newConversation, textAsset);
                        participantCountField?.SetValue(newConversation, participantCount);
                        conversationNameField?.SetValue(newConversation, textAsset.name);
                        descriptionField?.SetValue(newConversation, $"Auto-generated from {textAsset.name}");
                        
                        // Save the asset
                        string assetPath = $"Assets/Conversations/{newConversation.name}.asset";
                        AssetDatabase.CreateAsset(newConversation, assetPath);
                        
                        // Add to database
                        database.AddConversation(newConversation);
                        addedCount++;
                    }
                }
            }
        }
        
        if (addedCount > 0)
        {
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(database);
            Debug.Log($"Auto-populated {addedCount} conversations from Resources folder.");
        }
        else
        {
            Debug.Log("No new conversation files found to add.");
        }
    }
    
    private bool IsConversationFile(TextAsset textAsset)
    {
        // Simple check: does the file contain conversation sections like [Character] or [Character-Character]?
        string content = textAsset.text;
        return content.Contains("[") && content.Contains("]") && content.Contains(":");
    }
    
    private int DetermineParticipantCount(TextAsset textAsset)
    {
        // Analyze the text to determine max participant count
        string content = textAsset.text;
        string[] lines = content.Split('\n');
        
        int maxParticipants = 1;
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            
            // Look for section headers like [Waif-Priestess-Warder]
            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                string sectionName = trimmedLine.Substring(1, trimmedLine.Length - 2);
                int participantCount = sectionName.Split('-').Length;
                maxParticipants = Mathf.Max(maxParticipants, participantCount);
            }
        }
        
        return maxParticipants;
    }
}

// Menu items for easy setup
public class ConversationSystemMenu
{
    [MenuItem("Pass Into Night/Create Conversation Database")]
    public static void CreateConversationDatabase()
    {
        ConversationDatabase database = ScriptableObject.CreateInstance<ConversationDatabase>();
        database.name = "ConversationDatabase";
        
        string path = "Assets/ConversationDatabase.asset";
        AssetDatabase.CreateAsset(database, path);
        AssetDatabase.SaveAssets();
        
        Selection.activeObject = database;
        Debug.Log("Created ConversationDatabase asset. Don't forget to assign it to your DialogManager!");
    }
    
    [MenuItem("Pass Into Night/Create Conversation from Text File")]
    public static void CreateConversationFromTextFile()
    {
        string path = EditorUtility.OpenFilePanel("Select Conversation Text File", "Assets/Resources", "txt");
        
        if (!string.IsNullOrEmpty(path))
        {
            // Convert absolute path to relative path
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }
            
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            
            if (textAsset != null)
            {
                Conversation conversation = ScriptableObject.CreateInstance<Conversation>();
                conversation.name = textAsset.name;
                
                // Use reflection to set private fields
                var conversationFileField = typeof(Conversation).GetField("conversationFile", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var conversationNameField = typeof(Conversation).GetField("conversationName", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                conversationFileField?.SetValue(conversation, textAsset);
                conversationNameField?.SetValue(conversation, textAsset.name);
                
                string assetPath = path.Replace(".txt", ".asset");
                AssetDatabase.CreateAsset(conversation, assetPath);
                AssetDatabase.SaveAssets();
                
                Selection.activeObject = conversation;
                Debug.Log($"Created Conversation asset: {conversation.name}");
            }
            else
            {
                Debug.LogError("Could not load text asset from path: " + path);
            }
        }
    }
    
    [MenuItem("Pass Into Night/Setup Conversation System")]
    public static void SetupConversationSystem()
    {
        // Find DialogManager in scene
        DialogManager dialogManager = Object.FindFirstObjectByType<DialogManager>();
        
        if (dialogManager == null)
        {
            Debug.LogError("No DialogManager found in scene!");
            return;
        }
        
        // Check if database is already assigned
        if (dialogManager.conversationDatabase != null)
        {
            Debug.Log("DialogManager already has a conversation database assigned.");
            Selection.activeObject = dialogManager.conversationDatabase;
            return;
        }
        
        // Try to find existing database
        string[] guids = AssetDatabase.FindAssets("t:ConversationDatabase");
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            ConversationDatabase database = AssetDatabase.LoadAssetAtPath<ConversationDatabase>(path);
            
            // Assign to dialog manager using reflection
            var databaseField = typeof(DialogManager).GetField("conversationDatabase", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            databaseField?.SetValue(dialogManager, database);
            
            EditorUtility.SetDirty(dialogManager);
            
            Debug.Log($"Assigned existing ConversationDatabase ({database.name}) to DialogManager.");
            Selection.activeObject = database;
        }
        else
        {
            Debug.Log("No ConversationDatabase found. Creating one...");
            CreateConversationDatabase();
        }
    }
}