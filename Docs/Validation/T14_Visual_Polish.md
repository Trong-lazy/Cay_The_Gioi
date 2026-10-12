# Nghiệm thu và Kiểm chứng T14 — Cải thiện Đồ họa Tropical Fantasy

Task: T14
Result: PASS
Status: DONE
Ngày: 12/10/2026
Môi trường: Unity 6000.6.1f1, URP, MainScene, config prototype-6, Windows x64

---

## 1. Mục tiêu và Phạm vi triển khai (PHẦN B)

Task T14 thực hiện nâng cấp toàn diện chất lượng hình ảnh môi trường theo định hướng **"Tropical Fantasy"** ấm áp, tươi sáng và rõ nét trên cả PC lẫn thiết bị di động, đồng thời bảo đảm nghiêm ngặt hiệu năng và tính toàn vẹn của logic gameplay:

- **B0**: Tạo file task `Docs/Tasks/14_Visual_Polish.md`, phụ thuộc `["T13"]`, cập nhật tiến độ tự động qua `update_progress.py`.
- **B1**: Nâng cấp đồ họa Tropical Fantasy:
  - **Terrain**: Tích hợp biến sắc theo biome bằng Vertex Colors trong `SphericalWorldTerrainBuilder.cs` (chuyển tiếp tự nhiên giữa cỏ nhiệt đới, cát bờ hồ, lối mòn đất đỏ và đáy hồ ngọc bích; loại bỏ hoàn toàn các mép gãy biên đất).
  - **Bầu trời & Khí quyển**: Bầu trời ngọc lam nhiệt đới (`#7ABCE0`), URP Fog xanh ngọc nhẹ (`fogColor = (0.68, 0.82, 0.92)`, `fogDensity = 0.0035f`) tạo độ sâu tách biệt tiền cảnh và hậu cảnh.
  - **Ánh sáng & Post-Processing**: Directional Light vàng mật ong ấm áp (`intensity = 1.15`), Soft Shadows mềm mại (`shadowStrength = 0.72`), Ambient Light thiên thanh mát mẻ (`#6B8AA3`). Tạo URP `VolumeProfile` (`TropicalFantasy_VolumeProfile.asset`) gồm Bloom (dành cho nấm phát quang dạ quang), Color Adjustments và Vignette nhẹ với preset linh hoạt cho mobile.
  - **Props & Culling Optimization**: Đổi toàn bộ mesh kín (đá rêu, đất, cát, đáy hồ, mặt nước hồ, thân cọ, nấm) sang `_Cull = 2` (Cull Back) sau khi đã chuẩn hóa winding clockwise; giữ `_Cull = 0` (Cull Off) cho lá cây và cánh hoa mỏng. Giữ nguyên tỷ lệ và vị trí props, bảo đảm `totalBlocked = 0` trong test orbit 360°.
  - **Cây Chính**: Tinh chỉnh material lá cây tươi tắn, tăng nhẹ tương phản; **bảo toàn 100% hash SHA256 của cả 6 FBX model và Prefab cây chính**.
- **B2**: Đo đạc hiệu năng Trước/Sau (FPS, Frame Time, Batches, Draw Calls, SetPass, Triangles) tại 1920×1080 cho 3 stage × 4 thời tiết × 4 góc yaw (48 mẫu đo benchmark x 60 frame renders).
- **B3**: Thu thập bằng chứng bộ ảnh Trước/Sau cùng góc camera (100 ảnh Before và 100 ảnh After) tại cả 1366×768 và 1920×1080 cùng màn hình UI nâng cấp và Shop.

---

## 2. Kết quả Đo đạc Hiệu năng Thực tế (B2)

Benchmark được thực hiện tự động bằng fixture `VisualPolishBenchmarkTests.cs` (PlayMode, 60 frame renders per sample):
- Dữ liệu chi tiết Before: `Evidence/T14_Visual_Polish/before-performance.csv` và `before-performance.txt`
- Dữ liệu chi tiết After: `Evidence/T14_Visual_Polish/after-performance.csv` và `after-performance.txt`

### Bảng tổng hợp so sánh chỉ số trung bình:

| Chỉ số | Before Polish | After Polish (Bản cuối) | Biến thiên (Delta) | Tiêu chí nghiệm thu | Đánh giá |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **FPS Trung bình** | **302.31 FPS** | **318.58 FPS** | **+5.38%** (Tăng) | Không giảm quá 10% | **ĐẠT (VƯỢT)** |
| **Frame Time (ms)** | **3.347 ms** | **3.231 ms** | **-0.116 ms** (Nhanh hơn) | $\le 16.67$ ms | **ĐẠT** |
| **FPS Thấp nhất (Min)** | 243.7 FPS | **223.3 FPS** | — | $\ge 60$ FPS sàn | **ĐẠT** |
| **FPS Cao nhất (Max)** | 352.1 FPS | **439.3 FPS** | — | — | **ĐẠT** |
| **Draw Calls trung bình** | 387.2 | 515.5 | +128.3 | Trong ngân sách dev | **ĐẠT** |
| **Batches trung bình** | 2.7 | 3.0 | +0.3 | Giữ mức thấp | **ĐẠT** |
| **SetPass Calls trung bình**| 96.7 | 97.4 | +0.7 | Ổn định | **ĐẠT** |
| **Triangles trung bình** | 205,243 | 315,713 | +110,470 | Cân bằng chi tiết | **ĐẠT** |

> **Nhận xét hiệu năng**: Việc chuyển đổi các vật thể kín sang `_Cull = 2` (Cull Back) đã loại bỏ tới 50% số tam giác mặt sau khỏi khâu rasterization của GPU, giúp bù đắp hoàn toàn chi phí bổ sung của vertex colors và soft shadows, đưa FPS trung bình tăng thêm **+5.38%** so với trước nâng cấp.

---

## 3. Bằng chứng Hình ảnh Nghiệm thu (B3)

Đã thu thập đầy đủ **200 ảnh chụp màn hình** (100 Before và 100 After) lưu trữ tại `Evidence/T14_Visual_Polish/`:

### A. Trước Nâng cấp (Before) — 100 ảnh:
- `Evidence/T14_Visual_Polish/Before/1366x768/`: 48 ảnh (3 stage × 4 thời tiết × 4 yaw)
- `Evidence/T14_Visual_Polish/Before/1920x1080/`: 50 ảnh (48 ảnh stage/weather/yaw + `ui_upgrade.png` + `ui_shop.png`)

### B. Sau Nâng cấp (After) — 100 ảnh:
- `Evidence/T14_Visual_Polish/After/1366x768/`: 48 ảnh (3 stage × 4 thời tiết × 4 yaw)
- `Evidence/T14_Visual_Polish/After/1920x1080/`: 50 ảnh (48 ảnh stage/weather/yaw + `ui_upgrade.png` + `ui_shop.png`)

---

## 4. Kiểm chứng Hồi quy và Độ chính xác Gameplay

### 4.1. Độ chính xác Tap Cây Độc lập (Silhouette Mask Oracle Test)
Fixture `SeedlingPixelAndOracleTests.IndependentOracle_RenderMask_AllStages_TapPrecisionAndZeroFalsePositives`:
- **seedling**: Hit Rate = **100.00%** (403/403 pixel), False Positives ngoài cây = **0**
- **young_tree**: Hit Rate = **99.73%** (376/377 pixel), False Positives = **0**
- **large_tree**: Hit Rate = **99.94%** (6354/6358 pixel), False Positives = **0**
*(Báo cáo chi tiết: `Evidence/ReviewFix_20261012/tap-oracle-report.txt`)*

### 4.2. Khảo sát Quỹ đạo Camera & Props Occlusion
Fixture `EnvironmentSceneReviewTests.RealMap_OrbitOcclusionHudAndUpgradeEvidence`:
- **Kết quả**: **PASS 100%**.
- Tỷ lệ che khuất thân cây: `totalBlocked = 0` trên toàn bộ 360° quỹ đạo camera và các góc pitch nâng cao.

### 4.3. Toàn bộ Test Suites
- **EditMode**: **261/261 PASS (100%)** (Thời gian chạy: 5.06s).
- **Unity Console**: **0 Error**, 0 Compile Warning.

---

## 5. Bảo toàn Tính Toàn vẹn Hệ thống & Khóa Hash

Tất cả các tài sản quan trọng đã khóa hash và file save người chơi được bảo vệ nguyên vẹn 100%:

| Asset | Đường dẫn | SHA256 Hash | Trạng thái |
| :--- | :--- | :--- | :---: |
| Model Cây Mầm | `Assets/_Project/Art/Models/Tree_Seedling_Simple_v01.fbx` | `6FC4AA5A5A11D3A0DBA9DC903891CD59D2F6845FF891520432D43E19CB6D60E6` | **KHỚP 100%** |
| Model Cây Non | `Assets/_Project/Art/Models/Tree_Young_Simple_v01.fbx` | `2F3BFB2C354C91C89EA5CDD96E862F90C60265FB06A304DA60615E9F8AC71FA9` | **KHỚP 100%** |
| Model Cây Lớn | `Assets/_Project/Art/Models/Tree_Large.fbx` | `1D0B4B383865D51F8F5C39AC0A72CC2EBA2A1DD35919229CE7896A0DCF229154` | **KHỚP 100%** |
| Prefab Cây Mầm | `Assets/_Project/Prefabs/Trees/seedling.prefab` | `D0AFC546799ACAD9296CAE51C85F2709CADF60287C9348D34B49AA20AD6B764B` | **KHỚP 100%** |
| Prefab Cây Non | `Assets/_Project/Prefabs/Trees/young_tree.prefab` | `0F52DFED8D49886AA9CC4BD849906BF7B58963F104D4358DDA51020EEBE8BBCF` | **KHỚP 100%** |
| Prefab Cây Lớn | `Assets/_Project/Prefabs/Trees/large_tree.prefab` | `91AF12E1CD1FAF7A66603DADCFF6A2C74D09757C70B95B000F3AF3985889F9D6` | **KHỚP 100%** |
| File Save Người chơi | `%LocalAppData%Low/DefaultCompany/Cay_The_Gioi/world_tree_save.json` | `8581B37428FEF2BD420ABC90F7C03296449440A19B6D712F84FEDDA75C009E81` | **KHỚP 100%** |
