# Mẫu ghi kiểm tra công việc

Sao chép thành Txx_YYYYMMDD.md sau khi có kiểm tra thực. Không dùng mẫu này làm bằng chứng PASS.

Task: Txx
Result: NOT_RUN
Ngày và người hoặc agent kiểm tra:
Unity:
Scene:
Config version:
Build hoặc Play Mode:

## Phạm vi thay đổi

File/service/state đã thay và mục tiêu của task.

## Compile và kiểm thử

Lệnh hoặc thao tác thật, thời điểm, tổng test chạy/pass/fail; Console có lỗi/warning gì; liên kết log hoặc ảnh trong Evidence. Không suy ra compile thành công từ chỉ đọc code.

## Acceptance

| Tiêu chí | Bước và dữ liệu thử | Mong đợi | Thực tế | Kết quả |
| --- | --- | --- | --- | --- |
| Điền tiêu chí task | Điền thao tác thật | Expected | Actual | NOT_RUN |

## Hồi quy

Các task/hệ thống trước liên quan, kịch bản đã thử và kết quả. Không ghi “không phá gì” nếu chưa thử.

## Lỗi và bàn giao

Lỗi còn tồn tại, blocker, cách tái hiện và bước xử lý. Chỉ đổi Result thành PASS khi mọi acceptance và DoD đạt; chưa kiểm tra Unity thì WAITING_VALIDATION ở task.
