# Công việc hiện tại

Con trỏ tổng hợp từ file task. Không sửa trạng thái riêng tại đây.

Task: [T13 Trang trí môi trường bản đồ Nhiệt đới huyền ảo](Docs/Tasks/13_Tropical_Fantasy_Environment.md)
Trạng thái: IN_PROGRESS
Phụ thuộc: T11 (DONE), T12 (DONE)

## Mục tiêu

Trang trí bản đồ môi trường gameplay của Cây Thế Giới theo phong cách "Nhiệt đới huyền ảo" (Tropical Fantasy) dựa trên concept art đã được người dùng lựa chọn (`C:\Users\Asus\.codex\generated_images\01a129f0-1efe-7a21-8050-7eb6ba9a5020\exec-d9dcd018-3ced-4167-9936-ec8bd131c172.png`).

Ưu tiên hoàn thiện mặt đất hình cầu lớn và trang trí map. Bầu trời để người dùng lựa chọn sau (không đổi skybox, không import/tải AllSky, không tạo chu kỳ ngày/đêm hay bộ điều khiển ánh sáng cạnh tranh).

Bảo vệ tuyệt đối 3 model cây chính hiện có (`seedling`, `young_tree`, `large_tree`), logic gameplay, camera framing, raycast input, animation, thời tiết và dữ liệu save.

## Definition of Done

Hoàn tất checklist trong file task và AGENTS.md: compile, Unity Play Mode, acceptance, hồi quy, Validation PASS, bằng chứng và bàn giao.

## Acceptance hiện tại

- Nền hình cầu lớn nhìn rõ độ cong trong mọi góc nhìn camera gameplay.
- Môi trường toát lên phong cách nhiệt đới huyền ảo nhất quán với ảnh concept đã chọn.
- Cây chính không bị thay đổi mesh/texture/material/animation; 3 stage đều đứng vững trên mặt đất, không lơ lửng hay ngập rễ.
- Tap chuột / touch vào cây chính nhạy bén và chuẩn xác trên cả 3 stage; không click xuyên đá vào cây và không bị đất/cỏ che khuất.
- Giữ nguyên vẹn toàn bộ hệ thống thời tiết, VFX cây, âm thanh, UI, economy và save.
- Cả ba độ phân giải 1920×1080, 1366×768 và 2400×1080 hiển thị đẹp mắt, không lỗi visual hay camera clipping.

## Lỗi và bàn giao

### Nghiệm thu Hậu kiểm ReviewFix 12/10/2026 (Phần A)

1. **Hiệu năng Collider động & Loại bỏ Recook MeshCollider mỗi frame (D50)**:
   - Đo đạc thực tế 1800 frames benchmark (`Evidence/ReviewFix_20261012/collider-benchmark.csv` và `.txt`).
   - Tắt recook PhysX MeshCollider per-frame theo D50, mở rộng static bounds +0.3m và đồng bộ Transform qua `Physics.SyncTransforms()`.
   - Tối ưu CPU vertex loop khử 124,440 phép tính sin/cos mỗi frame; đưa p95 frame time animation từ 22.9 ms xuống < 0.93 ms (đạt 100% ngân sách <= 1.0 ms).
2. **Khắc phục Back-face Vỏ Hạt SeedShell & Viết Test Oracle Độc lập**:
   - Bỏ `queriesHitBackfaces = true` toàn cục. Scope cục bộ trong `TreeInputAdapter.OnTree` và hỗ trợ đầy đủ vật thể hai mặt (vỏ hạt SeedShell).
   - Pixel (642,307) của seedling nhận tap 100%, có ảnh crop bằng chứng tại `seedling_pixel_642_307_crop.png`.
   - Viết test oracle độc lập bằng RenderTexture Z-buffer mask (`SeedlingPixelAndOracleTests.cs`): Đạt 100% hit rate ở Seedling và Young Tree, 99.95% ở Large Tree, 0 false positives.
3. **Hoàn thiện UI và Weather Visual**:
   - Stage hiển thị tiếng Việt ("Mầm", "Cây Non", "Cây Lớn", "Cây Trưởng Thành").
   - Giông bão: ambient tối, fog giông, sky tint, mưa hạt mảnh có near-clip/spawn bám quanh tán cây tránh che màn hình.
   - Bố cục HUD: đầy đủ 4 icon thời tiết, dời nút Orbit ra góc dưới HUD, căn chỉnh chữ "Sẵn sàng" và thẻ Linh khí "Chưa mở" không cắt chữ.
   - Xuất bộ screenshots bản cuối: `thunderstorm_1920x1080.png`, `ui_1366x768.png`, `ui_1920x1080.png`, `ui_2400x1080.png`.
4. **Kiểm chứng Toàn bộ**:
   - EditMode: 261/261 PASS. PlayMode: 100% PASS (bao gồm `AnimatedTreeReceivesMouseTapsAcrossAllStagesAndCalmStorm`). Console 0 error.
   - Bảo toàn 100% hash SHA256 của 6 asset cây chính và file save thật của người chơi.

## Tiếp theo

T14. Chỉ bắt đầu sau khi task hiện tại DONE.
