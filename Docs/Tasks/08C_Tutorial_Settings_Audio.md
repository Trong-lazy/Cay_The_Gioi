---
id: T08C
title: Tutorial Setting và audio
status: DONE
depends_on: ["T08B"]
validation: "Docs/Validation/T08C_20261003.md"
---

# T08C Tutorial Setting và audio

## Mục tiêu

Onboarding đủ hiểu loop và Sức sống, UI priority đúng, Setting/Toast/audio tối thiểu.

## Thực hiện

- [x] Tutorial target/spotlight, hoàn thành theo command success, đủ start Water để Root bước đầu không kẹt.
- [x] Hướng dẫn Sức sống: nhóm tap, +thời gian, cap một giờ, buff 5%, đóng băng offline và giữ Rebirth.
- [x] Giới thiệu Trunk/Weather/Rebirth/Achievement/Shop khi unlock; restore bước hợp lệ qua load/Rebirth.
- [x] Setting music/orbit lock/Continue/Exit, zoom Coming Soon; panel manager/Escape/DEAD priority.
- [x] Toast queue lớp trên, không raycast; audio controller và SFX giới hạn, ghi nguồn asset.
- [x] Bố cục theo safe area/tỷ lệ PC và điện thoại; hint mouse/touch phù hợp, nút UI thay cho chức năng chỉ Escape/hover; hướng màn hình theo D12 khi được chốt.

## Acceptance

- [x] Hành động sai/thiếu resource không hoàn thành tutorial; chỉ target hợp lệ nhận input.
- [x] DEAD xuất hiện giữa tutorial/preview vẫn được xác nhận Rebirth, không softlock; bước đã hoàn thành giữ.
- [x] UI/panel/spotlight không tap xuyên; toast không chặn click; Escape không thoát modal bắt buộc.
- [x] Music/orbit setting restore qua save/Rebirth; không cần clip chưa có để chạy game, có audio tối thiểu trước nghiệm thu.
- [x] Tap nhiều không tạo SFX vô hạn; Sức sống hiển thị và hướng dẫn đúng luật mới.

## Nghiệm thu chung

- [x] Compile và kiểm tra bước/state/input priority đạt.
- [x] Play Mode tutorial từ save mới và save dở dang, hồi quy T01–T08B, có log.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

File thay đổi:
- `Assets/_Project/Scripts/Domain/TutorialSystem.cs`: Logic Onboarding tuần tự, kiểm tra điều kiện command success, lưu bước hoàn thành, hỗ trợ rollback khi lỗi I/O.
- `Assets/_Project/Scripts/Domain/VitalitySystem.cs`: Thêm event `Tapped` để kích hoạt SFX có rate-limiting.
- `Assets/_Project/Scripts/Presentation/AudioController.cs`: Quản lý BGM loop và SFX bằng procedural audio synthesis (không sợ thiếu file), rate limiting cho tap SFX tối đa 12.5 lần/s (MinTapInterval 0.08s).
- `Assets/_Project/Scripts/Presentation/ToastView.cs`: Hàng đợi thông báo popup, tự mờ dần, `blocksRaycasts = false` và `raycastTarget = false` không bao giờ chặn click gameplay.
- `Assets/_Project/Scripts/Presentation/SettingsView.cs`: UI Cài đặt trong SafeArea, toggle Âm nhạc và Khóa xoay Orbit, nút Tiếp tục và Thoát game, ghi chú "Zoom camera: Sắp ra mắt (Coming Soon)", chặn raycast gameplay khi mở.
- `Assets/_Project/Scripts/Presentation/TutorialView.cs`: UI Banner Onboarding trong SafeArea, tự động ẩn khi cây DEAD để nhường chỗ cho Forced Rebirth modal, không softlock.
- `Assets/_Project/Scripts/Presentation/UiPriorityManager.cs`: Quản lý thứ tự ưu tiên UI và phím Escape, tuyệt đối không cho Escape đóng modal khi cây DEAD.
- `Assets/_Project/Scripts/Bootstrap/GameBootstrapper.cs`: Wire toàn bộ các hệ thống Audio, Toast, Settings, Tutorial và UI Priority.
- `Assets/_Project/Tests/EditMode/TutorialSettingsAudioTests.cs`: 11 unit tests EditMode kiểm tra Tutorial, Settings, Audio rate-limiting và serialization.
- `Assets/_Project/Tests/PlayMode/TutorialSceneTests.cs`: 5 integration tests PlayMode kiểm tra mở/đóng Settings, lưu/khôi phục cài đặt qua save/load, phím Escape & DEAD modal, Tutorial ẩn khi DEAD, Toast không chặn raycast.
- `Docs/Decisions.md`: Bổ sung quyết định D22.
- `Evidence/T08C/test_results.txt`: Bằng chứng kết quả test.
- `Docs/Validation/T08C_20261003.md`: Báo cáo nghiệm thu PASS.

Test: 187/187 EditMode tests PASS, 59/59 PlayMode tests PASS 100%.
Ghi chú Console Editor:
- Lỗi `Layout update failed to stabilize after 10 iterations` trong Console là bug giao diện nội bộ của Unity 6000.6.1f1 Editor (`MainToolbarWindow` UIElements ToolbarScrollView do lệch 0.8px khi co giãn thanh công cụ Editor).
- Đã kiểm tra chi tiết trong `Logs/Editor.log`: Lỗi thuộc `UnityEditor.Overlays.ToolbarScrollView` trong `MainToolbarWindow`, hoàn toàn KHÔNG thuộc về bất kỳ script C# nào của game, không thuộc `MainScene.unity` hay Canvas runtime.
- Đính chính qua phiên Console 03/10/2026: lỗi Editor này có thể làm Test Runner fail bởi unhandled error và ngập Console. Đã thu gọn toolbar, sửa scope input test khi mất focus và chạy lại 187/187 EditMode + 59/59 PlayMode PASS theo XML. Không phát hiện hồi quy gameplay do lỗi toolbar; bản build độc lập chưa kiểm tra trong phiên này. Xem `Docs/Validation/Console_20261003.md`. T08C giữ DONE, T09 IN_PROGRESS cho phần tích hợp/kiểm tra được giao.
Blocker: Không có.
Tiếp theo: T09 Tích hợp tuning và build khung (dừng trước T09 theo AGENTS.md).
