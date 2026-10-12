# Bằng chứng T02, 02/10/2026

Các JSON là phản hồi thật từ Unity MCP local; ảnh do ScreenCapture trong MainScene Play. Scene/config/phiên và đối chiếu criteria ở Docs/Validation/T02_20261002.md.

- initial-editor-state.json, initial-console.json: kiểm tra đầu phiên.
- ui-setup.json: Unity dựng/lưu UI, giữ world/config.
- compile-console.json: compile sạch sau sửa fixture.
- editmode-start/result.json:46/46 PASS.
- playmode-start/result.json:11/11 PASS trước cải thiện clipping.
- playmode-layout-first-result.json: failure của phép kiểm tra layout với dimensions giả, giữ lịch sử.
- playmode-final-result.json:11/11 PASS sau sửa và kiểm tra text ở kích thước Game View thật.
- direct-playmode-acceptance.json: giao dịch, cost/shortage/lock/spam/rate/buff/protection/cap/dead trên MainScene đang Play.
- visual-scenario.json, landscape-size.json, portrait-layout.json, safe-area-layout.json: state và kích thước trong bước QA đầu.
- upgrade-landscape.png, upgrade-portrait.png, upgrade-portrait-insets.png: ảnh QA đầu; ảnh dọc đầu có clipping, không dùng làm kết quả cuối.
- upgrade-landscape-final.png, upgrade-portrait-final.png, upgrade-portrait-insets-final.png: ảnh cuối đã xem, đủ Water consumption, nút nằm trong safe area.
- portrait-final-layout.json: safe width1000/panel944, text preferred102/height110.
- upgrade-young-trunk.png: debug stage young_tree, TrunkL1/cap5 và Protection0.05; model Mầm vẫn giữ vì chưa Evolution.
- final-console.json, final-scene-state.json:0warning/error, đã thoát Play, scene không dirty, config prototype-1, Protection0.05.

Touch được mô phỏng bằng Input System Touchscreen trong Unity Editor. Chưa có bằng chứng điện thoại thật hoặc player build PC/mobile. Debug/TickDriver.ManualTick chỉ dùng runtime cho phép đo/ảnh, không lưu vào scene.
