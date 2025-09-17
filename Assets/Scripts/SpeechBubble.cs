
using UnityEngine;
using TMPro;


[RequireComponent(typeof(RectTransform))]
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private float extraHeight = 10f;

    private RectTransform rectTransform;
    private float lastPreferredHeight = -1f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (textMeshPro != null && rectTransform != null)
        {
            float preferredHeight = textMeshPro.GetPreferredValues(textMeshPro.text, rectTransform.rect.width, Mathf.Infinity).y;
            if (!Mathf.Approximately(preferredHeight, lastPreferredHeight))
            {
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight + extraHeight);
                lastPreferredHeight = preferredHeight;
            }
        }
    }

    public void SetText(string text)
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = text;
        }
    }
}
