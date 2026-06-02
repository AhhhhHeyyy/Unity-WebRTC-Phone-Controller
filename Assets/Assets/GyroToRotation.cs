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

    // 橫/直式共享狀態，供其他面板讀取以調整版面
    public static bool IsLandscape = false;

    // 右橫式補償：手機繞本地 Z 軸（螢幕法向量）順時針 90°
    // 反向 = 右乘 +90° 繞本地 Z，讓橫式前傾 ≡ 直式前傾
    private static readonly Quaternion LandscapeCompensation =
        new Quaternion(0f, 0f, 0.7071068f, 0.7071068f);

    // ── GUI 版面常數 ──────────────────────────────────────────
    private const float PanelWidth  = 300f;
    private const float PanelHeight = 120f;
    private const float LabelWidth  = 30f;
    private const float ValueWidth  = 50f;
    private const float Margin      = 10f;

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

        Quaternion q = new Quaternion(qx, qy, qz, qw);
        if (IsLandscape)
        {
            // 右橫式：消除手機 90° 順時針本地旋轉，使手勢語意與直式一致
            q = q * LandscapeCompensation;
        }
        // Browser right-hand (X=East, Y=North, Z=Up) → Unity left-hand (X=Right, Y=Up, Z=Forward)
        pendingRotation = new Quaternion(q.x, -q.z, q.y, q.w);
        hasData = true;
    }

    void Update()
    {
        if (hasData)
            transform.rotation = Quaternion.Euler(eulerOffset) * pendingRotation;
    }

    void OnGUI()
    {
        // 橫/直切換按鈕：固定右上角，兩種模式都顯示
        float btnW = 80f, btnH = 25f;
        if (GUI.Button(new Rect(Screen.width - btnW - Margin, Margin, btnW, btnH),
                       IsLandscape ? "直式" : "橫式"))
        {
            IsLandscape = !IsLandscape;
#if !UNITY_EDITOR
            Screen.orientation = IsLandscape
                ? ScreenOrientation.LandscapeLeft
                : ScreenOrientation.Portrait;
#endif
        }

        if (!showPanel) return;

        float panelX = Margin;
        float panelY = IsLandscape
            ? Screen.height - PanelHeight - Margin
            : Margin;

        GUI.Box(new Rect(panelX, panelY, PanelWidth, PanelHeight), "Euler Offset");

        float y = panelY + 25f;
        eulerOffset.x = DrawSliderRow("X", eulerOffset.x, panelX, y); y += 30f;
        eulerOffset.y = DrawSliderRow("Y", eulerOffset.y, panelX, y); y += 30f;
        eulerOffset.z = DrawSliderRow("Z", eulerOffset.z, panelX, y);

        y += 32f;
        if (GUI.Button(new Rect(panelX + 8f, y, 60f, 22f), "Reset"))
            eulerOffset = Vector3.zero;
    }

    private float DrawSliderRow(string axisLabel, float current, float panelX, float y)
    {
        float x = panelX + 8f;

        GUI.Label(new Rect(x, y, LabelWidth, 22f), axisLabel);
        x += LabelWidth;

        float sliderWidth = PanelWidth - LabelWidth - ValueWidth - 24f;
        float newVal = GUI.HorizontalSlider(
            new Rect(x, y + 4f, sliderWidth, 18f),
            current, -sliderRange, sliderRange);
        x += sliderWidth + 4f;

        string input = GUI.TextField(new Rect(x, y, ValueWidth, 22f), newVal.ToString("F1"));
        if (float.TryParse(input, out float parsed))
            newVal = Mathf.Clamp(parsed, -sliderRange, sliderRange);

        return newVal;
    }
}
