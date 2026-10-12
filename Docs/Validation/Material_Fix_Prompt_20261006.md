# Soạn prompt Antigravity khắc phục material

Task: T10
Date: 2026-10-06
Result: PROMPT_READY — chỉ hoàn thành tài liệu giao việc, không hoàn thành khắc phục material.

## Yêu cầu và đầu ra

Người dùng yêu cầu tạo file hoặc prompt để Antigravity xử lý các phần chưa đạt đã được kiểm tra. Tạo `Docs/Art/Antigravity_Material_Fixes_20261006.md`, lệnh tuần tự R1 màu lá/export, R2 hướng vân vỏ Cổ Thụ, R3 manifest/remap và kiểm chứng độc lập.

## Đối chiếu tài liệu

- Đọc START_HERE, con trỏ/task T10, bàn giao trước, quyết định D34–D40, manifest và report kiểm tra độc lập. T10 IN_PROGRESS, T09 DONE theo con trỏ; metadata trên đĩa giữ Unity 6000.6.1f1/URP 17.6.0/Input System 1.20.0.
- Prompt chỉ cho sửa nguồn ArtSource hiện có; khóa geometry/corner normals/leaf UV; chỉ UV vỏ Cổ Thụ được chỉnh để sửa hướng thớ. Không cho đổi Assets, scene, gameplay hoặc thêm stage Cổ Thụ.
- Phương án đề xuất rõ để tránh Mix không qua FBX: bốn map lá đầu ra cùng pattern chung, bake màu linear đúng, nối trực tiếp BaseColor và dùng tint trắng; không thêm bản model. Đây là hướng dẫn triển khai, chưa tạo PNG hoặc đổi graph.
- Yêu cầu reimport đủ bốn FBX và kiểm tra BaseColor thật, không chỉ số mesh/slot; ảnh RAW import phân biệt bản đã remap. Unity chưa thử phải ghi NOT_TESTED, không DONE T10.
- Không cần chạy Editor/Blender cho phiên soạn này; không kiểm tra lại live Console, scene hoặc kết quả vật liệu mới. Không gửi tin nhắn hoặc chạy Antigravity.

## File thay đổi và bàn giao

- Thêm prompt và validation phiên soạn này; thêm bàn giao T10, chạy công cụ tổng hợp tiến độ.
- Không sửa model, FBX, texture, manifest hoặc report review gốc. Không đổi config/luật/GDD hay trạng thái task/checklist.
- Prompt sẵn sàng để người dùng giao cho Antigravity. Chất lượng sửa thực tế phải nghiệm thu bằng bằng chứng sau khi agent triển khai.
