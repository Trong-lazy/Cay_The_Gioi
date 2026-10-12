# Bằng chứng T03, 02/10/2026

JSON là phản hồi thật Unity MCP local; PNG do ScreenCapture trong MainScene Play và đã xem. Đối chiếu acceptance ở Docs/Validation/T03_20261002.md.

- initial-editor-state.json / initial-console.json: MainScene,Unity6000.6.1f1,Console sạch đầu phiên.
- compile-console.json: sạch sau sửa debug access.
- editmode-initialization-timeout.json: chưa khởi chạy ca nào với timeout15s, giữ lịch sử.
- editmode-result.json:56PASS trước tăng version; editmode-final-result.json:56/56PASS prototype-2.
- playmode-first-result.json:16PASS bản đầu; playmode-final-result.json:16PASS sau kiểm tra disable snap và gesture trong transition.
- playmode-portrait-final-result.json:16PASS ở Game View dọc, thêm kiểm tra tap collider Cây Lớn.
- playmode-version2-final-result.json:16/16PASS trên config cuối prototype-2.
- direct-initial-play.json:run mới Mầm10Water,0Growth,thiếu Evolution.
- direct-first-evolution.json:50/25 trừ đúng0/0,BUSY/input lock, tick trong blend vẫn tạo4.75Water/2.1Growth và tiêu1giây bank.
- direct-young-completed.json:young_tree/model1/camera6.8,height1.1,caps10/10/5,input mở.
- direct-second-evolution.json:200/100 trừ0/0,giữ levels/quỹ.
- direct-large-completed.json:large_tree/model1,camera10.5/28,height1.9,FINAL_STAGE,point0/0/0.
- portrait-safe-area.json:Game View540×960,inset mô phỏng,safeWidth1000,panel944×900.
- seedling-panel.png,young-tree.png,young-panel-portrait.png,young-panel-insets.png,large-panel-portrait.png,large-tree-portrait.png,large-tree-landscape.png:QA stage/UI ngang/dọc/inset, đủ nút/cost/output/reason. Các ảnh/probe chain ở prototype-1 với tuning T03 mới, sau đó version tăng2; số camera/economy không đổi giữa ảnh và test cuối.
- fault-injection-console.json:log thiếu prefab có chủ đích trong test recovery, LogAssert.Expect; config được khôi phục. Log được lưu trước clear Console rồi chạy Play bình thường mới để xác nhận không còn lỗi runtime.
- fresh-play-smoke.json:khởi động sạch sau fault test, trước tăng version.
- direct-version2-evolution.json / direct-version2-completed.json:giao dịch50/25 và view/camera/input thật trên prototype-2 sau test cuối.
- final-console.json / final-scene-state.json:0error/warning,thoátPlay/notcompiling,MainScene không dirty,Validate config0error,version2.

Mouse/Touchscreen tests dùng device events trong Editor. Chưa có player build hoặc điện thoại thật. Runtime debug/manual tick không lưu vào scene/config; Game View đã trả lựa chọn trước. Không bịa log hoặc ảnh từ test không chạy.
