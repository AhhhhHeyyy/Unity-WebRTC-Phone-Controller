# Unity WebRTC Phone Controller

用手機瀏覽器透過 WebRTC 即時控制 Unity 場景中的物件——支援陀螺儀旋轉、搖桿移動、按鈕抓取三種輸入。

## 需求

- Unity **6000.0.60f1**（URP）
- 網路連線（手機與電腦需能連到 Railway Signaling Server）

## 下載與開啟方式

> ⚠️ 這是完整的 Unity 專案，**不是 Package**。
> 不要把連結貼到 Unity Package Manager，那樣會報錯。

### 方法一：直接下載 ZIP（不需安裝 git）

1. **[點此下載 ZIP](https://github.com/AhhhhHeyyy/UnityLong/archive/refs/heads/main.zip)**
2. 解壓縮，得到 `UnityLong-main` 資料夾
3. 打開 **Unity Hub** → **Add** → **Add project from disk**
4. 選取解壓縮後的資料夾，確認 Unity 版本 **6000.0.60f1** → Open

### 方法二：git clone（需安裝 git）

```bash
git clone https://github.com/AhhhhHeyyy/UnityLong.git
```

clone 完後同樣用 Unity Hub **Add project from disk** 開啟 `UnityLong` 資料夾。

---

### 開啟後

- 第一次開啟時 Unity 會自動下載套件（WebRTC、NativeWebSocket、URP），需等幾分鐘
- 套件還原完後，打開 `Assets/Assets/Scenes/` 內任一場景，按 **Play**
- 畫面上的 QR Code 用手機掃描即可開始控制

---

## 運作原理

```
手機瀏覽器 (sensor.html)
    │  WebSocket (Signaling)
    ▼
Railway Signaling Server
    │  WebRTC DataChannel (28 bytes / packet)
    ▼
Unity (WebRtcGyroscopeReceiver)
    │  SensorEvents (static events)
    ├─▶ GyroToRotation       → 物件旋轉
    ├─▶ WebRtcJoystickController → 物件移動
    └─▶ WebRtcSelectController   → 射線抓取
```

手機每幀發送 **28 bytes Big-Endian** 封包，包含四元數、加速度、搖桿軸值、按鈕狀態，Unity 端解析後透過 `SensorEvents` 靜態事件分發給各控制元件。

---

## 專案結構

```
Assets/Assets/
├── Scenes/
│   ├── 0521.unity          主要展示場景
│   ├── SampleScene.unity   URP 預設場景
│   └── Scene1.unity        基礎測試場景
│
├── WebRtcGyroscopeReceiver.cs   核心：WebRTC 連線 + 封包解析 + 房間碼產生
├── SensorEvents.cs              靜態事件匯流排（陀螺儀 / 搖桿 / 抓取）
├── GyroToRotation.cs            訂閱陀螺儀事件，驅動 Transform 旋轉
├── WebRtcJoystickController.cs  訂閱搖桿事件，驅動 Transform 位移
├── WebRtcSelectController.cs    訂閱按鈕事件，射線抓取 DraggableObject
├── DraggableObject_套於物件.cs  掛在可抓取物件上，供射線辨識
├── QrCodeDisplay.cs             讀取房間碼，產生並顯示 QR Code 圖片
│
├── JoyconLib_scripts/           Joy-Con 有線輸入支援（HIDapi / Joycon / JoyconManager）
├── JoyconLib_plugins/           HID 原生函式庫（win32 / win64 / mac）
│
├── AntiGravityObject.cs         反重力物件行為
├── Gravity_CSharpScript2_TkOver.cs  重力覆蓋腳本
├── Motor_Rotate&Move.cs         馬達旋轉 + 移動腳本
├── MouseDragPhysics2_套於Camera.cs  滑鼠拖拉物理（掛在相機上）
│
├── Settings/                    URP 渲染管線設定（PC / Mobile 品質分層）
├── Crate.prefab                 可抓取木箱預製件
├── GameObject.prefab            通用預製件
├── stick.prefab / big bawl.fbx  自訂模型
└── *.mat / *.shader             材質與 Shader
```

---

## 腳本掛載說明

### 最小可用配置

| 腳本 | 掛在哪裡 | 必填欄位 |
|------|----------|----------|
| `WebRtcGyroscopeReceiver` | 任意 GameObject | Signaling URL（預設已填） |
| `QrCodeDisplay` | 有 `RawImage` 的 UI 物件 | `Receiver`（拖入上面的物件） |
| `GyroToRotation` | 想被陀螺儀旋轉的物件 | — |
| `WebRtcJoystickController` | 想被搖桿移動的物件 | — |
| `WebRtcSelectController` | 任意 GameObject（相機上亦可） | `AimPoint`、`LockTarget` |
| `DraggableObject_套於物件` | 每個可被抓取的物件 | — |

### WebRtcSelectController 設定

- **AimPoint**：射線起點（通常是相機或手持點的 Transform）
- **LockTarget**：持有物件時跟隨的 Transform（與 AimPoint 相同即可）
- **DraggableLayer**：只有同層的物件會被射線命中

---

## 套件依賴

| 套件 | 版本 |
|------|------|
| com.unity.webrtc | 3.0.0-pre.8 |
| com.endel.nativewebsocket | git (upm branch) |
| com.unity.render-pipelines.universal | 17.0.4 |

套件清單記錄在 `Packages/manifest.json`，clone 後 Unity 會自動還原，無需手動安裝。

---

## Signaling Server

預設連線至 `wss://wtb-sensor-production.up.railway.app`，可在 `WebRtcGyroscopeReceiver` Inspector 的 **Signaling URL** 欄位修改。手機端頁面為同網域下的 `sensor.html`，掃 QR Code 後會自動帶入房間碼參數。
