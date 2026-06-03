using UnityEngine;

/// <summary>
/// 訂閱 SensorEvents.OnJoystickReceived，依搖桿輸入移動指定物件。
/// horizontal(-1~1) = 左右，vertical(-1~1) = 前後。
/// 若 targetObject 有指定則移動該物件，否則移動自身。
/// 需在同場景中有 WebRtcGyroscopeReceiver 負責接收 WebRTC 封包。
/// </summary>
public class WebRtcJoystickController : MonoBehaviour
{
    [Header("控制目標")]
    [Tooltip("要被搖桿移動的父物件；留空則移動此腳本所在物件")]
    public Transform targetObject;

    [Header("移動設定")]
    [Tooltip("移動速度（單位/秒）")]
    public float speed = 5f;

    [Tooltip("勾選後以物件自身朝向決定前後左右；取消則以世界座標 XZ 平面移動")]
    public bool useLocalSpace = false;

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

    // 實際操控的 Transform（targetObject 優先，否則 fallback 自身）
    private Transform Target => targetObject != null ? targetObject : transform;

    // ── GUI 版面常數 ──────────────────────────────────────────
    private const float PanelX      = 10f;
    private const float PanelY      = 140f;   // 接在 GyroToRotation 面板下方
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
        var space = useLocalSpace ? Space.Self : Space.World;
        Target.Translate(new Vector3(h, 0f, v) * speed * Time.deltaTime, space);
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

        // ── useLocalSpace Toggle ──────────────────────────────
        useLocalSpace = GUI.Toggle(new Rect(x, y, PanelWidth - 16f, 22f),
                                   useLocalSpace, " Local Space（以自身朝向移動）");
        y += 26f;

        // ── invertHorizontal Toggle ───────────────────────────
        invertHorizontal = GUI.Toggle(new Rect(x, y, PanelWidth - 16f, 22f),
                                      invertHorizontal, " 水平軸反轉（左右）");
        y += 26f;

        // ── invertVertical Toggle ─────────────────────────────
        invertVertical = GUI.Toggle(new Rect(x, y, PanelWidth - 16f, 22f),
                                    invertVertical, " 垂直軸反轉（前後）");
    }
}
