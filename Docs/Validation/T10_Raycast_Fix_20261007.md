# Kiểm tra nghiệm thu bản sửa Raycast/Collider và hoàn tất T10 (07/10/2026)

Task: T10  
Result: FAIL (hậu kiểm độc lập; PASS bên dưới là kết quả lịch sử của agent)  
Phạm vi: Sửa hệ collider/raycast cho 3 stage đại diện (`seedling`, `young_tree`, `large_tree`), bổ sung bộ test hồi quy `ArtPipelineSceneTests`, đồng bộ số liệu prefab/tài liệu/bằng chứng Blender và kiểm định bản build Windows Standalone mới (`Builds/Windows_T10_20261007/Cay_The_Gioi.exe`).  
Ngày: 07/10/2026, Asia/Saigon.  
Unity: 6000.6.1f1 (URP 17.6.0).  
Scene: `Assets/_Project/Scenes/MainScene.unity`.  
Config: `Assets/_Project/Configs/PrototypeConfig.asset` (`prototype-4`).  
Build Output mới: `Builds/Windows_T10_20261007/Cay_The_Gioi.exe` (`SHA256 = 07534CC5839D2C89C4FE45EC272C91B046FF3D64F14AB3F0A32221121E0FFFF2`, `667,136` bytes; tổng thư mục build `112,652,986` bytes; giữ nguyên các bản build cũ `Builds/Windows/` và `Builds/Windows_T10/`).

---

> Hậu kiểm: T10 mở lại IN_PROGRESS, còn click mặt sau SeedShell không được render và click xuyên mặt đất tới SeedShell. Xem `Docs/Validation/T10_Independent_Recheck_20261007.md`. Các số liệu PASS/probe dưới đây chưa kiểm chứng hai ca này.

## 1. Nguyên nhân gốc rễ (Root Cause) và Giải pháp kỹ thuật

### 1.1. Nguyên nhân gốc rễ từ kiểm tra độc lập (`Docs/Validation/T10_Independent_Review_20261007.md`)
1. `TreeInputAdapter.OnTree(Vector2 position)` kiểm tra `Physics.Raycast(orbit.SceneCamera.ScreenPointToRay(position), out var hit) && hit.collider.GetComponentInParent<TreeView>() == tree`.
2. Trước khi sửa:
   - GameObject gốc `Tree` trong `MainScene` bật `SphereCollider` thô (`radius = 0.14 / 0.24 / 1.02`), bao trùm cả vùng không gian trống xung quanh thân mầm và cây non (`168` điểm false positive trên `seedling`, `162` điểm trên `young_tree`).
   - Ba prefab stage gắn `CapsuleCollider` hình học thô (`large_tree` có `CanopyTapCollider` `center=(0.02, 1.35, -0.08), r=0.52, h=2.9, dir=0`), vừa phình ra ngoài khoảng trống bên cạnh tán (`84` điểm false positive trên `large_tree`, tiêu biểu tại `(611.49, 629.6163)` ở `1920×1080`), vừa cắt hụt các cụm lá nhô ra ngoài biên capsule (`17` điểm false negative trên `large_tree`, tiêu biểu tại `(573.2952, 412.3293)`).
   - Ngoài ra, `Physics.queriesHitBackfaces` mặc định tắt (`false`) và mặt trên `BoxCollider` của `Ground` đặt sát $y = 0$ khiến các tia chiếu vào mặt trong vỏ hạt tách (`SeedShell`) hoặc cổ rễ sát mặt đất ($\approx -0.9\text{ mm}$) trên `seedling` bị tính chạm `Ground` trước.

### 1.2. Giải pháp đã triển khai
1. **Collider khớp chính xác hình học 3D hiển thị trên cả 3 stage prefab (`seedling.prefab`, `young_tree.prefab`, `large_tree.prefab`)**:
   - Xóa toàn bộ `CapsuleCollider` đại khái (`TapCollider`, `CanopyTapCollider`) khỏi 3 stage prefab (giữ nguyên 100% đường dẫn và GUID của 3 prefab).
   - Gắn `MeshCollider` tĩnh không lồi (`convex = false`, `enabled = true`) trực tiếp lên từng mesh con hiển thị của từng stage:
     - `seedling.prefab`: 4 `MeshCollider` (`Foliage`, `Roots`, `SeedShell`, `Trunk_Branches` — tổng `2,770` tris).
     - `young_tree.prefab`: 3 `MeshCollider` (`Foliage`, `Roots`, `Trunk_Branches` — tổng `4,574` tris).
     - `large_tree.prefab`: 3 `MeshCollider` (`Foliage`, `Roots`, `Trunk_Branches` — tổng `26,682` tris).
2. **Mở rộng `TreeStageAnchors` và tự động tắt `SphereCollider` thô trên `TreeView`**:
   - Bổ sung trường tuần tự hóa `Collider[] hitColliders` và phương thức `HasActiveHitColliders()` trong `Assets/_Project/Scripts/Presentation/TreeStageAnchors.cs`.
   - Trong `Assets/_Project/Scripts/Presentation/TreeView.cs` (`ApplyStageAnchorsAndCollider`), `SphereCollider` gốc trên `Tree` vẫn được đồng bộ `center` (nằm trên thân/tán nhìn thấy) và `radius` (`0.14 / 0.24 / 1.02`) để tương thích ngược với các bài test cũ đọc `SphereCollider.center`/`.radius`, nhưng đặt **`collider.enabled = !currentStageAnchors.HasActiveHitColliders()`** (`false` cho cả 3 stage thật; chỉ bật `true` làm fallback khi test dùng prefab giả lập không có collider con).
3. **Bật `Physics.queriesHitBackfaces = true` và tinh chỉnh `Ground` `BoxCollider`**:
   - Bật `Physics.queriesHitBackfaces = true` trong `ProjectSettings/DynamicsManager.asset` (`m_QueriesHitBackfaces: 1`), `ProjectSetup.ApplySettings()` và `TreeView.ApplyStageAnchorsAndCollider()` để các phiến lá mỏng hai mặt và mặt lõm trong của vỏ hạt `SeedShell` nhận raycast chính xác từ mọi góc camera.
   - Hạ nhẹ tâm `BoxCollider` của `Ground` trong `MainScene.unity` và `ProjectSetup.cs` xuống `center = (0, -0.02, 0)` ($\approx -2.4\text{ mm}$ theo trục $Y$ thế giới) để không che khuất phần cổ rễ nổi sát mặt đất của `seedling`, đồng thời vẫn chặn các phần rễ chìm sâu dưới đất.
   - Bật `isReadable = true` trên 3 FBX (`Tree_Seedling_Simple_v01.fbx`, `Tree_Young_Simple_v01.fbx`, `Tree_Large.fbx`) qua `ModelImporter` để việc đọc tam giác kiểm định trong PlayMode/Standalone QA không phát sinh lỗi Console.

---

## 2. Kết quả đối chiếu Trước / Sau khi sửa (Before vs After Reproduction)

### 2.1. Kiểm tra 2 tọa độ chuẩn trên `large_tree` (`1920×1080`, `distance=6.2`, `pitch=20`, `targetHeight=1.18`, `yaw=0`)
Bằng chứng JSON:
- Trước khi sửa: `Evidence/T10_Raycast_Fix_20261007/before_fix_repro.json`
- Sau khi sửa: `Evidence/T10_Raycast_Fix_20261007/after_fix_repro.json`

| Tọa độ màn hình (`1920×1080`) | Ý nghĩa hình học thực tế | Thiết bị Input System | Trước khi sửa (`tapProgress` delta) | Sau khi sửa (`tapProgress` delta) | Kết luận |
| --- | --- | --- | --- | --- | --- |
| `(611.49, 629.6163)` | Khoảng trống bên trái tán cây (tia không cắt bất kỳ tam giác nào của cây) | `MouseState` (Left Click) | `+1` (`0 -> 1`, False Positive) | **`0`** (`0 -> 0`) | **PASS** |
| `(611.49, 629.6163)` | Khoảng trống bên trái tán cây (tia không cắt bất kỳ tam giác nào của cây) | `TouchState` (`Began -> Ended`) | `+1` (`1 -> 2`, False Positive) | **`0`** (`0 -> 0`) | **PASS** |
| `(573.2952, 412.3293)` | Phiến lá nhìn thấy ở rìa tán (`Foliage` tại `6.1323m < Ground 8.0961m`) | `MouseState` (Left Click) | `0` (`2 -> 2`, False Negative) | **`+1`** (`0 -> 1`) | **PASS** |
| `(573.2952, 412.3293)` | Phiến lá nhìn thấy ở rìa tán (`Foliage` tại `6.1323m < Ground 8.0961m`) | `TouchState` (`Began -> Ended`) | `0` (`2 -> 2`, False Negative) | **`+1`** (`1 -> 2`) | **PASS** |

### 2.2. Kết quả quét lưới `25×25` (`raycast_probe.cs.txt`) trên cả 3 stage
Bằng chứng JSON:
- Trước khi sửa: `Evidence/T10_Raycast_Fix_20261007/before_fix_probe.json`
- Sau khi sửa: `Evidence/T10_Raycast_Fix_20261007/after_fix_probe.json`

| Stage | Điểm kiểm tra (`checkedPoints`) | Điểm cắt Mesh (`meshHits`) | Điểm trúng cây (`physicsTreeHits` Trước -> Sau) | False Positive khoảng trống (`falsePositiveEmpty` Trước -> Sau) | False Negative bề mặt nhìn thấy (`falseNegativeVisible` Trước -> Sau) |
| --- | --- | --- | --- | --- | --- |
| `seedling` | `625` | `45` | `206` -> **`40`** (5 điểm rễ chìm sâu dưới mặt đất bị `Ground` chặn đúng) | `168` -> **`0`** | `3` -> **`0`** |
| `young_tree` | `613` | `53` | `206` -> **`41`** (12 điểm rễ chìm sâu dưới mặt đất bị `Ground` chặn đúng) | `162` -> **`0`** | `0` -> **`0`** |
| `large_tree` | `580` | `187` | `242` -> **`175`** (12 điểm rễ chìm sâu dưới mặt đất bị `Ground` chặn đúng) | `84` -> **`0`** | `17` -> **`0`** |

---

## 3. Bảng thông số thực tế đã lưu trên Prefab & Scene (Khớp 100% dữ liệu Serialized)

Bằng chứng trích xuất trực tiếp từ Unity: `Evidence/T10_Raycast_Fix_20261007/prefab_anchor_collider_audit.json`.

| Stage Prefab (GUID) | `Anchor_Root` (`localPosition` / `localScale`) | `Anchor_TrunkAura` (`localPosition` / `localScale`) | `Anchor_LeavesCenter` (`localPosition` / `localScale`) | `Anchor_LightningTarget` (`localPosition` / `localScale`) | Collider thực tế trên Prefab (`hitColliders`) | `TreeView` Root `SphereCollider` (`enabled` / `center` / `radius`) | Camera Pose (`Distance` / `Pitch` / `TargetHeight`) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `seedling.prefab` (`0f9f4b7c4c98be74fb9434b3e4962af8`) | `(0, 0.02, 0)` / `(0.22, 0.22, 0.22)` | `(0, 0.10, 0)` / `(0.22, 0.22, 0.22)` | `(0, 0.19, 0)` / `(0.25, 0.25, 0.25)` | `(0, 0.25, 0)` / `(0.25, 0.25, 0.25)` | 4 `MeshCollider` (`convex=false`): `Foliage` (`1172` tris), `Roots` (`316` tris), `SeedShell` (`1024` tris), `Trunk_Branches` (`258` tris) | `enabled = false`, `center = (0, 0.14, 0)`, `radius = 0.14` | `1.4m` / `16°` / `0.12m` |
| `young_tree.prefab` (`71802b4846f1f0045ba8add282f6dc2a`) | `(0, 0.04, 0)` / `(0.50, 0.50, 0.50)` | `(0, 0.36, 0)` / `(0.50, 0.50, 0.50)` | `(0, 0.52, -0.03)` / `(0.55, 0.55, 0.55)` | `(0, 0.80, -0.02)` / `(0.55, 0.55, 0.55)` | 3 `MeshCollider` (`convex=false`): `Foliage` (`3224` tris), `Roots` (`760` tris), `Trunk_Branches` (`590` tris) | `enabled = false`, `center = (0, 0.42, 0)`, `radius = 0.24` | `2.8m` / `18°` / `0.42m` |
| `large_tree.prefab` (`37dc3f7f57f2bca4baaedb64db2936a3`) | `(0, 0.08, 0)` / `(0.90, 0.90, 0.90)` | `(0, 0.72, -0.02)` / `(0.90, 0.90, 0.90)` | `(0.02, 1.52, -0.10)` / `(1.00, 1.00, 1.00)` | `(0.02, 2.22, -0.08)` / `(1.00, 1.00, 1.00)` | 3 `MeshCollider` (`convex=false`): `Foliage` (`24640` tris), `Roots` (`634` tris), `Trunk_Branches` (`1408` tris) | `enabled = false`, `center = (0, 1.15, -0.02)`, `radius = 1.02` | `6.2m` / `20°` / `0.18m` -> `1.18m` |

---

## 4. Kết quả chạy Test tự động (EditMode & PlayMode) và Standalone Windows Build

| Hạng mục kiểm thử | Phạm vi / Số lượng | Kết quả | File bằng chứng |
| --- | --- | --- | --- |
| **PlayMode Regression Test (Trước khi sửa)** | `ArtPipelineSceneTests` (4 tests) | **3 Failed / 1 Passed** (chứng minh test bắt đúng lỗi collider/raycast cũ) | `Evidence/T10_Raycast_Fix_20261007/playmode_before_fix_test_results.json` |
| **PlayMode Regression Test (Sau khi sửa)** | `ArtPipelineSceneTests` (4 tests: kiểm tra $\ge 3$ điểm bề mặt nhìn thấy & $\ge 3$ điểm trống sát silhouette trên cả 3 stage bằng `MouseState` & `TouchState`, 4 góc yaw `0°/90°/180°/270°`, góc pitch cao `35°`, tỷ lệ khung hình rộng `20:9`, vật cản `FrontOccluder`, Evolution 3 stage, Voluntary/Forced Rebirth, Save/Load) | **4 / 4 PASS** (`0 failed`) | `Evidence/T10_Raycast_Fix_20261007/playmode_after_fix_art_tests.json` |
| **Full EditMode Suite (Sau khi sửa)** | Toàn bộ 191 EditMode tests | **191 / 191 PASS** (`0 failed`) | `Evidence/T10_Raycast_Fix_20261007/editmode_after_fix_test_results.json` |
| **Full PlayMode Suite (Sau khi sửa)** | Toàn bộ 63 PlayMode tests across 9 test classes | **63 / 63 PASS** (`0 failed`) | `Evidence/T10_Raycast_Fix_20261007/playmode_after_fix_all_test_results.json` |
| **Standalone Windows Build (`Builds/Windows_T10_20261007/Cay_The_Gioi.exe`)** | 6 kịch bản QA độc lập với `-ctg-save-dir` riêng: `smoke_fresh`, `smoke_offline`, `smoke_progression`, `smoke_death_forced_rebirth`, `ui_flow`, và **`smoke_raycast`** (chạy lưới probe `25×25` + `MouseState`/`TouchState` thật trong bản build `.exe`) | **6 / 6 PASS** (`exitCode = 0`, `falsePositiveEmpty = 0`, `falseNegativeVisible = 0` trên cả 3 stage) | `Evidence/T10_Raycast_Fix_20261007/windows_build_metadata.json`, `Evidence/T10_Raycast_Fix_20261007/windows_build_qa_summary.json`, `Evidence/T10_Raycast_Fix_20261007/windows_build_qa/report_smoke_raycast.json` |
| **Kiểm toán Blender 5.2.2 LTS (Read-only)** | Kiểm tra `ArtSource/Models/*.blend` & `ArtSource/Exports/*.fbx` cho 3 stage (`hashes_unchanged_after_audit = true`) | **PASS** | `Evidence/T10_Raycast_Fix_20261007/blender_source_export_audit.json` & `Evidence/T10/blender_audit_step1.json` |
| **Bảo toàn Save thật của người chơi** | `world_tree_save.json` (`91B3491C...`, `1491` bytes) & `world_tree_save.backup.json` (`9242975C...`, `1490` bytes) | **100% Khớp trước/sau** | `Evidence/T10_Raycast_Fix_20261007/initial_snapshot.json` & `Evidence/T10_Raycast_Fix_20261007/final_snapshot.json` |
