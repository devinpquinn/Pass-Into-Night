
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
    public RectTransform dialogPanel;
    private float shrinkAmount = 0.967f;
    private Vector3 dialogPanelOriginalScale;
    private bool isDialogPanelPunching = false;
    private float dialogPanelPunchTimer = 0f;
    private Camera uiCamera;
    public TextMeshProUGUI dialogText;
    public GameObject speechBubble;

    public Color textSpoken;
    public Color textUnspoken;

    public Image[] characterPortraits;
    public Sprite[] characterSpritesInactive;
    public Sprite[] characterSpritesActive;

    private float activePortraitScale = 1.125f;
    private float portraitScaleTime = 0.1f;

    private Queue<string> dialogQueue = new Queue<string>();
    private int currentSpeaker = -1;
    private int previousSpeaker = -1;

    private bool isScaling = false;
    private float[] portraitScaleTimers;
    private float[] portraitStartScales;
    private float[] portraitTargetScales;

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
        if (dialogPanel != null)
        {
            dialogPanelOriginalScale = dialogPanel.localScale;
        }
        uiCamera = Camera.main;
        // Debug dialog lines
        dialogQueue.Enqueue("Waif: Hello, I am Waif.");
        dialogQueue.Enqueue("Interesting. Here is some descriptive text.");
        dialogQueue.Enqueue("Priestess: Now Priestess is speaking.");
        dialogQueue.Enqueue("Warder: And now Warder is speaking.");
        dialogQueue.Enqueue("Warder: I am still speaking.");
        dialogQueue.Enqueue("Pilot waits patiently.");
        dialogQueue.Enqueue("Pilot: Ok, now it's my turn!");

        portraitScaleTimers = new float[characterPortraits.Length];
        portraitStartScales = new float[characterPortraits.Length];
        portraitTargetScales = new float[characterPortraits.Length];
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            if (characterPortraits[i].transform.parent != null)
                characterPortraits[i].transform.parent.localScale = Vector3.one;
            portraitScaleTimers[i] = 0f;
            portraitStartScales[i] = 1f;
            portraitTargetScales[i] = 1f;
        }

        ShowNextDialog();
    }

    void Update()
    {
        // Handle dialog panel punch effect (scale)
        if (isDialogPanelPunching && dialogPanel != null)
        {
            dialogPanelPunchTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dialogPanelPunchTimer / portraitScaleTime);
            float punchT = Mathf.Sin(t * Mathf.PI * 0.5f);
            dialogPanel.localScale = Vector3.Lerp(dialogPanelOriginalScale * shrinkAmount, dialogPanelOriginalScale, punchT);
            if (t >= 1f)
            {
                dialogPanel.localScale = dialogPanelOriginalScale;
                isDialogPanelPunching = false;
            }
        }
        // Handle portrait scaling
        if (isScaling)
        {
            bool allDone = true;
            for (int i = 0; i < characterPortraits.Length; i++)
            {
                if (portraitScaleTimers[i] < portraitScaleTime)
                {
                    portraitScaleTimers[i] += Time.deltaTime;
                    float t = Mathf.Clamp01(portraitScaleTimers[i] / portraitScaleTime);
                    float scale = Mathf.Lerp(portraitStartScales[i], portraitTargetScales[i], t);
                    if (characterPortraits[i].transform.parent != null)
                        characterPortraits[i].transform.parent.localScale = new Vector3(scale, scale, 1f);
                    if (t < 1f) allDone = false;
                }
            }
            if (allDone)
            {
                isScaling = false;
            }
        }

        bool advance = false;
        if (!isScaling && Input.GetKeyDown(KeyCode.Space))
        {
            advance = true;
        }
        else if (!isScaling && Input.GetMouseButtonDown(0))
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
        // Start punch effect on dialog panel (scale)
        if (dialogPanel != null)
        {
            dialogPanel.localScale = dialogPanelOriginalScale * shrinkAmount;
            dialogPanelPunchTimer = 0f;
            isDialogPanelPunching = true;
        }
        if (dialogQueue.Count == 0)
        {
            dialogText.text = "";
            HighlightSpeaker(-1);
            speechBubble.SetActive(false);
            // Smoothly scale last speaker's portrait parent back to 1
            StartPortraitScale(currentSpeaker, -1);
            currentSpeaker = -1;
            return;
        }

        string line = dialogQueue.Dequeue();
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
                if (speechBubble != null) speechBubble.SetActive(true);
                StartPortraitScale(previousSpeaker, currentSpeaker);
            }
            else
            {
                // Unknown speaker: treat as unspoken text
                dialogText.text = line;
                dialogText.color = textUnspoken;
                HighlightSpeaker(-1);
                if (speechBubble != null) speechBubble.SetActive(false);
                StartPortraitScale(currentSpeaker, -1);
            }
        }
        else
        {
            // Descriptive/unspoken text
            dialogText.text = line;
            dialogText.color = textUnspoken;
            HighlightSpeaker(-1);
            if (speechBubble != null) speechBubble.SetActive(false);
            StartPortraitScale(currentSpeaker, -1);
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

    void StartPortraitScale(int prev, int curr)
    {
        // Reset all timers and set targets
        for (int i = 0; i < characterPortraits.Length; i++)
        {
            float currentScale = 1f;
            if (characterPortraits[i].transform.parent != null)
                currentScale = characterPortraits[i].transform.parent.localScale.x;
            portraitStartScales[i] = currentScale;
            if (i == curr)
            {
                portraitTargetScales[i] = activePortraitScale;
                portraitScaleTimers[i] = 0f;
            }
            else
            {
                portraitTargetScales[i] = 1f;
                portraitScaleTimers[i] = 0f;
            }
        }
        isScaling = true;
    }
}
