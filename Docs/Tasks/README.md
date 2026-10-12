# Quy trình công việc

File task là nguồn trạng thái duy nhất. Metadata đầu file dùng id, title, status, depends_on và validation. Chỉnh status và checklist trong file task, rồi chạy `Tools/Docs/update_progress.py` để tổng hợp. Không sửa riêng Progress hoặc CURRENT_TASK.

Thứ tự bắt buộc T00 → T01 → T02 → T03 → T04 → T05 → T06 → T07 → T08A → T08B → T08C → T09. Giữ một task hiện tại; không nhảy qua task BLOCKED hoặc WAITING_VALIDATION. T08 chia nhỏ để dễ nghiệm thu.

## Trạng thái

- NOT_STARTED: chưa bắt đầu triển khai hoặc chưa kiểm tra hiện trạng để nhận task.
- IN_PROGRESS: đang làm, có bàn giao cụ thể nếu kết thúc phiên.
- WAITING_VALIDATION: phần thực hiện đã đủ để kiểm tra nhưng chưa hoàn tất nghiệm thu, đặc biệt khi chưa vào Unity.
- BLOCKED: lý do cụ thể ngăn tiếp tục phần cần làm, ghi hành động giải quyết; không dùng cho mọi lỗi thông thường.
- DONE: mọi checkbox đạt, compile/Play Mode/hồi quy đã kiểm chứng và validation ghi PASS.

## Chu kỳ

Đọc phụ thuộc → kiểm tra hiện trạng → IN_PROGRESS → thực hiện trong scope → WAITING_VALIDATION → kiểm tra/checklist → ghi Validation và Evidence → DONE → tổng hợp → đọc task kế tiếp.

Không tự đặt DONE từ checklist code. `validation` trỏ tới bản ghi thực tế dùng mẫu tại `Docs/Validation/Template.md`; với DONE, bản ghi cần `Task: Txx` và `Result: PASS`. Công cụ kiểm tra metadata, phụ thuộc, checkbox và bản ghi có tồn tại; nó không thay việc kiểm chứng thực tế trong Unity.

## Definition of Done chung

Tất cả task dưới đây đều có checkbox cho compile, kiểm tra Unity, hồi quy, bằng chứng và bàn giao. Kết quả cần gồm phiên bản Unity, scene/config, bước thử, expected/actual và kết luận. Chỉ tích việc đã làm thật. Lỗi chặn acceptance phải sửa trước DONE.

Nếu mở lại task đã DONE, đánh giá các task phụ thuộc, bỏ DONE của task cần kiểm tra lại; công cụ từ chối chuỗi DONE có phụ thuộc chưa DONE. Trường hợp dự án đã có phần công việc sẵn, kiểm tra và ghi bằng chứng trước khi đánh dấu; không làm lại vô ích và không mặc định hoàn thành.
