---
id: T13
title: Trang trí môi trường bản đồ Nhiệt đới huyền ảo
status: IN_PROGRESS
depends_on: ["T11", "T12"]
validation: "Docs/Validation/ReviewFix_20261012.md"
---

# T13 Trang trí môi trường bản đồ Nhiệt đới huyền ảo

## Mục tiêu

Trang trí bản đồ môi trường gameplay của Cây Thế Giới theo phong cách "Nhiệt đới huyền ảo" (Tropical Fantasy) dựa trên concept art đã được người dùng lựa chọn (`C:\Users\Asus\.codex\generated_images\01a129f0-1efe-7a21-8050-7eb6ba9a5020\exec-d9dcd018-3ced-4167-9936-ec8bd131c172.png`).

Ưu tiên hoàn thiện mặt đất hình cầu lớn và trang trí map. Bầu trời để người dùng lựa chọn sau (không đổi skybox, không import/tải AllSky, không tạo chu kỳ ngày/đêm hay bộ điều khiển ánh sáng cạnh tranh).

Bảo vệ tuyệt đối 3 model cây chính hiện có (`seedling`, `young_tree`, `large_tree`), logic gameplay, camera framing, raycast input, animation, thời tiết và dữ liệu save.

## Nguyên tắc kỹ thuật và bảo vệ asset

- Không sửa đổi model, mesh, topology, UV, texture, material, rig, GUID, prefab hay controller của 3 stage cây chính.
- Bán kính và bề mặt địa hình đồi cầu khớp hoàn hảo đỉnh (0, 0, 0) với gốc cây; không làm cây lơ lửng hay vùi lấp rễ.
- Cây chính nằm ở trung tâm cao nhất, thông thoáng, không bị bất kỳ props (đá, cỏ, cây bụi) nào che khuất ở các góc nhìn gameplay.
- Gom toàn bộ môi trường vào root `WorldEnvironment`, tách biệt khỏi `Tree` và gameplay systems.
- Bố cục địa hình và props mang tính tất định (deterministic), không random lại mỗi frame.
- Giữ nguyên economy, HP, Protection, xác suất sét, Evolution, Rebirth và bố cục UI.
- Thử nghiệm trên save test riêng biệt, bảo toàn save thật của người chơi.

## Checklist thực hiện

- [x] Chuẩn bị và bảo vệ an toàn:
  - [x] Sao lưu file save thật của người dùng (`world_tree_save.json`) và lưu SHA256 trước khi sửa.
  - [x] Lưu bảng SHA256 của các asset cây chính được bảo vệ trước khi bắt đầu.
- [x] Mặt đất hình cầu lớn (Spherical Planet Terrain):
  - [x] Thay thế đĩa dẹt `Ground` bằng nền địa hình hình học cầu rõ ràng với độ cong nhận diện được từ camera.
  - [x] Đỉnh đồi cầu tại (0, 0, 0) tiếp xúc chính xác với gốc cây của cả 3 stage (`seedling`, `young_tree`, `large_tree`).
  - [x] Bề mặt địa hình phân vùng tự nhiên: thảm cỏ nhiệt đới xanh mướt, lối mòn đất nâu ấm, bờ cát sáng, lòng hồ ngọc lam trũng xuống.
  - [x] Cấu hình collider địa hình chuẩn xác, đảm bảo không click xuyên đất và không che chắn raycast tap cây.
- [x] Bố cục trang trí Nhiệt đới:
  - [x] Vùng cây chính: Mặt cỏ thấp, đất nâu lộ và sỏi mịn bám đất; khoảng thoáng rộng rãi quanh gốc cây.
  - [x] Vùng hồ & thác nước: Hồ nước màu ngọc lam uốn lượn lệch một phía, vách đá tối phủ rêu với thác nước nhỏ nhiều tầng đổ vào hồ; bờ chuyển tiếp mềm mại nước -> cát -> đất ẩm -> cỏ.
  - [x] Vùng đối diện: Cụm đá núi phủ rêu kích thước lớn/vừa/nhỏ phối hợp cùng dương xỉ, cây lá lớn và hoa san hô tạo điểm cân bằng thị giác.
  - [x] Vùng rìa thế giới: Bụi cây nhiệt đới, cây cọ mini viền chân trời tạo chiều sâu và tôn vinh kích thước cây chính.
- [x] Điểm nhấn huyền ảo (Fantasy Accents):
  - [x] Cụm nấm phát sáng dạ quang màu xanh ngọc lam (bioluminescent cyan mushrooms) mọc gần vách đá phủ rêu và mép nước.
  - [x] Đom đóm bay lơ lửng chậm rãi màu vàng ấm nhẹ nhàng, phân tán tự nhiên trong không gian.
  - [x] Hiệu ứng ánh sáng ấm dịu nhẹ, không gây cháy sáng cây chính hoặc trùng lặp với Burning/sét.
- [x] Tương thích gameplay & camera:
  - [x] Kiểm tra camera orbit và framing ở cả 3 stage: cây chính luôn nổi bật, tỷ lệ hợp lý, không bị props che khuất.
  - [ ] Raycast tap cây chính xác trên cả 3 stage, không bị che bởi đá hay cây phụ; nghiệm thu lại vùng hình cầu theo yêu cầu người dùng.
  - [x] Hệ thống thời tiết (Nắng, Mưa, Giông, Flash sét, Burning) hoạt động nguyên vẹn trên môi trường mới.
- [x] Kiểm thử & nghiệm thu:
  - [x] Không có lỗi compile hay runtime error trong Unity Console.
  - [x] Toàn bộ bộ test EditMode vượt qua (261/261 PASS).
  - [x] Chạy kiểm tra hiển thị trực tiếp trong MainScene ở 1920×1080, 1366×768 và 2400×1080.
  - [x] Đo đạc hiệu năng collider động (Stopwatch + Profiler marker >= 300 frames) đạt ngân sách p95 <= 1.0 ms (p95 cao nhất là 0.9258 ms trên Large Tree, không recook mỗi frame).
  - [x] Bằng chứng đối chiếu ảnh concept và kết quả thực tế trong `Evidence/ReviewFix_20261012/`.
  - [x] Hash asset cây chính và save thật được bảo toàn nguyên vẹn 100%.

## Acceptance

- Nền hình cầu lớn nhìn rõ độ cong trong mọi góc nhìn camera gameplay.
- Môi trường toát lên phong cách nhiệt đới huyền ảo nhất quán với ảnh concept đã chọn.
- Cây chính không bị thay đổi mesh/texture/material/animation; 3 stage đều đứng vững trên mặt đất, không lơ lửng hay ngập rễ.
- Tap chuột / touch vào cây chính nhạy bén và chuẩn xác trên cả 3 stage; không click xuyên đá vào cây và không bị đất/cỏ che khuất.
- Giữ nguyên vẹn toàn bộ hệ thống thời tiết, VFX cây, âm thanh, UI, economy và save.
- Cả ba độ phân giải 1920×1080, 1366×768 và 2400×1080 hiển thị đẹp mắt, không lỗi visual hay camera clipping.

## Bàn giao

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
