using UnityEngine;

/// <summary>
/// 以相機為起點，穿過 aimPoint 的螢幕投影位置發射射線（等同滑鼠點擊邏輯）。
/// 可處理物件有深度差的情況，相機移動時選取行為與 FPS 準心一致。
/// 持有期間物件跟著 lockTarget 位置移動。
/// </summary>
public class WebRtcScreenRayController : MonoBehaviour
{
    [Header("瞄準點")]
    [Tooltip("瞄準點 Transform，其螢幕座標作為射線穿透點")]
    public Transform aimPoint;

    [Tooltip("持有時物件跟著此 Transform 移動")]
    public Transform lockTarget;

    [Header("抓取設定")]
    public float dragForce   = 20f;
    public float dragDamping = 5f;
    public LayerMask draggableLayer = ~0;

    private Camera    _cam;
    private Rigidbody _targetBody;
    private Vector3   _holdOffset;
    private bool      _isHolding;

    void Start()
    {
        _cam = Camera.main;
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
        if (!_isHolding || aimPoint == null || _targetBody != null) return;

        Vector3 screenPos = _cam.WorldToScreenPoint(aimPoint.position);
        Ray ray = _cam.ScreenPointToRay(screenPos);

        RaycastHit[] hits = Physics.RaycastAll(ray, 200f, draggableLayer);
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
                Debug.LogWarning($"[ScreenRay] {hit.collider.name} 有 DraggableObject 但父層缺少 Rigidbody");
                continue;
            }

            Debug.Log($"[ScreenRay] 命中: {hit.collider.name}，距離: {hit.distance:F2}");
            _targetBody = rb;

            Transform anchor = lockTarget != null ? lockTarget : aimPoint;
            _holdOffset = rb.position - anchor.position;

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
        if (_targetBody == null) return;

        Transform anchor = lockTarget != null ? lockTarget : aimPoint;
        if (anchor == null) return;

        Vector3 target = anchor.position + _holdOffset;

        if (_targetBody.isKinematic)
            _targetBody.MovePosition(target);
        else
            _targetBody.linearVelocity = (target - _targetBody.position) * dragForce;
    }

    private void Release()
    {
        _isHolding = false;
        if (_targetBody == null) return;

        if (!_targetBody.isKinematic)
        {
            _targetBody.useGravity     = true;
            _targetBody.linearDamping  = 0.05f;
            _targetBody.angularDamping = 0.05f;
        }
        _targetBody = null;
    }
}
