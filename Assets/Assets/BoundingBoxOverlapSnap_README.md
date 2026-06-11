# BoundingBoxOverlapSnap 使用說明

偵測3D物件的螢幕空間Bounding Box與PNG(UI Image)的Bounding Box重疊比例，
超過閾值時讓PNG變色，並把3D物件吸附到指定位置與朝向（一次性觸發），之後鎖定不再被
controller或物理影響。

## 原理

- 每幀把3D物件`Renderer.bounds`的8個角點投影到螢幕座標，取得2D AABB。
- 把PNG所在`RectTransform`的四個角轉成螢幕座標AABB。
- 計算兩個矩形的交集面積 ÷ 3D物件AABB面積，得到重疊比例。
- 比例 ≥ `Overlap Threshold` 時觸發一次：
  1. PNG變色
  2. 若正被`Screen Ray Controller`抓取，強制釋放
  3. `Rigidbody`歸零速度並設為`isKinematic`
  4. 停用`Collider`（避免之後再被射線偵測抓取）
  5. 通知`Stage Manager`累計完成片數
  6. 吸附到目標位置與旋轉（瞬間或Lerp過渡）
- 吸附完成後，`LateUpdate`每幀強制鎖回該位置與旋轉，避免被其他系統在本幀稍早覆寫。

## PNG設置步驟

1. 選取PNG檔案，Inspector將`Texture Type`改為`Sprite (2D and UI)`，Apply。
2. 建立Canvas（建議Render Mode = `Screen Space - Overlay`）。
3. 在Canvas下新增`UI > Image`，把PNG拖到`Source Image`。
4. 調整Image的RectTransform位置與大小（最終畫面位置）。

> 若用`Screen Space - Camera`或`World Space`，需確認Canvas的`Render Camera`已設定，
> 腳本會用該camera做座標轉換。

## 腳本設置步驟

1. 將`BoundingBoxOverlapSnap.cs`掛在要偵測的3D物件上（該物件需有`Renderer`元件，
   例如`MeshRenderer`；若有`Rigidbody`/`Collider`會在吸附時一併處理）。
2. Inspector欄位設定：

| 欄位 | 說明 |
|---|---|
| Target Camera | 留空則自動使用`Camera.main` |
| Png Rect Transform | PNG所在的UI RectTransform |
| Png Image | 同一個PNG的`Image`元件（用來變色） |
| Snap Target | 吸附目標Transform：固定模式下提供位置+旋轉；`Follow Png Position`模式下只提供高度（平面參考）+旋轉 |
| Board Plane | 選填。PNG對應的3D平面，用其`position`+`up`定義平面；留空則用`Snap Target.position` + `Plane Normal` |
| Follow Png Position | 勾選後，吸附位置改為「PNG目前螢幕位置」即時投影到平面算出（適合PNG/Camera會移動的情況） |
| Plane Normal | `Board Plane`未設定時，平面的法向量，預設`(0,1,0)`水平面 |
| Screen Ray Controller | 選填。若物件正被此controller抓取，吸附時會強制釋放 |
| Stage Manager | 選填。吸附觸發時呼叫其`OnPieceCompleted()` |
| Overlap Threshold | 觸發門檻，預設`0.7`（70%） |
| Overlap Color | 觸發時PNG變成的顏色 |
| Snap Duration | 吸附過渡時間（秒），`0`為瞬間吸附 |

## 吸附位置兩種模式

- **固定模式**（`Follow Png Position = false`，預設）：吸附位置/旋轉直接使用`Snap Target`
  的`position`/`rotation`。適合PNG與Camera都不會變動的情況，所見即所得。
- **跟隨PNG模式**（`Follow Png Position = true`）：觸發當下，從目前Camera對著PNG目前螢幕中心點
  打射線，與平面（`Board Plane`或`Snap Target.position` + `Plane Normal`）的交點作為吸附位置；
  旋轉仍使用`Snap Target.rotation`。適合PNG位置/Camera角度會變動的情況。

## 行為

- 一次性觸發：達到閾值後變色+吸附+鎖定，之後`Update`不再判斷。
- 鎖定期間`LateUpdate`每幀強制設回吸附位置/旋轉，並（非kinematic時）歸零速度，
  避免被controller或物理覆寫。
- 若需重新偵測，呼叫`ResetTrigger()`會還原PNG原色、重新啟用Collider、解除`isKinematic`
  並解除位置鎖定。

## 已知限制

- Bounding Box是矩形近似，物件旋轉或形狀不規則時，重疊比例會有誤差。
- PNG的AABB是整個RectTransform範圍（含透明區域），並非實際不透明輪廓。
- 多子物件組成的3D模型，`Renderer.bounds`只取`GetComponent<Renderer>()`抓到的
  那一個，若需合併所有子物件範圍需另外處理。
- `Follow Png Position`模式下，若`Plane Normal`與實際拼圖板法向量不一致，
  投影位置會有偏移。
