# Quy tắc triển khai Cây Thế Giới

## Mục tiêu và phạm vi

- Làm khung gameplay trước. Dùng placeholder, UI chức năng và cấu hình dễ thay đổi. Chưa làm model Blender chi tiết, shader, môi trường đẹp hoặc tối ưu hình ảnh cầu kỳ.
- Giữ Unity 6000.6.1f1, URP và Input System đang có. Không tự nâng phiên bản hoặc thêm package khi task không cần.
- Một gameplay scene, offline, dùng chung cho PC Windows, Android và iOS; điện thoại Landscape cố định theo D12. Ưu tiên nghiệm thu Windows trước; build/test mobile thiết bị thật tạm hoãn theo yêu cầu ngày 04/10/2026 và không chặn DONE của T09 Windows. Giữ input cảm ứng và kiến trúc dùng chung. Ba stage đại diện; không backend, tài khoản, quảng cáo, IAP, multiplayer hoặc nhiều map.
- Chỉ làm task hiện tại và các sửa lỗi trực tiếp cần để task đạt tiêu chí. Ghi ý tưởng tương lai vào `Docs/Backlog.md`; thay đổi luật cần cập nhật `Docs/Decisions.md`.
- Có thể sửa hồi quy của task trước trong phạm vi cần thiết; ghi rõ nguyên nhân và kiểm tra lại. Không dùng quy tắc phạm vi để bỏ mặc lỗi ngăn task hiện tại hoạt động.

## Trước mỗi phiên

1. Đọc `START_HERE.md`, con trỏ công việc và file task. Đọc đặc tả kỹ thuật có liên quan.
2. Kiểm tra project, phiên bản, Console và scene thực tế. Không xóa nội dung sẵn có ngoài phạm vi task.
3. Kiểm tra phụ thuộc đều `DONE`. Nếu thiếu, xử lý task trước; không vượt qua task đang bị chặn.
4. Chuyển task sang `IN_PROGRESS` trước khi sửa. Mỗi phiên chỉ có một task hoạt động; không chạy nhiều agent sửa chung state hoặc scene nếu người dùng chưa yêu cầu.
5. Đọc quyết định `PENDING_USER`; chỉ hỏi khi phần chưa chốt ngăn việc đang làm. Có thể tiếp tục phần độc lập. Giá trị `PROTOTYPE_DEFAULT` được dùng để làm prototype và phải nằm trong config.

## Nguyên tắc kỹ thuật

- `GameBootstrapper` chỉ tạo, nối và khởi tạo hệ thống. Không biến GameManager thành nơi xử lý mọi logic.
- Logic gameplay không phụ thuộc model, particle, text HUD hoặc tên GameObject. UI gọi API hệ thống; view nghe thông báo sau khi state được cập nhật thành công.
- Một bộ logic gameplay dùng chung PC/điện thoại. Input System chuyển chuột hoặc cảm ứng thành command chung; chặn UI, không tính kéo camera thành tap và không nhận đôi từ touch/mouse mô phỏng. UI thích ứng tỷ lệ màn hình và safe area; chức năng thiết yếu không chỉ phụ thuộc chuột phải, hover hoặc Escape. Save theo persistentDataPath và pause/focus ứng dụng, không chỉ OnApplicationQuit. Xem Docs/Technical/Cross_Platform.md.
- Tách config bất biến, state runtime và DTO lưu. Dùng ID ổn định cho stage, achievement và cosmetic; không dùng thứ tự danh sách hay tên model làm ID save.
- Giao dịch tài nguyên, Evolution, Rebirth, claim Coin và mua item phải có kiểm tra điều kiện và kết quả thất bại rõ ràng. Không cập nhật tài nguyên trong UI.
- Không tự coi một đề xuất trong góp ý hoặc ví dụ số là luật mới. Yêu cầu trực tiếp mới nhất của người dùng có ưu tiên; phản ánh thay đổi vào quyết định và tài liệu bị ảnh hưởng.
- Unity MCP dùng để kiểm tra phiên Editor và thao tác scene khi khả dụng; đọc skill Unity của môi trường trước khi dùng. Nếu mất kết nối, vẫn có thể sửa phần độc lập nhưng không tuyên bố đã kiểm tra trong Editor.
- Blender MCP chỉ dùng khi đến task hình ảnh và có khả năng tương ứng. Không bắt dự án gameplay phụ thuộc kết nối Blender.

## Khi nào được hoàn thành

Code đã viết chưa đủ để đánh dấu `DONE`. Với task gameplay, tất cả điều kiện sau phải đạt:

- [ ] Unity compile không lỗi liên quan thay đổi; lỗi có sẵn được ghi và xử lý nếu ngăn kiểm tra.
- [ ] Checklist thực hiện và acceptance criteria của task đều được kiểm chứng.
- [ ] Thử trực tiếp trong Unity Play Mode với scene và config được ghi rõ.
- [ ] Kiểm tra hồi quy có liên quan không phá task trước; test tự động cho logic quan trọng nếu task yêu cầu.
- [ ] Có bản ghi trong `Docs/Validation/` và bằng chứng/log thực tế khi phù hợp trong `Evidence/`.
- [ ] Ghi file thay đổi, config, kết quả và lỗi còn lại trong phần bàn giao task.
- [ ] Đặt `status: DONE` trong file task, chạy công cụ tổng hợp để cập nhật Progress và CURRENT_TASK, kiểm tra con trỏ đúng.

Nếu chưa kiểm tra trực tiếp trong Unity, trạng thái tối đa là `WAITING_VALIDATION`; không tích tiêu chí chưa thử. Nếu thiếu thông tin hoặc công cụ ngăn phần việc cần làm, dùng `BLOCKED` với lý do cụ thể. Không bịa test, ảnh, log hoặc kết quả playtest.

Nếu task đã DONE xuất hiện lỗi làm sai tiêu chí, bỏ tích các mục bị ảnh hưởng, mở lại task với `IN_PROGRESS`, cập nhật Validation và con trỏ. Các task đã DONE phụ thuộc nó phải được đánh giá lại; không để Progress tuyên bố chuỗi phụ thuộc hoàn thành khi nền tảng chưa đạt.

## Cuối mỗi phiên

- Lưu và ghi bàn giao trong file task, kể cả khi chưa xong.
- Cập nhật log kiểm tra và quyết định nếu thay config hoặc luật.
- Chạy `Tools/Docs/update_progress.py`; không tự tích DONE để mở task tiếp theo.
- Khi sửa GDD Markdown, xuất lại Word bằng `export_gdd_docx.py` và kiểm tra bố cục Word; các file kỹ thuật và task được dẫn từ GDD, không cần sao chép toàn bộ vào Word.
- Không chỉnh tay GUID hoặc `.meta`; di chuyển asset qua Unity khi có thể, giữ `.meta` cùng asset. Không chỉnh `Library`, `Temp`, project C# sinh tự động.
