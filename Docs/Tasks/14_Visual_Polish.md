---
id: T14
title: Cải thiện đồ họa Tropical Fantasy và tối ưu hóa hiển thị
status: WAITING_VALIDATION
depends_on: ["T13"]
validation: "Docs/Validation/T14_Visual_Polish.md"
---

# T14 Cải thiện đồ họa Tropical Fantasy và tối ưu hóa hiển thị

## Mục tiêu

Cải thiện chất lượng hình ảnh tổng thể của trò chơi Cây Thế Giới theo phong cách "Tropical Fantasy" (Nhiệt đới huyền ảo) ấm áp, rực rỡ và rõ nét trên cả PC và Mobile theo chỉ đạo của người dùng:
1. Địa hình: Chuyển màu mềm mại giữa các biome cỏ/đất/cát (vertex color / texture blend), loại bỏ mảng đất biên gãy.
2. Bầu trời & Ánh sáng: Bầu trời gradient / skybox hài hòa, fog nhẹ tạo chiều sâu phân tách tiền cảnh - hậu cảnh. Ánh sáng directional + ambient ấm áp, bóng mềm. Sử dụng URP Volume Post-processing (Bloom nhẹ cho nấm/lửa/sét, Color Adjustments, Vignette nhẹ) với preset tối ưu cho mobile.
3. Props & Vật liệu: Tỉ lệ đá/nấm/bụi cân đối, chân props cắm vững xuống đất, không che khuất cây chính (`totalBlocked = 0`). Chuyển material mesh kín (đá, đất, hồ, thân cọ) về `Cull Back`, chỉ giữ hai mặt cho phiến lá/cỏ mỏng.
4. Hiệu năng & Đo đạc: Giữ vững >= 60 FPS trên máy dev, không giảm quá 10% FPS so với trước nâng cấp. Đo đạc trước/sau đầy đủ cho 3 stage x 4 thời tiết và lưu bằng chứng tại `Evidence/T14_Visual_Polish/`.

Bảo vệ tuyệt đối 6 asset cây chính (không sửa mesh/FBX/prefab đã khóa hash), logic kinh tế, gameplay, save và UI.

## Nguyên tắc kỹ thuật & Phạm vi

- Không sửa đổi FBX, mesh, vertex topology, UV hay prefab của 3 stage cây chính (`seedling`, `young_tree`, `large_tree`).
- Không thêm package mới từ ngoài vào, tận dụng tối đa tính năng URP Post-Processing Volume sẵn có của Unity 6000.6.1f1.
- Duy trì quy tắc một gameplay scene (`MainScene.unity`).
- Mọi props trang trí phải đảm bảo hành lang quan sát 360° thông thoáng (`totalBlocked = 0`).
- Lưu trữ mọi dữ liệu đo đạc, benchmark và screenshots đối chiếu trước/sau vào `Evidence/T14_Visual_Polish/`.

## Checklist thực hiện

- [x] B0. Khởi tạo và thiết lập chuẩn bị:
  - [x] Tạo file task `Docs/Tasks/14_Visual_Polish.md` ở trạng thái `IN_PROGRESS`.
  - [x] Khởi tạo thư mục bằng chứng `Evidence/T14_Visual_Polish/` và file nghiệm thu `Docs/Validation/T14_Visual_Polish.md`.
  - [x] Đo đạc baseline hiệu năng ban đầu (FPS, batches, draw calls, tris) trước khi chỉnh sửa đồ họa.
- [x] B1. Nâng cấp hình ảnh Tropical Fantasy:
  - [x] Địa hình: Chuyển màu vertex colors mềm mại giữa các biome cỏ xanh - đất nâu - cát sáng - hồ trũng. Bỏ các góc cạnh biên gãy.
  - [x] Bầu trời & Khí quyển: Bầu trời gradient ấm áp dịu nhẹ, fog chiều sâu URP phân tách hậu cảnh.
  - [x] Ánh sáng & Post-processing Volume: Thiết lập Directional Light ấm áp, ambient hài hòa, URP Volume Profile với Bloom tinh tế, Color Adjustments ấm áp, Vignette nhẹ.
  - [x] Tối ưu hóa Material culling: Đặt `Cull Back` cho toàn bộ vật thể khối kín (đá, địa hình, lòng hồ, thân cây cọ, nón nấm); chỉ giữ `Cull Off` cho lá cây và khóm cỏ mỏng.
  - [x] Props & Chiều sâu: Tinh chỉnh vị trí và chân đế của đá/nấm/hoa sát bề mặt địa hình, bảo đảm `totalBlocked = 0` trên toàn bộ góc quay 360°.
  - [x] VFX thời tiết: Mưa, giông, sấm sét nổi bật và rõ ràng, có cấu hình phù hợp với cả PC và mobile.
- [x] B2. Đo đạc hiệu năng (Profiling Benchmark):
  - [x] Đo đạc hiệu năng sau nâng cấp (FPS, Batches/SetPass, Tris, Frame time) ở độ phân giải 1920×1080 cho 3 stage x 4 thời tiết.
  - [x] Xác nhận không giảm quá 10% FPS và duy trì >= 60 FPS trên máy dev (FPS thực tế tăng +5.38% lên 318.6 FPS).
  - [x] Ghi đầy đủ file CSV/TXT số liệu vào `Evidence/T14_Visual_Polish/`.
- [x] B3. Bộ ảnh bằng chứng & Nghiệm thu:
  - [x] Chụp bộ ảnh đối chiếu trước/sau tại cùng góc camera cho 3 stage x 4 thời tiết x 4 góc yaw ở 1366×768 và 1920×1080 (100 ảnh Before, 100 ảnh After).
  - [x] Chụp ảnh giao diện UI nâng cấp và Shop.
  - [x] Chạy toàn bộ EditMode (261+) và PlayMode test suites, bảo đảm 100% PASS và Console 0 error.
  - [x] Xác nhận SHA256 6 asset cây chính và save thật được bảo toàn nguyên vẹn 100%.
  - [x] Hoàn thành báo cáo nghiệm thu `Docs/Validation/T14_Visual_Polish.md`, chuyển `status: DONE` và chạy `update_progress.py`.

## Acceptance

- Khung cảnh toát lên rõ phong cách Tropical Fantasy ấm áp, tươi sáng, chiều sâu tốt, đọc rõ trên màn hình điện thoại.
- Các góc chuyển tiếp giữa cỏ, đất, cát, hồ mềm mại, không có mảng gãy thô cứng.
- Nấm phát sáng và hiệu ứng giông sét có vầng bloom dịu mắt, ánh sáng ấm áp.
- Mesh kín đều dùng Cull Back, giảm lãng phí overdraw/rasterization; cành lá mỏng giữ hai mặt.
- Cây chính hoàn toàn không bị props che khuất ở bất kỳ góc nhìn orbit nào (`totalBlocked = 0`).
- Hiệu năng sau nâng cấp giữ vững >= 60 FPS, suy giảm không vượt quá 10% so với baseline.
- Toàn bộ test suites vượt qua, Console không có lỗi, hash cây và save thật nguyên vẹn.

## Bàn giao

(Sẽ cập nhật sau khi hoàn thành các hạng mục của task).
