# Kiểm tra bộ tài liệu triển khai

Ngày 02 tháng 10 năm 2026. Phạm vi chỉ là bộ tài liệu và công cụ tổng hợp/xuất tài liệu. Bản ghi này không nghiệm thu task gameplay T00–T09.

## Kết quả

- GDD Word xuất từ Markdown, 5 trang; đã xem toàn bộ ảnh trang render từ Microsoft Word và kiểm tra tiếng Việt, ngắt trang, heading, bullet, footer, không có chữ cắt hoặc chồng và không còn đường viền dưới title.
- Renderer đóng gói render_docx.py đã thử nhưng không chạy được vì thiếu soffice.exe. Dùng Microsoft Word COM xuất PDF và Poppler đi kèm runtime tạo ảnh kiểm tra; không cài phần mềm hoặc thay project Unity.
- Công cụ tổng hợp đọc 12 file task, phụ thuộc hợp lệ, 0 DONE, con trỏ T00. Không đổi metadata nguồn khi chạy tổng hợp.
- Kiểm tra 13 liên kết Markdown tới file hiện có; không thấy liên kết hỏng trong các file đã tạo.
- Công cụ Python parse cú pháp thành công; GDD Word chứa các luật 24 giờ, 50%, cap 3600 giây, +5% và ví dụ 50 tap/20 giây.
- Đối chiếu quyết định mới trong GDD, config, schema, task và tutorial: Sức sống giữ qua Rebirth, đóng băng offline, không tính buff trong offline production.

## Trạng thái triển khai

163 checkbox gameplay vẫn chưa tích. Chưa compile gameplay mới, chưa Play Mode, chưa build hoặc playtest game. START_HERE và CURRENT_TASK chỉ định T00 để agent tiếp theo kiểm tra môi trường và bắt đầu nền tảng.
