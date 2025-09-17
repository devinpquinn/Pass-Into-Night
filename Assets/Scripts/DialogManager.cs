using UnityEngine;
using UnityEngine.UI;
using TMPro;

using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
    public TextMeshProUGUI dialogText;

    public Image[] characterPortraits;
    public Sprite[] characterSpritesInactive;
    public Sprite[] characterSpritesActive;

    private Queue<string> dialogQueue = new Queue<string>();
    private int currentSpeaker = -1;

    void Start()
    {
        // Debug dialog lines
        dialogQueue.Enqueue("0: Hello, I am character 0.");
        dialogQueue.Enqueue("1: Now character 1 is speaking.");
        dialogQueue.Enqueue("2: Test dialog");
        dialogQueue.Enqueue("0: Back to character 0!");
        ShowNextDialog();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
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
                currentSpeaker = speakerIndex;
                dialogText.text = dialog;
                HighlightSpeaker(currentSpeaker);
            }
            else
            {
                dialogText.text = line;
                HighlightSpeaker(-1);
            }
        }
        else
        {
            dialogText.text = line;
            HighlightSpeaker(-1);
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
}
