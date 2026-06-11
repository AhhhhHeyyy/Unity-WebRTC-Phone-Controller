using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 套於3D物件上：偵測自身螢幕空間AABB與PNG(UI Image)的AABB重疊比例，
// 超過閾值時PNG變色並把自身吸附到指定位置（一次性觸發）。
public class BoundingBoxOverlapSnap : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;          // 留空則用Camera.main
    public RectTransform pngRectTransform;
    public Image pngImage;
    public Transform snapTarget;         // 吸附目標：固定吸附位置，或followPngPosition時提供平面高度與旋轉
    public Transform boardPlane;         // 選填，PNG對應的3D平面（用其position+up定義平面）；留空則用snapTarget.position
    public bool followPngPosition = false; // true: 吸附位置改用PNG在螢幕上的位置投影到平面計算
    public Vector3 planeNormal = Vector3.up; // boardPlane未設定時，平面的法向量（預設水平面）
    public WebRtcScreenRayController screenRayController; // 若目前正被抓取，吸附時強制釋放
    public PuzzleStageManager stageManager;               // 觸發吸附時通知階段系統

    [Header("Settings")]
    [Range(0f, 1f)]
    public float overlapThreshold = 0.7f;
    public Color overlapColor = Color.red;
    public float snapDuration = 0.2f;    // 0 = 瞬間吸附

    private Renderer targetRenderer;
    private Rigidbody targetRigidbody;
    private Color originalColor;
    private bool triggered = false;
    private bool locked = false;
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;

    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        targetRigidbody = GetComponent<Rigidbody>();
        if (targetCamera == null) targetCamera = Camera.main;
        if (pngImage != null) originalColor = pngImage.color;
    }

    void Update()
    {
        if (triggered) return;

        Rect objRect = GetScreenRect(targetRenderer.bounds);
        Rect pngRect = GetPngScreenRect();

        float ratio = GetOverlapRatio(objRect, pngRect);
        if (ratio >= overlapThreshold)
        {
            Trigger();
        }
    }

    void LateUpdate()
    {
        if (!locked) return;

        // 每幀最後強制鎖回吸附位置與朝向，避免被controller/物理在本幀稍早覆寫
        transform.position = lockedPosition;
        transform.rotation = lockedRotation;

        if (targetRigidbody != null && !targetRigidbody.isKinematic)
        {
            targetRigidbody.linearVelocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void Trigger()
    {
        triggered = true;

        if (pngImage != null)
            pngImage.color = overlapColor;

        // 若目前正被controller抓取，先強制釋放，避免吸附後仍被搖桿/抓取邏輯持續移動
        if (screenRayController != null && targetRigidbody != null)
            screenRayController.ForceRelease(targetRigidbody);

        // 吸附前先讓Rigidbody停止物理運算，避免被其他Collider推開
        if (targetRigidbody != null)
        {
            targetRigidbody.linearVelocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
            targetRigidbody.isKinematic = true;
        }

        // 停用Collider，避免之後再被射線偵測抓取
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (stageManager != null)
            stageManager.OnPieceCompleted();

        // 計算吸附目標位置：優先使用PNG在平面上的投影，否則使用snapTarget
        lockedPosition = followPngPosition
            ? GetPngWorldPosition()
            : (snapTarget != null ? snapTarget.position : transform.position);
        lockedRotation = snapTarget != null ? snapTarget.rotation : transform.rotation;

        if (snapDuration <= 0f)
        {
            transform.position = lockedPosition;
            transform.rotation = lockedRotation;
            locked = true;
        }
        else
        {
            StartCoroutine(SnapLerp());
        }
    }

    // 將PNG在螢幕上的中心點，投影到3D平面，取得對應世界座標
    // 平面：有設定boardPlane則用其position+up，否則用snapTarget.position+planeNormal
    private Vector3 GetPngWorldPosition()
    {
        Vector3 fallback = snapTarget != null ? snapTarget.position : transform.position;

        Vector3 planePoint = boardPlane != null ? boardPlane.position : fallback;
        Vector3 normal = boardPlane != null ? boardPlane.up : planeNormal;

        Rect pngRect = GetPngScreenRect();
        Vector2 center = pngRect.center;

        Ray ray = targetCamera.ScreenPointToRay(center);
        Plane plane = new Plane(normal, planePoint);

        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);

        return fallback;
    }

    private IEnumerator SnapLerp()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = lockedPosition;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = lockedRotation;
        float t = 0f;

        while (t < snapDuration)
        {
            t += Time.deltaTime;
            float lerpT = t / snapDuration;
            transform.position = Vector3.Lerp(startPos, endPos, lerpT);
            transform.rotation = Quaternion.Slerp(startRot, endRot, lerpT);
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
        locked = true;
    }

    // 取得3D物件bounds投影到螢幕空間後的AABB
    private Rect GetScreenRect(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners = new Vector3[8]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
        };

        Vector2 screenMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 screenMax = new Vector2(float.MinValue, float.MinValue);

        foreach (var corner in corners)
        {
            Vector3 sp = targetCamera.WorldToScreenPoint(corner);
            if (sp.z < 0f) continue; // 物件在相機後方，忽略

            screenMin = Vector2.Min(screenMin, sp);
            screenMax = Vector2.Max(screenMax, sp);
        }

        return Rect.MinMaxRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
    }

    // 取得PNG(UI RectTransform)在螢幕空間的AABB
    private Rect GetPngScreenRect()
    {
        Vector3[] worldCorners = new Vector3[4];
        pngRectTransform.GetWorldCorners(worldCorners);

        Canvas canvas = pngRectTransform.GetComponentInParent<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        Vector2 screenMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 screenMax = new Vector2(float.MinValue, float.MinValue);

        foreach (var corner in worldCorners)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
            screenMin = Vector2.Min(screenMin, sp);
            screenMax = Vector2.Max(screenMax, sp);
        }

        return Rect.MinMaxRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
    }

    // 重疊面積 / 3D物件投影面積
    private float GetOverlapRatio(Rect objRect, Rect pngRect)
    {
        float xOverlap = Mathf.Max(0f, Mathf.Min(objRect.xMax, pngRect.xMax) - Mathf.Max(objRect.xMin, pngRect.xMin));
        float yOverlap = Mathf.Max(0f, Mathf.Min(objRect.yMax, pngRect.yMax) - Mathf.Max(objRect.yMin, pngRect.yMin));
        float overlapArea = xOverlap * yOverlap;

        float objArea = objRect.width * objRect.height;
        if (objArea <= 0f) return 0f;

        return overlapArea / objArea;
    }

    // 一次性觸發後可呼叫此方法手動重置
    public void ResetTrigger()
    {
        triggered = false;
        locked = false;
        if (pngImage != null)
            pngImage.color = originalColor;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (targetRigidbody != null)
            targetRigidbody.isKinematic = false;
    }
}
