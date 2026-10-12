# Bố cục UI theo ảnh tham chiếu — 10/10/2026

Task: T09B
Result: PASS

Phạm vi: yêu cầu trực tiếp sắp xếp 7 nhóm UI theo ảnh trong chat, bỏ phần sự kiện. Unity 6000.6.1f1/MainScene/PrototypeConfig prototype-6. D47 không đổi.

## Đã triển khai

- Settings và Achievement dock trái; Upgrade và Shop dock phải. Back/ngoài panel để đóng; không click xuyên UI.
- Coin/điểm trùng sinh trên giữa; thời tiết trên phải; Shop/Nâng cấp ở đáy vùng nhìn cây. Bốn tài nguyên, HP, Sức sống/tap giữ để không mất dữ liệu gameplay.
- Shop lưới hai cột, tab Tất cả/VFX/Trang trí, giữ API preview/buy/equip; tab Trang trí chưa có vật phẩm không bịa sản phẩm. Achievement có tiến độ tổng, tab lọc, list cuộn, claim API cũ.
- Trùng sinh thường: xem ví/hiệu ứng hiện hành → bấm Trùng sinh → xác nhận riêng. Forced DEAD vẫn bắt buộc, không cho hủy.
- Tutorial làm tối nền, highlight target hiện hành; giữ các bước/ID đã lưu của tutorial D47 thay vì thay 7 bước gameplay hiện hành bằng 12 bước khác luật trong ảnh.
- Camera dùng offset projection để cây nằm trong phần trống khi mở side-panel; không thay transform/pose/config camera.
- Dùng lại 10 PNG production hiện có; đây là sắp xếp bố cục, không tuyên bố đạt chất lượng minh họa fantasy của ảnh. Âm nhạc giữ chức năng bật/tắt hiện có (chưa thêm slider âm lượng có persistence); không thêm zoom/các item/stage mở rộng từ ảnh.

## File chính

ReferenceUiLayout.cs (composition, scroll/tabs/backdrop/spotlight), GameUiTheme.cs (nối layout), các view HUD/Upgrade/Shop/Achievement/Settings (nhường quyền layout), RebirthView.cs (luồng 2 bước). ReferenceUiSceneTests.cs mới; RebirthSceneTests cập nhật thao tác hai bước; Evolution/Upgrade tests cập nhật giả định bố cục cũ nhưng giữ kiểm tra chức năng.

## Kiểm tra

- Compile/Console không có lỗi. Cảnh báo importer version9 AllSkyFree đã có trước sửa, không chỉnh package.
- **68/68 PlayMode PASS**, job b1a473557ade43c3b62e6492cd9692e7, XML thật play_final.xml. **220/220 EditMode PASS**, job a42babce67e5490885ff122d4a5c1302, edit_final.xml. Ba tests mới kiểm tra dock/no-event, click nền trong/ngoài, preview không commit trước xác nhận.
- Play trực tiếp MainScene với save thử riêng (editor_save/final_preview_save). Đã capture và xem final_main, final_upgrade, final_shop, final_achievement, final_settings, final_rebirth_overview, final_rebirth_confirm, final_tutorial tại1920×1080; thêm final_main_1366/final_upgrade_1366 tại1366×768. Safe-area test giữ mô phỏng portrait/landscape; điện thoại thật hoãn theo D12.
- Các lượt đầu có8/4/2/1 failures được giữ lịch sử. Đã sửa click bubbling, hủy trùng sinh bị nút Begin đè, kích thước modal cạnh tranh giữa view/layout, forced DEAD layout trước Canvas update, statusCard che retry save và projection cập nhật muộn. Một số MCP run trả0 tests không được tính PASS; refresh rồi chạy đủ68 thật.
- Windows Development mới: Builds/Windows_UI_Reference_20261010/Cay_The_Gioi.exe. Build cuối Succeeded,0 errors/6 warnings. Không ghi đè EXE cũ.
- StandaloneQA lượt đầu2/5, lượt2 0/5 do runner chạy hidden bị SUSPENDED và đồng hồ quay lùi sau offline thử+3600. Sửa runner giữ UTC monotonic, ghi HOOK_FOCUS và emulates focused lifecycle **chỉ khi có QA scenario opt-in**, không thay lifecycle chơi bình thường. RunUiFlow thêm click Begin trước Confirm. Lượt3 4/5; thêm khởi tạo focus ngay trước chạy scenario, lượt4 **5/5 PASS**, save thật/backup không đổi (StandaloneQA4/summary.json và reports/logs). Đây là kiểm thử uGUI EventSystem/API hook, không phải chuột OS thật. Ảnh Player hidden đã mở xem nhưng đen, **không dùng làm bằng chứng visual PASS**; nghiệm thu hình thức dựa vào ảnh MainScene Play thực tế đã xem ở trên. Giữ đủ các lượt lỗi, không sửa kết quả.

## File thay đổi bổ sung và hạn chế

SaveStatusView.cs tránh HUD che nút retry; QaBuildRunner.cs cập nhật luồng xác nhận và điều kiện chạy tự động ẩn; Tools/Unity/run_standalone_qa.ps1 giữ đồng hồ thử nhất quán và cửa sổ ẩn. Các thay đổi chỉ phục vụ kiểm chứng/UI, không đổi domain economy/save schema/config.

Các phần chưa làm đúng chi tiết minh họa: slider âm lượng (vẫn toggle), texture khung gỗ/lá fantasy và icon minh họa cho từng achievement/stage, các item không có trong config,12 bước tutorial khác luật hiện hành. Không tuyên bố pixel-perfect hoặc bổ sung gameplay từ ảnh. Đây là nghiệm thu bố cục/chức năng dựa trên tài nguyên hiện có.

## Bảo toàn

Save thật trước/sau thời điểm kiểm tra: main B14C9D760F2A85F42A3CB8CCB89EF4DB5D50DD0214CF34C51AB8B36CDBDFA36A; backup 5006931C2BAB4BDE7C26988063D595C87E60AE010D1CEFA98020E798765BDD87. Không sửa ArtSource/FBX/model/raycast T10. MainScene chưa được chỉnh nội dung; composition được bootstrap tạo lúc runtime. Meta mới do Unity tạo.

Cuối kiểm tra Editor ngoài Play, MainScene sạch, SaveService.ValidationDirectory/ValidationUtcSeconds đều null. Hash MainScene BF2BA86D9097CD8E529FCABD54A60FE3EC534ACF3753548C253E9F6E063C5AB6.
