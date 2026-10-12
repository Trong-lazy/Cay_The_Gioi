# Bằng chứng T01 — 02 tháng 10 năm 2026

Các JSON là phản hồi RPC thật từ Unity MCP đang kết nối Cay_The_Gioi@c16bfba113f4554d. Không tạo lại số pass hoặc log giả. File `mcp_client.py` dùng server localhost đang có khi bộ tool Unity không được desktop expose.

- `project-info.json`, `initial-editor-state.json`, `initial-console.json`: project, phiên bản, MainScene và Console đầu phiên.
- `editmode-final-result.json`: 29/29 passed, gồm 19 case T01 và 10 case hồi quy T00; job 8f2862f221e648529d3449f8db48a292.
- `playmode-final-result.json`: 6/6 passed, gồm 5 test T01 và 1 test T00 reload MainScene ba lần; job efe81731fb05455aa883143022c35210. Mouse/Touchscreen được tạo tạm qua Input System rồi gỡ; đây là mô phỏng trong Editor, không phải điện thoại thật.
- `direct-playmode-acceptance.json`: gọi service trên state MainScene đang Play, kết quả 10 Water, 4 Growth, 10.25 Water, quỹ 0/20/40 và cap 3600.
- `hud-portrait.png`, `hud-landscape.png`: Game View thật sau sửa layout/font/contrast; 540×960 và 1920×1080. Các file `*-state.json`/`*-size*.json` ghi state và kích thước; `landscape-final-state.json` còn ghi raycast đã hit Tree.
- `online-smoke-start.json`, `online-smoke-end.json`: tick theo frame thật trong Play với Root=1, Leaves=0, bank=0; thời gian quan sát và Water thực nhận.
- `final-console.json`, `final-editor-state.json`, `final-scene-state.json`: Console và trạng thái Editor/scene cuối phiên.

Giữ lần PlayMode thất bại đầu (`playmode-result.json`) và các collider/raycast diagnostic: CapsuleCollider của Cylinder đất dẹt có world extents 2.9 ở cả ba trục, hit trước cây. Đã thay riêng bằng BoxCollider khớp mesh qua Unity Editor API; lần retry và final pass. Ảnh `hud-portrait-initial.png` là trước tăng font/chỉnh contrast, không phải ảnh nghiệm thu cuối. Các lần start/retry là lịch sử kiểm tra, kết quả nghiệm thu lấy các file `final` nêu trên.

Chưa có Windows player build, Android/iOS build hoặc test thiết bị thật; không dùng bằng chứng này để tuyên bố T09/mobile build hoàn tất.
