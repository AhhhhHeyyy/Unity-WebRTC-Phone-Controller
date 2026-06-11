using UnityEngine;
using UnityEngine.Events;

// 拼圖階段系統：每完成一片呼叫OnPieceCompleted()，
// 累計片數達到某個Stage的requiredCount時觸發該Stage（可同時觸發多個物件/動畫），
// 並停止上一個已啟用的Stage（呼叫其onDeactivate），直到下次被指定觸發為止。
public class PuzzleStageManager : MonoBehaviour
{
    [System.Serializable]
    public class Stage
    {
        public string stageName;

        [Tooltip("累計完成片數達到此值時觸發")]
        public int requiredCount;

        [Tooltip("觸發時要做的事：啟用3D物件、播放動畫等")]
        public UnityEvent onActivate;

        [Tooltip("被下一個Stage取代時要做的事：停用物件、停止動畫等")]
        public UnityEvent onDeactivate;

        [HideInInspector] public bool activated;
    }

    public Stage[] stages;

    private int completedCount = 0;
    private Stage currentStage;

    // 由每片拼圖（BoundingBoxOverlapSnap）吸附完成時呼叫
    public void OnPieceCompleted()
    {
        completedCount++;

        foreach (var stage in stages)
        {
            if (!stage.activated && completedCount >= stage.requiredCount)
            {
                ActivateStage(stage);
            }
        }
    }

    private void ActivateStage(Stage stage)
    {
        if (currentStage != null)
            currentStage.onDeactivate?.Invoke();

        stage.onActivate?.Invoke();
        stage.activated = true;
        currentStage = stage;
    }

    // 重置整個階段系統（例如重玩關卡時呼叫）
    public void ResetStages()
    {
        completedCount = 0;
        currentStage = null;
        foreach (var stage in stages)
            stage.activated = false;
    }
}
