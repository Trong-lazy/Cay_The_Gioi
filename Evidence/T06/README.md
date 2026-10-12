# Bằng chứng T06 — 03/10/2026

Nguồn kết quả: Docs/Validation/T06_20261003.md. JSON RPC/Unity tools thật, ảnh ScreenCapture thật đã mở xem; không chỉnh ảnh. Clock2000000000/temp save tách khỏi player; SHA256 before/after giống nhau.

- editmode-final.json: job8fbf756…133/133.
- playmode-final.json: jobb95166…41/41; landscape-test-console.json markers1920x1080.
- playmode-portrait-final.json: job45b051…41/41; portrait-test-console.json markers540x960.
- direct-seed/preview/cancel-io-failure/confirm/reopen/dead/dead-reopen/forced-confirm.json: diễn biến direct Play qua MCP/API/onClick, không phải bấm tay; device events trong Play tests.
- rebirth-preview-landscape.png, rebirth-io-portrait-insets.png (inset20,35,500,880), dead-landscape.png: ảnh đạt đã xem; portrait-viewport-confirmed.json xác nhận kích thước.
- console-final/editor-final.json: không error/warning, version/target/config/MainScene/Play settings/overrides/native Mouse cuối.
- player-save-before/after.json: main/backup không thay.

Chẩn đoán giữ nguyên: editmode-first-failed.json (Play settings drift), playmode-first-failed.json (physical pointer interfering), editmode-interim/playmode-landscape.json (lượt trung gian). playmode-*-empty.json total0 KHÔNG tính PASS. playmode-unverified-viewport.json và rebirth-io-invalid-viewport.png sai preset (3840×2160), KHÔNG bằng chứng portrait; ui-debug.png chỉ chẩn đoán. Touch/inset/lifecycle đều mô phỏng Editor; không bằng chứng phone/build thật.

Final settings check: `Evidence/T06/editor-settings-persisted.json` xác nhận cả API và file ProjectSettings/EditorSettings.asset là disabled/None (0/0); lưu thiết lập sau SerializedObject/SetDirty/AssetDatabase.SaveAssets, không sửa YAML thủ công.

console-followup.json và editor-followup.json: sau bàn giao phát hiện3 Error từ lệnh công cụ SaveToSerializedFileAndForget với EditorSettings persistent; không phải gameplay/compile/save game. console-final.json là snapshot trước các thao tác settings cuối, không khẳng định Console cuối phiên không lỗi. Xem đính chính Validation.
