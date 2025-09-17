
using UnityEngine;
using TMPro;


using UnityEngine.Serialization;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SpeechBubble : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private float extraHeight = 10f;
    [SerializeField] private Color lightTextColor = Color.white;
    [SerializeField] private Color darkTextColor = Color.black;
    [SerializeField] private UnityEngine.UI.Image image;
    void OnEnable()
    {
        if (image != null)
            image.enabled = true;
        if (textMeshPro != null)
            textMeshPro.color = lightTextColor;
    }

    void OnDisable()
    {
        if (image != null)
            image.enabled = false;
        if (textMeshPro != null)
            textMeshPro.color = darkTextColor;
    }

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
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                textMeshPro.ForceMeshUpdate();
            #endif
            float preferredHeight = textMeshPro.preferredHeight;
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
