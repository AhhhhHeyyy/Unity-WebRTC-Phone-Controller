using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 網頁按鈕按下(0x03) → 從 aimPoint 發射 Raycast，
/// 射線路徑上第一個有 DraggableObject 的物件才會被抓取，其餘全部穿透。
/// 持有期間物件跟著 lockTarget 位置移動。
/// 按鈕放開(0x04) → 放開物件。
/// </summary>
public class WebRtcSelectController : MonoBehaviour
{
    [Header("瞄準點")]
    [Tooltip("Raycast 從這裡射出（方向參考用，本身不會被命中）")]
    public Transform aimPoint;

    [Tooltip("射線固定朝向此物件；持有時物件跟著此 Transform 移動")]
    public Transform lockTarget;

    [Header("抓取設定")]
    public float dragForce   = 20f;
    public float dragDamping = 5f;
    public LayerMask draggableLayer = ~0;

    [Header("射線視覺化")]
    public float rayLength = 100f;
    [Tooltip("光管直徑（越大越粗壯）")]
    public float tubeWidth = 0.18f;

    private Rigidbody    targetBody;
    private Vector3      holdOffset;
    private bool         _isHolding;
    private LineRenderer _glow;   // 外層柔光（寬＋漸層貼圖）
    private LineRenderer _core;   // 亮白芯（細）

    void Awake()
    {
        Texture2D neonTex = BuildNeonTexture(64);

        // 外層：寬光暈，用漸層貼圖製造「圓管截面」感
        _glow = MakeLayer("_RayGlow");
        _glow.startWidth = tubeWidth;
        _glow.endWidth   = tubeWidth * 0.25f;
        _glow.material   = new Material(Shader.Find("Sprites/Default"));
        _glow.material.mainTexture = neonTex;
        _glow.startColor = Color.white;
        _glow.endColor   = new Color(1f, 1f, 1f, 0.4f);

        // 內層：細亮芯，讓中心有強烈亮點
        _core = gameObject.AddComponent<LineRenderer>();
        _core.positionCount     = 2;
        _core.useWorldSpace     = true;
        _core.shadowCastingMode = ShadowCastingMode.Off;
        _core.receiveShadows    = false;
        _core.startWidth = tubeWidth * 0.10f;
        _core.endWidth   = tubeWidth * 0.03f;
        _core.material   = new Material(Shader.Find("Sprites/Default"));
        _core.material.mainTexture = neonTex;
        _core.startColor = Color.white;
        _core.endColor   = new Color(1f, 1f, 1f, 0.8f);
    }

    // 生成 1×h 的 Gaussian 漸層貼圖：中心全白不透明，邊緣淡出為透明
    private Texture2D BuildNeonTexture(int h)
    {
        var tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int i = 0; i < h; i++)
        {
            float t    = (float)i / (h - 1);          // 0..1
            float dist = Mathf.Abs(t - 0.5f) * 2f;   // 0=中心 1=邊緣
            float a    = Mathf.Exp(-dist * dist * 5f); // Gaussian 衰減
            tex.SetPixel(0, i, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    private LineRenderer MakeLayer(string goName)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount     = 2;
        lr.useWorldSpace     = true;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        return lr;
    }

    void OnEnable()
    {
        SensorEvents.OnGrabPressed  += OnPressed;
        SensorEvents.OnGrabReleased += Release;
    }

    void OnDisable()
    {
        SensorEvents.OnGrabPressed  -= OnPressed;
        SensorEvents.OnGrabReleased -= Release;
    }

    private void OnPressed() => _isHolding = true;

    void Update()
    {
        if (aimPoint != null)
        {
            Vector3 start = aimPoint.position;
            Vector3 end   = start + GetDir() * rayLength;
            SetPositions(start, end);
            SetEnabled(true);
        }
        else
        {
            SetEnabled(false);
        }

        if (!_isHolding || aimPoint == null) return;

        Vector3 dir = GetDir();

        if (targetBody != null) return;

        RaycastHit[] hits = Physics.RaycastAll(aimPoint.position, dir, 100f, draggableLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(aimPoint.root)) continue;
            if (lockTarget != null && hit.collider.transform.IsChildOf(lockTarget.root)) continue;

            DraggableObject draggable = hit.collider.GetComponentInParent<DraggableObject>();
            if (draggable == null) continue;

            Rigidbody rb = draggable.GetComponentInParent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning($"[Grab] {hit.collider.name} 有 DraggableObject 但父層缺少 Rigidbody");
                continue;
            }

            Debug.Log($"[Grab] 命中: {hit.collider.name}，距離: {hit.distance:F2}");
            targetBody = rb;

            Transform anchor = lockTarget != null ? lockTarget : aimPoint;
            holdOffset = rb.position - anchor.position;

            if (!rb.isKinematic)
            {
                rb.useGravity     = false;
                rb.linearDamping  = dragDamping;
                rb.angularDamping = dragDamping;
            }
            break;
        }
    }

    void FixedUpdate()
    {
        if (targetBody == null) return;

        Transform anchor = lockTarget != null ? lockTarget : aimPoint;
        if (anchor == null) return;

        Vector3 target = anchor.position + holdOffset;

        if (targetBody.isKinematic)
            targetBody.MovePosition(target);
        else
            targetBody.linearVelocity = (target - targetBody.position) * dragForce;
    }

    private void Release()
    {
        _isHolding = false;
        if (targetBody == null) return;

        if (!targetBody.isKinematic)
        {
            targetBody.useGravity     = true;
            targetBody.linearDamping  = 0.05f;
            targetBody.angularDamping = 0.05f;
        }
        targetBody = null;
    }

    private void SetPositions(Vector3 s, Vector3 e)
    {
        _glow.SetPosition(0, s); _glow.SetPosition(1, e);
        _core.SetPosition(0, s); _core.SetPosition(1, e);
    }

    private void SetEnabled(bool on)
    {
        _glow.enabled = on;
        _core.enabled = on;
    }

    private Vector3 GetDir() => lockTarget != null
        ? (lockTarget.position - aimPoint.position).normalized
        : aimPoint.forward;
}
