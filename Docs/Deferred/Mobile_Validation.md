# Nghiệm thu điện thoại tạm hoãn

Trạng thái: DEFERRED, chưa nghiệm thu. Nguồn: yêu cầu trực tiếp ngày 04/10/2026, ưu tiên PC Windows trước. Đây là danh sách công việc làm sau, không là task đang hoạt động hoặc phụ thuộc chặn T09 Windows.

Mục tiêu đã chốt: Android và iOS, Landscape cố định. Giữ MainScene, domain/config/state/save schema, input cảm ứng và UI safe area dùng chung. Không coi touch/lifecycle mô phỏng trong Editor là bằng chứng máy thật.

Khi người dùng yêu cầu tiếp tục mobile, lập task riêng với thiết bị và điều kiện nghiệm thu:

- [ ] Xác định model/OS máy thật và môi trường build cho Android/iOS; không ghi build thành công khi chưa có toolchain.
- [ ] Cấu hình build/pipeline chất lượng phù hợp điện thoại, Landscape, application identifier; không đổi đường dẫn save PC ngoài ý muốn.
- [ ] Tạo gói cài đặt và cài/chạy trên từng nền tảng cần nghiệm thu, ghi version/config/device.
- [ ] Kiểm tra tap/drag/UI/đa chạm, camera/lock, safe area/notch, chữ và kích thước nút.
- [ ] Kiểm tra pause/focus/resume/kill/reopen, save atomic/recovery/offline một lần và Sức sống đóng băng trong nền.
- [ ] Chạy toàn bộ vòng chơi và kiểm tra hiệu năng/phiên dài bằng log/số đo thật.
- [ ] Ghi Validation/Evidence; chỉ đánh dấu hoàn thành sau khi tiêu chí thiết bị thật đạt.

Hiện chưa có nghiệm thu mobile. Phần hoãn không được báo là PASS khi T09 Windows DONE.
