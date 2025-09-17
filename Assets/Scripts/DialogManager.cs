using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
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

    void Start()
    {
        // Find the parent Canvas and cache its worldCamera (if any)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            uiCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        }
        // Debug dialog lines
        dialogQueue.Enqueue("0: Hello, I am character 0.");
        dialogQueue.Enqueue("Interesting. Here is some descriptive text.");
        dialogQueue.Enqueue("1: Now character 1 is speaking.");
        dialogQueue.Enqueue("2: And now character 2 is speaking.");
        dialogQueue.Enqueue("Character 3 waits patiently.");
        dialogQueue.Enqueue("3: Ok, now it's my turn!");

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
            // Check if mouse is over this object's RectTransform
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 mousePos = Input.mousePosition;
                if (RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, uiCamera))
                {
                    advance = true;
                }
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
            if (int.TryParse(speakerStr, out int speakerIndex))
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
                // Fallback: treat as unspoken text
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
