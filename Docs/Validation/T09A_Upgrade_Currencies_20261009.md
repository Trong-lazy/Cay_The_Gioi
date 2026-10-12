# Nghiệm thu phân bổ tài nguyên nâng cấp

Task: T09A
Result: PASS
Date: 2026-10-10

Unity 6000.6.1f1 / WindowsEditor, Assets/_Project/Scenes/MainScene.unity, config prototype-6, Game View 1920×1080. Không đổi package/pipeline. Các giá/caps/nguồn sản xuất và vốn sau Rebirth là PROTOTYPE_DEFAULT của D47.

## Kiểm chứng

- EditMode: 220/220 PASS, job 03b1df5a97ea4bfdb1229382f7f4175a, Evidence/UpgradeCurrency_20261009/editmode_resume_results.json.
- PlayMode: 65/65 PASS, job b93025bdd5f24cca9c7e03791d168e96, Evidence/UpgradeCurrency_20261009/playmode_resume_results.json. Bao gồm mouse/touch, chặn UI, save/offline, shop và chuyển stage.
- Domain tests: đúng currency, thiếu tiền không mutation/event, commit trước event, chặn double-spend/độ chính xác, migration một lần, save roundtrip, chuỗi sản xuất và Rebirth.
- Console kiểm tra cuối: không có entry lỗi/cảnh báo; trước đó có 6 cảnh báo CS0618 có sẵn trong QaBuildRunner, không chặn compile.
- MainScene Play trực tiếp: mua Rễ/Lá/Thân L1 từ vốn W10/P5/G10, còn W5/P0/G0. Tick sản xuất và Evolution lần lượt young_tree, large_tree, mature_tree; trả đủ ba tài nguyên. Mở Spirit L1 trả 1000 W/P/G; sản xuất 16 Spirit rồi nâng L2 trả 16 Spirit. ForceSave và đọc state trên đĩa khớp. Rebirth trả seedling và vốn W10/P5/G10, Spirit0/locked/L0, nhận mature point1; mua lại cả ba L1 thành công.
- Log trực tiếp: manual_v6_transactions.json, manual_v6_rebirth.json, manual_v6_screenshot.json; ảnh economy_v6_before_ui.png. Tick được gọi qua ResourceSystem trong Play Mode, không mô phỏng thời gian thời tiết.
- Save thử cô lập tại Evidence/UpgradeCurrency_20261009/manual_v6_save; manual_v6_restore.json xác nhận override directory/UTC về null và scene không dirty.
- protected_before.json/protected_after_economy.json: MainScene, save thật và backup không đổi hash; config thay đổi có chủ đích.
- Docs/GDD_AI_Agent.docx xuất lại từ Markdown. Hidden Word COM xuất PDF và kiểm tra ảnh đủ 6 trang tại gdd_render_v6, không clipping.

## Hạn chế còn lại

Chưa build lại Windows sau economy; build lịch sử không đại diện config 6. Cây Trưởng Thành dùng prefab large_tree. Hai lỗi raycast T10 chưa nằm trong sửa economy; không tuyên bố đã sửa. Nghiệm thu mobile thật hoãn theo D12. Chi phí và cân bằng sản xuất cần playtest dài hơn sau bản UI.
