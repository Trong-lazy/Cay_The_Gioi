# Nghiệm thu và Khắc phục Lỗi Hậu kiểm Độc lập — 12/10/2026

Task: T13
Result: IN_PROGRESS
Ngày: 12/10/2026
Môi trường: Unity 6000.6.1f1, URP 17.0.3, Windows x64, Unity MCP kết nối đầy đủ

---

## 1. Khôi phục Phiên thử An toàn (Safe Session Recovery)

- **Trạng thái Editor**: Dừng Play Mode an toàn (`EditorApplication.isPlaying = false`).
- **Khôi phục overrides**: Reset hoàn toàn `SaveService.ValidationDirectory` và `SaveService.ValidationUtcSeconds` về `null`. Editor runtime không còn trỏ vào thư mục save thử nghiệm `Evidence/TapFix_Recheck_20261012/ManualSave`.
- **Bảo toàn save thật**: File save thật của người chơi tại `AppData/LocalLow/DefaultCompany/Cay_The_Gioi/`:
  - `world_tree_save.json`: `8581B37428FEF2BD420ABC90F7C03296449440A19B6D712F84FEDDA75C009E81` (Khớp 100% với bản ghi `protected-before.json`)
  - `world_tree_save.backup.json`: `3FFE3FA332A9F67DBBCE36686DB328C41B34A8DE946BCD7A27272EB5BCECA5BC` (Khớp 100%)
- **Bảo toàn 6 asset cây chính**:
  - `Assets/_Project/Art/Models/Tree_Large.fbx`: `1D0B4B383865D51F8F5C39AC0A72CC2EBA2A1DD35919229CE7896A0DCF229154` (Khớp 100%)
  - `Assets/_Project/Art/Models/Tree_Seedling_Simple_v01.fbx`: `6FC4AA5A5A11D3A0DBA9DC903891CD59D2F6845FF891520432D43E19CB6D60E6` (Khớp 100%)
  - `Assets/_Project/Art/Models/Tree_Young_Simple_v01.fbx`: `2F3BFB2C354C91C89EA5CDD96E862F90C60265FB06A304DA60615E9F8AC71FA9` (Khớp 100%)
  - `Assets/_Project/Prefabs/Trees/large_tree.prefab`: `91AF12E1CD1FAF7A66603DADCFF6A2C74D09757C70B95B000F3AF3985889F9D6` (Khớp 100%)
  - `Assets/_Project/Prefabs/Trees/seedling.prefab`: `D0AFC546799ACAD9296CAE51C85F2709CADF60287C9348D34B49AA20AD6B764B` (Khớp 100%)
  - `Assets/_Project/Prefabs/Trees/young_tree.prefab`: `0F52DFED8D49886AA9CC4BD849906BF7B58963F104D4358DDA51020EEBE8BBCF` (Khớp 100%)
  - Bản ghi đối chiếu lưu tại: `Evidence/ReviewFix_20261012/protected-after.json`.

---

## 2. Hoàn tất Kiểm chứng Phần Tap Interaction (Spherical Trigger Volume)

- **Cấu hình Vùng Cầu**:
  - `TreeView.cs` bật root `SphereCollider` dạng trigger trên cả ba stage (`seedling`, `young_tree`, `large_tree`).
  - Stage Mầm và Cây Non sử dụng tâm và bán kính cấu hình từ `GameplayInputConfig`.
  - Stage Cây Lớn tự động tính bán kính từ bounds MeshRenderer kèm padding `0.08m` (vùng ổn định theo stage, không recook per-frame).
  - `TreeInputAdapter.OnTree` chủ động truy vấn vùng cầu trigger này trước khi kiểm tra mesh polygon, bỏ qua các trigger trang trí khác và giữ nguyên cơ chế chặn từ collider rắn phía trước.
- **Tiêu chí nghiệm thu Tap mới**:
  - Khe lá/khoảng trống bên trong vùng cầu được nhận tap có chủ ý; không bắt buộc nhắm trúng từng polygon mỏng của lá hay cành.
  - Vị trí bên ngoài cả mô hình cây và vùng cầu tuyệt đối không nhận tap (0 false positives).
- **Kết quả Kiểm tra Thực tế**:
  - `TapInteractionReviewTests.FiftyRealGesturesRewardExactlyOnceAcrossStagesAndYawWithAnimation`: 50 gestures (25 chuột + 25 touch) trên 5 góc yaw (0°, 72°, 144°, 216°, 288°) cho cả 3 stage đều đạt đúng 1 reward 20s và tăng tỷ lệ Water lên 1.05x. Log chi tiết tại `Evidence/TapFix_Recheck_20261012/fifty-tap-results.txt`.
  - `TapInteractionReviewTests.SphericalGapsAcceptTapsFromEveryYawAndBlockOutsideAndFrontOccluder`: PASS. Gaps trong cầu nhận tap ở cả 2 thời tiết (calm, storm) và 4 góc yaw (0°, 90°, 180°, 270°); vật cản rắn phía trước chặn tap thành công; nền xa không nhận tap.
  - `ReviewFixVerificationTests.TapInteraction_SphericalVolume_AcceptsGaps_BlocksOutsideAndOccluder`: PASS trên cả 3 stage.
  - Native click lệch thân Mầm (pixel 642, 307) nhận tap chuẩn xác, xác minh qua `SeedlingPixelAndOracleTests.Probe_SeedlingPixel_642_307_RaycastAndCrop` (PASS).
  - Oracle độc lập bằng RenderTexture Z-buffer ID mask (`IndependentOracle_RenderMask_AllStages_TapPrecisionAndZeroFalsePositives`): Đạt 100.00% hit rate trên cả 3 stage, 0 false positive ngoài vùng cây + cầu.

---

## 3. Sửa Lỗi Tutorial Banner Che HUD

- **Hiện tượng**: Ở độ phân giải Game View 1366×768 (và 1920×1080), banner hướng dẫn Bước 1 ("Tích lũy Sức sống") đè lên các thẻ tài nguyên phía trên (WaterCard, SunshineCard... ở vị trí $y = -120$ đến $-216$).
- **Khắc phục**:
  - Trong `ReferenceUiLayout.cs`:
    * Khi ở Bước 1 (TargetElement == "tree"): Banner tự động dock sang bên phải vùng cầu tap của Cây Mầm (`bannerX = center + 140 + 12`, `bannerY = -226`, kích thước $480 \times 116$). Banner nằm hoàn toàn phía dưới các thẻ tài nguyên ($y \le -216$), đồng thời nằm ngoài biên phải của hình chiếu vùng cầu tap cây mầm ($x > screenCenter.x + screenRadius$).
    * Khi mở panel nâng cấp (UpgradePanelView.IsOpen): Banner tự động neo vào vùng viewport trống bên trái ($y = -226$), không che khuất panel nâng cấp ($x + w \le panelLeftEdge$).
    * Đặt `bannerImg.raycastTarget = false` trên Banner Image, triệt tiêu hoàn toàn rủi ro banner chặn tia raycast tap vào cây.
  - Trong `TutorialView.cs`: Thêm guard kiểm tra `ReferenceUiLayout` trong `LateUpdate` để tránh ghi đè vị trí neo đã tối ưu.
- **Bảo toàn**: Giữ nguyên bố cục HUD, thanh trạng thái, thẻ tài nguyên và giá nguyên liệu nâng cấp.
- **Bằng chứng Ảnh chụp Thực tế**:
  - `Evidence/ReviewFix_20261012/proof_tutorial_step1_1366x768.png`: 1366×768, banner nằm dưới card tài nguyên, bên phải cây mầm.
  - `Evidence/ReviewFix_20261012/proof_tutorial_step1_1920x1080.png`: 1920×1080, không chồng lấn.
  - `Evidence/ReviewFix_20261012/proof_tutorial_step2_upgrade_open_1366x768.png`: 1366×768 khi mở panel nâng cấp, banner nằm bên trái gọn gàng.

---

## 4. Khắc phục Chuyển màu Địa hình (Terrain Vertex Colors)

- **Nguyên nhân**: `SphericalWorldTerrainBuilder` sinh ra 7,204 đỉnh có màu vertex color (`mesh.SetColors(colors)`), nhưng 4 material của địa hình (`M_Tropical_Grass`, `M_Warm_Dirt`, `M_Sandy_Shore`, `M_Lake_Bed`) sử dụng shader mặc định `Universal Render Pipeline/Lit`. Shader URP Lit mặc định không có semantic input `COLOR` trong vertex attributes và pass forward, dẫn đến toàn bộ dữ liệu màu đỉnh bị bỏ qua khi render.
- **Giải pháp**:
  - Tạo shader chuyên dụng `Assets/_Project/Art/Environment/Shaders/TerrainLit.shader` (`Universal Render Pipeline/Custom/TerrainLit`).
  - Vertex shader nhận `float4 color : COLOR;` và truyền qua `Varyings.color`.
  - Fragment shader hòa trộn màu đỉnh trực tiếp vào albedo:
    $$\text{albedo} = \_BaseColor.rgb \times texColor.rgb \times input.color.rgb$$
  - Tích hợp đầy đủ ánh sáng PBR URP (Directional Light, UniversalLight, Ambient Spherical Harmonics, Shadows, Fog).
  - Cập nhật `TropicalEnvironmentSetup.cs` gán shader `Universal Render Pipeline/Custom/TerrainLit` cho cả 4 material địa hình.
  - Loại bỏ lệnh can thiệp vật liệu lá cây `PolishTreeLeafMaterials()`. Không cài thêm package ngoài, không thay đổi model cây.
- **Kiểm chứng**:
  - Test PlayMode `ReviewFixVerificationTests.TerrainVertexColors_RenderedWithDedicatedTerrainLitShader`: PASS (xác minh mesh có > 0 vertex colors, 4 material đều dùng shader `TerrainLit`).
  - Ảnh chụp thực tế: `Evidence/ReviewFix_20261012/proof_terrain_vertex_color_lit_1366x768.png` (hiển thị rõ ràng sự chuyển màu mềm mại giữa thảm cỏ xanh, dải đất nâu ấm và bãi cát).

---

## 5. Xử lý Lỗi Render và Báo cáo Hiệu năng

### 5.1. Triệt tiêu Lỗi Render Attachment Mismatch
- **Hiện tượng**: Console ghi nhận lỗi `UberPostProcess/Draw UIToolkit/uGUI Overlay: dimensions or sample count ... (1918 x 1006) vs (1366 x 768)` và `NextSubPass/EndRenderPass: Not inside a Renderpass` trong lúc chạy test chụp nhiều độ phân giải.
- **Nguyên nhân**: Harness chụp ảnh trước đây gán trực tiếp `cam.targetTexture = rt` vào `rig.SceneCamera` trong lúc Game View đang hiển thị ở kích thước cửa sổ Editor (1918×1006). Khi URP render overlay uGUI/UIToolkit, RTHandle nội bộ bị lệch kích thước bộ đệm, làm vỡ RenderPass.
- **Khắc phục**:
  - Sử dụng camera tạm độc lập `CaptureCamTemp` (`new GameObject("CaptureCamTemp", typeof(Camera))`), sao chép thông số qua `captureCam.CopyFrom(cam)` và chỉ gán `targetTexture` vào camera tạm.
  - Tuyệt đối không can thiệp vào `rig.SceneCamera.targetTexture`.
  - Sau khi chụp xong, khôi phục `hudCanvas.worldCamera` và hủy hoàn toàn camera tạm.
  - Đã áp dụng trên cả `VisualPolishBenchmarkTests.cs`, `WeatherAtmosphereAndUiResolutionTests.cs` và `ReviewFixVerificationTests.cs`.
  - Gỡ bỏ hoàn toàn cờ che lỗi `LogAssert.ignoreFailingMessages = true;`.
- **Kết quả**: Console hoàn toàn sạch lỗi native attachment và renderpass trong toàn bộ quá trình chạy test.

### 5.2. Chuẩn hóa Báo cáo Hiệu năng T14
- **Điều chỉnh Harness**: Thay thế vòng lặp 60 lần `cam.Render()` liên tiếp trong 1 frame bằng vòng lặp thực qua nhiều frame với `yield return null`, đo `unscaledDeltaTime` và `realtimeSinceStartup` của chu trình game loop thực tế.
- **Phạm vi Báo cáo**: Ghi rõ đây là số đo trong Editor PlayMode trên máy dev Windows x64 (không phải Standalone build hay thiết bị mobile thật).
- **Rút lại kết luận chưa đủ căn cứ**: Đã sửa nhãn và nội dung báo cáo, không khẳng định "$\ge 60$ FPS vòng chơi thực tế" hoặc "tăng 5.38%" khi chưa có dữ liệu Standalone / Device profiling.

---

## 6. Kết quả Kiểm thử Tổng thể

### 6.1. EditMode Suite
- **Kết quả**: **261/261 PASS (100%)**, 0 FAIL, 0 SKIP.
- **Bản ghi**: `Evidence/ReviewFix_20261012/EditMode_Results_20261012.json` (Job `6192ab18ba4e4916b775370d27517113`).

### 6.2. PlayMode Suite (Full 86 Test Cases)
- **Lượt 1** (trước khi sửa event queue của mouse bitfield): 84 PASS, 1 FAIL, 1 Ignored (`PlayModeResults_Full_20261012.xml`).
- **Lượt 2 — Hoàn tất** (sau khi chuẩn hóa `InputSystem.QueueStateEvent`):
  - **85 PASS, 0 FAIL, 1 Ignored** (Ignored: `T14_Run_Baseline_Benchmark_And_Screenshots` do baseline đã được ghi nhận trước đó).
  - **Bản ghi**: `Evidence/ReviewFix_20261012/PlayModeResults_Final.xml`.
  - `ReviewFixVerificationTests`: 3/3 PASS.
  - `TapInteractionReviewTests`: 2/2 PASS.
  - `SeedlingPixelAndOracleTests`: 2/2 PASS.
  - `SaveSceneTests`: 12/12 PASS.
  - `UpgradeSceneTests`: 7/7 PASS.
  - `WeatherSceneTests`: 10/10 PASS.
  - `TutorialSceneTests`: 5/5 PASS.
  - `AchievementSceneTests`: 3/3 PASS.
  - `ReferenceUiSceneTests`: 4/4 PASS.
  - `WeatherAtmosphereAndUiResolutionTests`: 2/2 PASS.
  - `VisualPolishBenchmarkTests`: 1 PASS, 1 Ignored.

---

## 7. Trạng thái Task & Bàn giao Trung thực

- **T13**: Giữ `IN_PROGRESS` (tiếp tục hoàn thiện các chi tiết môi trường và kiểm định độc lập).
- **T14**: Giữ `WAITING_VALIDATION` (chờ đánh giá lại toàn diện đồ họa và hiệu năng sau khi T13 hoàn tất).
- **Công cụ tiến độ**: Đã cập nhật `Tools/Docs/update_progress.py`, `Progress.md` (17/19 DONE) và `CURRENT_TASK.md` (con trỏ trỏ đúng T13).
- **Giới hạn kiểm thử**: Mọi kiểm thử được thực hiện trong Unity 6000.6.1f1 Editor trên nền Windows x64. Chưa kiểm thử trên thiết bị di động thật hay build Standalone mới.
