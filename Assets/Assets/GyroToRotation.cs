using UnityEngine;

public class GyroToRotation : MonoBehaviour
{
    [SerializeField] private Vector3 eulerOffset = Vector3.zero;

    [Header("Game 視窗調整面板")]
    [Tooltip("在 Game 視窗顯示 Euler Offset 調整滑桿")]
    public bool showPanel = true;

    [Tooltip("每軸可調範圍（±值）")]
    public float sliderRange = 180f;

    private Quaternion pendingRotation = Quaternion.identity;
    private bool hasData = false;

    // ── GUI 版面常數 ──────────────────────────────────────────
    private const float PanelX      = 10f;
    private const float PanelY      = 10f;
    private const float PanelWidth  = 300f;
    private const float PanelHeight = 120f;
    private const float LabelWidth  = 30f;
    private const float ValueWidth  = 50f;

    void OnEnable()
    {
        SensorEvents.OnGyroscopeDataReceived += HandleGyroscopeData;
    }

    void OnDisable()
    {
        SensorEvents.OnGyroscopeDataReceived -= HandleGyroscopeData;
    }

    private void HandleGyroscopeData(SensorEvents.GyroscopeData data)
    {
        float qx = data.qx, qy = data.qy, qz = data.qz, qw = data.qw;
        float mag2 = qx*qx + qy*qy + qz*qz + qw*qw;
        if (mag2 < 0.5f) return;
        // Browser right-hand (X=East, Y=North, Z=Up) → Unity left-hand (X=Right, Y=Up, Z=Forward)
        pendingRotation = new Quaternion(qx, -qz, qy, qw);
        hasData = true;
    }

    void Update()
    {
        if (hasData)
            transform.rotation = Quaternion.Euler(eulerOffset) * pendingRotation;
    }

    void OnGUI()
    {
        if (!showPanel) return;

        GUI.Box(new Rect(PanelX, PanelY, PanelWidth, PanelHeight), "Euler Offset");

        float y = PanelY + 25f;
        eulerOffset.x = DrawSliderRow("X", eulerOffset.x, y);
        y += 30f;
        eulerOffset.y = DrawSliderRow("Y", eulerOffset.y, y);
        y += 30f;
        eulerOffset.z = DrawSliderRow("Z", eulerOffset.z, y);

        // Reset 按鈕
        y += 32f;
        if (GUI.Button(new Rect(PanelX + 8f, y, 60f, 22f), "Reset"))
            eulerOffset = Vector3.zero;
    }

    /// <summary>畫一行：軸名 Label + Slider + 數值顯示</summary>
    private float DrawSliderRow(string axisLabel, float current, float y)
    {
        float x = PanelX + 8f;

        // 軸名
        GUI.Label(new Rect(x, y, LabelWidth, 22f), axisLabel);
        x += LabelWidth;

        // Slider
        float sliderWidth = PanelWidth - LabelWidth - ValueWidth - 24f;
        float newVal = GUI.HorizontalSlider(
            new Rect(x, y + 4f, sliderWidth, 18f),
            current, -sliderRange, sliderRange);
        x += sliderWidth + 4f;

        // 數值（可直接輸入）
        string input = GUI.TextField(new Rect(x, y, ValueWidth, 22f), newVal.ToString("F1"));
        if (float.TryParse(input, out float parsed))
            newVal = Mathf.Clamp(parsed, -sliderRange, sliderRange);

        return newVal;
    }
}
