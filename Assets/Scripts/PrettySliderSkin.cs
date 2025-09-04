using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class PrettySliderSkin : MonoBehaviour
{
    [Header("A qué sliders aplicar")]
    public Slider[] targets;

    [Header("Colores")]
    public Color trackColor = new Color(1,1,1,0.2f);       // BG
    public Color fillColor  = new Color(0.22f,0.64f,0.96f); // Relleno
    public Color handleColor = Color.white;

    [Header("Dimensiones")]
    [Range(4,20)] public int trackHeight = 8;
    [Range(16,40)] public int handleSize = 28;

    [Header("Sombras")]
    public bool addShadow = true;
    [Range(0f,1f)] public float shadowAlpha = 0.3f;

    [Header("Etiqueta de valor")]
    public bool showValueLabel = false;
    public TMP_FontAsset labelFont;
    [Range(10,24)] public int labelFontSize = 12;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        if (targets == null) return;
        foreach (var s in targets)
        {
            if (s == null) continue;

            // Transitions
            s.transition = Selectable.Transition.ColorTint;
            var colors = s.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f,0.96f,0.96f);
            colors.pressedColor = new Color(0.90f,0.90f,0.90f);
            s.colors = colors;

            // Background
            var bg = s.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = trackColor;
                var rt = bg.rectTransform;
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.sizeDelta = new Vector2(0, trackHeight);
            }

            // Fill
            var fill = s.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = fillColor;
                var rt = fill.rectTransform;
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.sizeDelta = new Vector2(0, trackHeight);
            }

            // Handle
            var handle = s.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
            if (handle != null)
            {
                handle.color = handleColor;
                var rt = handle.rectTransform;
                rt.sizeDelta = new Vector2(handleSize, handleSize);

                // Shadow (opcional)
                var shadow = handle.GetComponent<Shadow>();
                if (addShadow)
                {
                    if (shadow == null) shadow = handle.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0,0,0, shadowAlpha);
                    shadow.effectDistance = new Vector2(0, -1f);
                }
                else if (shadow != null) DestroyImmediate(shadow);
            }

            // Value label (opcional)
            var handleTf = s.transform.Find("Handle Slide Area/Handle");
            if (showValueLabel && handleTf != null)
            {
                var existing = handleTf.GetComponentInChildren<TextMeshProUGUI>();
                if (existing == null)
                {
                    var go = new GameObject("ValueLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                    go.transform.SetParent(handleTf, false);
                    var txt = go.GetComponent<TextMeshProUGUI>();
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = labelFontSize;
                    if (labelFont) txt.font = labelFont;
                    var r = go.GetComponent<RectTransform>();
                    r.anchorMin = r.anchorMax = new Vector2(0.5f, -0.2f);
                    r.pivot = new Vector2(0.5f, 1f);
                    r.anchoredPosition = new Vector2(0, -18f);
                    r.sizeDelta = new Vector2(40, 20);

                    // Actualiza el texto en tiempo real
                    var updater = s.gameObject.GetComponent<_ValueLabelUpdater>();
                    if (updater == null) updater = s.gameObject.AddComponent<_ValueLabelUpdater>();
                    updater.hook(s, txt);
                }
            }
        }
    }

    // Helper interno para mantener el valor sincronizado
    class _ValueLabelUpdater : MonoBehaviour
    {
        Slider s; TextMeshProUGUI t;
        public void hook(Slider _s, TextMeshProUGUI _t)
        { s = _s; t = _t; UpdateText(); s.onValueChanged.AddListener(_ => UpdateText()); }
        void OnDisable(){ if (s!=null) s.onValueChanged.RemoveAllListeners(); }
        void UpdateText(){ if (s!=null && t!=null) t.text = s.wholeNumbers ? s.value.ToString("0") : s.value.ToString("0.0"); }
    }
}
