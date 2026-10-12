# Bằng chứng T04

Các JSON là kết quả MCP/Unity thật, các PNG là ScreenCapture trong Play Mode và đã được xem. Không chứa nội dung save người chơi.

- editmode-final.json:93/93, T04 38 ca; playmode-final.json:28/28 ở FullHD, T04 12 ca; playmode-portrait-final.json:28/28 ở540×960.
- direct-save.json/direct-reopen.json: Stop/Play roundtrip, state/view/camera, delta online đúng production.
- direct-io-failure.json + io-failure-landscape.png: main/revision giữ, dirty UI/retry.
- direct-recovery-blocked.json/direct-recovery-confirmed.json + recovery-landscape.png/recovery-portrait.png: giữ main lỗi, xác nhận backup, checkpoint1000→1790958914, Water88 không thưởng, archive1.
- direct-reset-confirmation.json: hai bước, archive2, game mới Water10.
- saved-hud-landscape.png/restored-hud-portrait-insets.png/final-reopened-hud.png: HUD chức năng và bố cục.
- final-play-smoke.json/final-console.json/final-editor-state.json: main/config/version/URP/scene và smoke sạch. test-fault-console.json có lỗi cố ý T03 được LogAssert kiểm chứng.
- player-save-read-only.json/player-save-hashes-before-manual.json/player-save-preservation.json: default persistentDataPath Loaded/valid, hash trước thử trực tiếp và cuối phiên không đổi. Không sao chép save thật vào repository.
- playmode-first-expanded.json/playmode-layout-retry.json giữ các lượt fail trước sửa pointer/layout/encoding. playmode-portrait-orphaned.json/empty-run.json là lỗi khởi tạo Test Runner và0test, không tính PASS; lượt portrait-final thực sự28ca.

Touchscreen, SendMessage lifecycle và safe area là mô phỏng Editor; chưa điện thoại/build/native file replacement. Chi tiết scene/config/path và giới hạn trong Docs/Validation/T04_20261002.md.
