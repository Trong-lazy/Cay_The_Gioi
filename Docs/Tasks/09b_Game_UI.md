---
id: T09B
title: Chỉnh giao diện gameplay sau sửa economy
status: DONE
depends_on: ["T09A"]
validation: "Docs/Validation/UI_Reference_Layout_20261010.md"
---

# T09B Giao diện gameplay

## Mục tiêu

Theo yêu cầu sửa economy trước rồi chỉnh UI: hiển thị rõ bốn tài nguyên, chi phí và điều kiện mở khóa; bố cục chừa không gian nhìn cây. Dùng các PNG production từ chat “Tách UI assets từ ảnh tham chiếu”, chỉ dùng nền có nine-slice đúng và icon sạch. Không làm VFX/map/model trong task này.

## Thực hiện

- [x] HUD có bốn thẻ tài nguyên và trạng thái gọn; Linh khí khóa hiển thị điều kiện.
- [x] Nâng cấp/Evolution/Rebirth/tutorial/menu có vị trí và hình thức thống nhất, không che nút thiết yếu.
- [x] Import asset có provenance, không chỉnh GUID/meta tay; giữ UI gọi API domain.

## Acceptance

- [x] Unity compile không lỗi liên quan.
- [x] Hồi quy gameplay, save và input mouse/touch đạt.
- [x] Thử MainScene Play trực tiếp với UI đóng/mở/menu và các tỷ lệ Landscape; ảnh thật được lưu và xem.
- [x] Safe area, chữ/chi phí đọc được, trạng thái khóa/thiếu tài nguyên đúng, không tap xuyên UI.

## Bàn giao

10/10/2026 — hoàn tất bố cục theo ảnh, không sự kiện. 220/220 EditMode,68/68 PlayMode PASS; xem đủ8 ảnh MainScene1920×1080 và HUD/Upgrade1366×768. Build mới Builds/Windows_UI_Reference_20261010/Cay_The_Gioi.exe Succeeded0 errors/6 warnings; StandaloneQA4 đạt5/5 bằng API/uGUI hooks, không phải chuột OS; ảnh Player hidden đen nên không tính visual bằng chứng. Save thật giữ hash, Editor ngoài Play/MainScene sạch, validation overrides null. Báo cáo hiện hành UI_Reference_Layout_20261010.md có lịch sử lỗi/sửa và giới hạn: âm nhạc vẫn toggle, tutorial giữ7 ID theo D47, bộ art chưa fantasy/pixel-perfect. T10 vẫn NOT_STARTED với hai lỗi raycast cũ, không sửa trong phiên bố cục này.

10/10/2026 — yêu cầu bố cục theo bộ 7 ảnh trong chat, bỏ toàn bộ phần sự kiện: mở lại T09B IN_PROGRESS (T09A DONE, T10 tiếp tục NOT_STARTED). Thêm ReferenceUiLayout làm chủ bố cục responsive: Settings/Achievement trái, Shop/Upgrade phải, ví Coin/điểm trên giữa, thời tiết trên phải, hai action dưới vùng nhìn cây. Giữ thêm HUD bốn tài nguyên/HP/Sức sống để không mất thông tin gameplay D47. Có scroll/tab lọc, bấm ngoài đóng (không đóng khi bấm nền bên trong panel), dịch projection hiển thị cây vào vùng trống, Trùng sinh hai bước và spotlight tutorial theo target hiện có. Không đổi economy/model/stage/save ID, không thêm sự kiện hoặc vật phẩm trong ảnh. Tiếp tục kiểm thử; báo cáo phiên này: Docs/Validation/UI_Reference_Layout_20261010.md. Các kết quả DONE bên dưới là phiên trước, không phải nghiệm thu thay đổi mới.

10/10/2026: đã triển khai HUD bốn thẻ tài nguyên, HP/Sức sống/thời tiết gọn, menu trên và action dưới. GameUiTheme thống nhất 10 PNG production có provenance/nine-slice qua Sprite importer Unity. Upgrade/Evolution ghi đúng ba tài nguyên, khóa Linh khí; Rebirth hiện đủ bốn ví point. Các view gọi API domain, không sửa số dư. Hướng dẫn tạm ẩn khi modal mở; thông báo lỗi lưu và nút retry vẫn hoạt động.

Sửa hồi quy bố cục của Shop/Achievement: list và row đang stretch nhưng cộng thêm chiều rộng cố định khiến tràn màn hình. Chuyển sizeDelta ngang thành inset/0; đã xem ảnh thật. Test HUD input chuyển từ ResourceBackdrop cũ sang WaterCard; card chặn gesture bắt đầu trên UI.

220/220 EditMode PASS; lượt PlayMode cuối sau sửa menu đạt 65/65 PASS (job 267f60bf297d4f94bfe915a0460286c4). MainScene config prototype-6 đã xem trực tiếp HUD, Upgrade, Settings, Shop, Achievement, Rebirth và Landscape 1920×1080/1366×768. Ảnh và log nằm Evidence/GameUI_20261010. Một lượt đầu 64/65 và callback MCP mất được giữ làm lịch sử, đã khắc phục và chạy lại thật; không sửa kết quả test.

Build Windows Development thành công, 0 lỗi/10 cảnh báo; 5/5 Standalone QA PASS, thêm ui_flow cửa sổ hiển thị PASS và ảnh Player thật đã xem. Save thật/backup/MainScene giữ nguyên hash; override đã về null, Editor thoát Play và scene sạch. Xem bản ghi Validation đầy đủ. Không mở rộng VFX/map/model trong task này. Cây Trưởng Thành tiếp tục dùng model Cây Lớn placeholder; T10 còn hai lỗi raycast ghi trước đó. Mobile thiết bị thật hoãn theo D12.
