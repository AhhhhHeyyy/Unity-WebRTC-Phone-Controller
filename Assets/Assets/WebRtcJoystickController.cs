using UnityEngine;

/// <summary>
/// 訂閱 SensorEvents.OnJoystickReceived，依搖桿輸入移動物件。
/// screenRayController 持有物件時，搖桿移動該被抓物件（修改 holdOffset）；無持有則不作用。
/// </summary>
public class WebRtcJoystickController : MonoBehaviour
{
    [Header("控制目標")]
    [Tooltip("WebRtcScreenRayController；有物件被抓起時，搖桿移動它")]
    public WebRtcScreenRayController screenRayController;

    [Header("移動設定")]
    [Tooltip("移動速度（單位/秒）")]
    public float speed = 5f;

    [Header("軸向反轉")]
    [Tooltip("勾選後水平軸（左右）反向")]
    public bool invertHorizontal = false;

    [Tooltip("勾選後垂直軸（前後）反向")]
    public bool invertVertical = false;

    [Header("Game 視窗調整面板")]
    [Tooltip("在 Game 視窗顯示即時調整面板")]
    public bool showPanel = true;

    [Tooltip("速度 Slider 最大值")]
    public float maxSpeed = 30f;

    private float _h, _v;

    // ── GUI 版面常數 ──────────────────────────────────────────
    private const float PanelX      = 10f;
    private const float PanelY      = 140f;
    private const float PanelWidth  = 300f;
    private const float PanelHeight = 135f;

    void OnEnable()  => SensorEvents.OnJoystickReceived += HandleJoystick;
    void OnDisable() => SensorEvents.OnJoystickReceived -= HandleJoystick;

    private void HandleJoystick(SensorEvents.JoystickData data)
    {
        _h = data.horizontal;
        _v = data.vertical;
    }

    void Update()
    {
        if (_h == 0f && _v == 0f) return;

        float h = _h * (invertHorizontal ? -1f : 1f);
        float v = _v * (invertVertical   ? -1f : 1f);
        Vector3 delta = new Vector3(h, 0f, v) * speed * Time.deltaTime;

        if (screenRayController != null && screenRayController.IsHolding)
            screenRayController.AddHoldOffset(delta);
    }

    void OnGUI()
    {
        if (!showPanel) return;

        GUI.Box(new Rect(PanelX, PanelY, PanelWidth, PanelHeight), "Joystick Controller");

        float x = PanelX + 8f;
        float y = PanelY + 25f;

        // ── Speed Slider ──────────────────────────────────────
        GUI.Label(new Rect(x, y, 50f, 22f), "Speed");
        float sliderW = PanelWidth - 50f - 58f - 16f;
        speed = GUI.HorizontalSlider(new Rect(x + 54f, y + 4f, sliderW, 18f), speed, 0f, maxSpeed);
        string speedInput = GUI.TextField(new Rect(x + 54f + sliderW + 4f, y, 50f, 22f), speed.ToString("F1"));
        if (float.TryParse(speedInput, out float parsedSpeed))
            speed = Mathf.Clamp(parsedSpeed, 0f, maxSpeed);
        y += 30f;

        // ── invertHorizontal Toggle ───────────────────────────
        invertHorizontal = GUI.Toggle(new Rect(x, y, PanelWidth - 16f, 22f),
                                      invertHorizontal, " 水平軸反轉（左右）");
        y += 26f;

        // ── invertVertical Toggle ─────────────────────────────
        invertVertical = GUI.Toggle(new Rect(x, y, PanelWidth - 16f, 22f),
                                    invertVertical, " 垂直軸反轉（前後）");
    }
}
