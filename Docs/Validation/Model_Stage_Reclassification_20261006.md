# Ghi nhận chuyển vai trò Tree_Large sang Cây Trưởng Thành

Task: T10
Date: 2026-10-06
Result: DOCUMENTATION_UPDATED — không phải nghiệm thu model hoặc gameplay.

## Yêu cầu và phạm vi

Người dùng chốt chuyển model Cây Lớn hiện tại sang Cây Trưởng Thành và yêu cầu lưu thông tin, sẽ tìm các model thiếu sau. Thực hiện bằng phân loại tài liệu/registry, không đổi tên file vật lý hoặc stage/save ID.

## Thay đổi

- Tạo `Docs/Art/Tree_Model_Registry.md`: bộ Tree_Large và texture Large hiện thuộc Trưởng Thành; còn thiếu Cây Lớn, Cổ Đại, Cây Thế Giới; không ghi đè Tree_Large để dựng Cây Lớn mới.
- Bổ sung D42 vào `Docs/Decisions.md`; cập nhật Backlog, START_HERE, task T10 và con trỏ/Progress qua công cụ tổng hợp.
- Cập nhật nhãn mục 2.3 và ghi chú trong material manifest; thêm ghi chú ưu tiên registry cho các prompt material/fix và brief dựng đơn giản cũ. Không sửa các thông số material/hashes để giả như đã khắc phục review.
- Cập nhật mốc/table count trong `Current_Models_GDD_Review_20261006.md`, phân biệt rõ số đo kiểm tra trước đó và phân loại mới. Giữ history các báo cáo/quyết định cũ.
- Prototype vẫn Mầm/Non/Lớn, nhưng sau phân loại nguồn Cây Lớn mới còn thiếu 1. Không đổi luật GDD, config, prefab, camera, Achievement hoặc save; không tạo source, shader hoặc texture mới.

## Kiểm tra và giới hạn

- Kiểm tra đường dẫn bốn nguồn/FBX và texture hiện có, nhãn mới, số lượng 4 nguồn/3 mốc thiếu và trạng thái T10 IN_PROGRESS.
- So SHA256 trước/sau 21 file: bốn nguồn blend, bốn FBX, mười PNG, hai asset Bonsai được bảo vệ và PrototypeConfig. Kết quả: 21/21 giữ nguyên, 0 file thay đổi trong phiên cập nhật tài liệu.
- Chạy `Tools/Docs/update_progress.py`: `Validated 13 tasks; 12 DONE. Current: T10`. Không tự tích DONE hoặc sửa con trỏ bằng tay.
- Không gọi Blender/Unity Editor, không save/export/import asset hoặc kiểm thử Play Mode mới. Không xóa/di chuyển model, `.blend1`, `.meta` hoặc GUID.
- GDD Markdown/Word không chỉnh vì đây là phân loại art, không thay phạm vi ba stage hoặc luật game. Người dùng tìm/chọn nguồn thiếu sau; chưa khởi động việc dựng/tải mới.
