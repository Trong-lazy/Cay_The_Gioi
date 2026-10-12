# T12 — Nhãn chi phí trên nút nâng cấp

Task: T12
Result: PASS
Ngày: 11/10/2026. Kết quả: **PASS**.

## Phạm vi

Theo yêu cầu trực tiếp: chỉ thay nhãn nút nâng cấp +1 bằng số lượng và tên nguyên liệu, rồi kiểm thử. Không sửa VFX, model, luật gameplay, giá nâng cấp, vị trí/kích thước nút hoặc UI khác. Giữ nhãn Mở khóa và Tiến hóa.

## Thay đổi

- `Assets/_Project/Scripts/Presentation/UpgradePanelView.cs`: lấy `Cost` và `CostResourceId` từ quote hiện hành để hiện hai dòng số lượng/tên tài nguyên; cập nhật qua Refresh sẵn có. Dùng formatter hiện hành K/M/B; không dùng chữ hiển thị để cấp quyền giao dịch. Best-fit chữ14–fontSize gốc để Quang hợp không tràn nút.
- `Assets/_Project/Scripts/Presentation/ReferenceUiLayout.cs`: bỏ thao tác ghi đè nhãn nút thành Nâng cấp mỗi lần layout; UpgradePanelView sở hữu nhãn giá trực tiếp. Không đổi bố cục.
- Task/Validation/Evidence và con trỏ tiến độ được cập nhật. Không sửa GDD hoặc config vì không thay luật.

## Kiểm chứng trực tiếp

Unity6000.6.1f1, MainScene, PrototypeConfig hiện hành; Play với thư mục save thử `Evidence/T12_Upgrade_Button_Cost_UI_20261011/isolated_save`. ManualTick giữ economy ổn định trong probe UI.

| Ca | Kết quả |
|---|---|
| Rễ L2 | Nút13 Water; bấm UnityEvent mua lênL3, trừ13 Water; nút đổi21 Water |
| Lá L2 | Nút13 Quang hợp; mua lênL3, trừ13 Quang hợp; nút đổi21 Quang hợp |
| Thân L2, Growth10 | Hiện26 Growth, nút disabled; không trừ tiền/không lên cấp |
| Rễ đạt cap | Nút disabled |
| Hấp thụ chưa mở | Giữ Mở khóa |
| Hấp thụ đã mở L1 | Hiện16 Linh khí; mua lênL2, ví100→84; nút đổi26 Linh khí |
| Layout định kỳ | Không ghi đè chi phí thành Nâng cấp/+1 |
| 1920×1080 và1366×768 | Ảnh thật đã xem; số và tên tài nguyên đủ hai dòng; Quang hợp best-fit19, Water/Growth22 |

Probe bấm UnityEvent là kiểm tra API UI, không tuyên bố chuột OS. Suite PlayMode bên dưới bổ sung input chuột/touch mô phỏng.

## Test và Console

`UpgradeSceneTests`: **7/7 PASS**, không skipped/failed. Bằng chứng chính là XML Unity xuất `Evidence/T12_Upgrade_Button_Cost_UI_20261011/UpgradeSceneTests.xml`, start2026-10-11 11:51:09Z, end11:51:13Z (18:51 giờ Việt Nam).

Bao gồm chuột mở/mua/giữ/đóng, touch mua đúng một lần và chặn tap xuyên, đúng ví Growth, mở hấp thụ và lưu sản xuất, cap/dead/rebind listener, safe area portrait/landscape, cập nhật quote/buff.

Công cụ MCP lượt đầu timeout khởi động; lượt tiếp theo báo0 tests nên không tính PASS. Lượt lọc nhóm chạy thật7 ca và xuất XML nhưng MCP bị kẹt trạng thái sau domain reload. XML kết quả mới được đọc, lưu và xác nhận đủ7 test-case, tất cả Passed. Lượt kiểm tra cuối không còn job chạy. Không thay test hoặc thêm test chỉ để phản chiếu thay đổi nhãn.

Unity compile hoàn tất; Console cuối0 error. Có cảnh báo CS0618 API FindObjectsSortMode cũ trong QaBuildRunner, ngoài hai file sửa; không sửa ngoài phạm vi. Lỗi reflection/biên dịch đoạn probe ban đầu đã được chỉnh trong probe, không phải compiler error source dự án.

## Bằng chứng và bàn giao

- `button_probe.json`, `spirit_probe.json`, `layout_1366.json`, `button_probe_source.txt`.
- Ảnh đạt: `upgrade_1920_final-1.png`, `upgrade_1366_final.png`.
- `upgrade_1920.png` là ảnh vòng đầu còn cắt Quang hợp; giữ để thể hiện trước khi best-fit. `upgrade_1920_final.png` chụp trước probe thành công, không dùng làm bằng chứng đạt.
- `UpgradeSceneTests.xml`, `test_initial_timeout.json`, hash save trước/sau.
- Save thật main/backup hash khớp hoàn toàn sau toàn bộ kiểm thử. Editor ngoài Play, MainScene sạch, save/clock override null, kích thước Game View đã trả giá trị trước kiểm thử.
- Không build lại Windows/mobile và không nghiệm thu lại VFX trong task UI này. Không có lỗi còn mở trong phạm vi nhãn nút đã yêu cầu.
