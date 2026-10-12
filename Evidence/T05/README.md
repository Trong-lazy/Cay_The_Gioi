# Evidence T05 — 03/10/2026

Xem Docs/Validation/T05_20261003.md để đối chiếu acceptance và giới hạn. JSON được lấy trực tiếp từ Unity MCP, không dựng kết quả. Ảnh gốc ScreenCapture trong actual Play Mode, đã mở xem; không chỉnh sửa.

- editmode-final.json:109/109; playmode-final.json:34/34 FullHD; playmode-portrait-final.json:34/34 portrait. playmode-portrait-empty.json có0 test, không tính PASS. Các lượt đầu được giữ để truy vết.
- direct-seed/open/before-stop/immediate-reopen: clock thử và atomic file checkpoint. Runtime sau init đã có online tick, file/report đúng1800/1h; reopen0.
- direct-io-failure/ui-retry và direct-console-log: pending giữ baseline/file/revision, khóa commands, retry một lần. Listener gọi trực tiếp qua MCP; mouse/touch device events thực thi trong Play tests.
- offline-landscape/portrait-insets/io-retry-landscape.png: popup/readability/nút/safe area. Portrait540×960, inset20,35,500,880 mô phỏng Editor.
- final-console.json/final-scene.json:0 error/warning, clean MainScene/Editor stopped, overrides null. player-save-preservation.json:SHA256 main/backup trước/sau giống nhau; không chứa nội dung save người chơi.

Tất cả test file ở thư mục temp riêng. Không có bằng chứng điện thoại thật/native lifecycle/mobile atomic file API/Windows build độc lập. Không coi simulated touch hoặc SendMessage lifecycle là kiểm thử thiết bị thật.
