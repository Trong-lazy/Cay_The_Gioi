# Nghiệm thu độc lập và sửa riêng vùng tap — 12/10/2026

Task: T13
Result: IN_PROGRESS

Phạm vi: nghiệm thu và lập prompt các bug khác; chỉ sửa tap theo yêu cầu trực tiếp. Không sửa đồ họa, UI, model, vật liệu cây hoặc môi trường trong phiên này.

## Trước sửa

Unity MCP kết nối đúng Cay_The_Gioi, Unity 6000.6.1f1, MainScene. Console không có error/warning trước kiểm tra. Bộ PlayMode chạy trực tiếp: 81 ca = 80 PASS, 1 Ignored (baseline benchmark cũ), 0 FAIL. Job b2723e472a3542c8bb94aa77cc72a91f. Không gọi kết quả này là81PASS.

Code mới đã bật sphere ở Mầm/Cây Non nhưng vẫn tắt ở Cây Lớn. Ca bổ sung 50 gesture tại tâm cây, luân phiên mouse/touch, animation bật và năm yaw, đã PASS trước sửa (job6de53b8e0dab43fca46a628bd4069bb8). Vì vậy chưa tái hiện được nhận định “mọi click đều không nhận” với input mô phỏng; không khẳng định một nguyên nhân tuyệt đối. Yêu cầu vùng tap hình cầu được áp dụng cho cả ba stage nhằm tránh phải nhắm đúng mesh lá/thân và khe tán.

## Thay đổi riêng tap

- TreeView bật root SphereCollider ở tất cả stage, isTrigger để không tạo vật cản rắn. Mầm/Cây Non giữ vùng hiện có, chuyển thông số vào GameplayInputConfig; cây lớn tự tính từ bounds MeshRenderer và padding0.08m. Vùng ổn định theo stage, không recook mỗi frame.
- Input.Bind truyền snapshot cấu hình cho TreeView. OnTree chủ động truy vấn trigger tap, bỏ qua trigger trang trí khác, giữ chặn collider rắn phía trước. Không thay thời gian gesture, drag threshold, UI arbitration, economy hoặc VitalitySystem.
- Expectation T10/oracle theo yêu cầu mới: khoảng trống giữa lá bên trong sphere là tap hợp lệ; ngoài cả model và sphere vẫn không cộng. Giữ ca front occluder, mouse/touch, yaw/aspect và các ca UI/drag/lifecycle.
- TapInteractionReviewTests: 50 gesture/stage qua năm yaw, 25mouse+25touch, đúng một reward20s và rate Water1.05x; tap lệch tâm3stage×2weather×4yaw, chặn vật cản và nền xa. Hai ca mới PASS, jobfc786d0d47a0425cb4b3c765152e3241.

## Bug và khoảng trống nghiệm thu chuyển agent

1. Terrain vertex color không có tác dụng: SphericalWorldTerrainBuilder gọi mesh.SetColors nhưng material terrain dùng Universal Render Pipeline/Lit. Kiểm tra asset thật qua MCP và mã package LitForwardPass.hlsl: Attributes không có COLOR. Không thể coi dữ liệu colors là bằng chứng blend biome đang render. Agent cần nối shader/material hỗ trợ hoặc giải pháp blend khác, kiểm ảnh thực tế; chưa sửa.
2. FPS T14 không đo vòng chơi: VisualPolishBenchmarkTests gọi cam.Render60lần liên tục không yield trong một frame, Stopwatch đo render CPU rồi quy đổi FPS. Không bao gồm đầy đủ Update/input/animation/GPU/present; không chứng minh >=60FPS hay tăng5.38% khi chơi. Agent cần đo frame thực/Standalone, sửa nhãn và kết luận báo cáo; chưa sửa.
3. Benchmark đặt LogAssert.ignoreFailingMessages=true không có finally khôi phục. PASS không đủ chứng minh không runtime error; cần cô lập trạng thái rồi chạy lại.
   Khi chạy lại suite thật, Console xuất hiện lỗi native `UberPostProcess/Draw UIToolkit/uGUI Overlay: dimensions or sample count ... (1918 x 1006) vs (1366 x 768)`, `NextSubPass/EndRenderPass: Not inside a Renderpass` trong luồng chụp/render. Đây là lỗi đã quan sát ở kiểm thử render, chưa kết luận xảy ra khi chơi bình thường. Agent cần tái hiện riêng với thay đổi độ phân giải/targetTexture, kiểm screenshot và loại bỏ lỗi trước khi khẳng định Console0error.
4. T14 sửa màu material lá cây và nền trời trong khi T13 yêu cầu giữ material cây, bầu trời để user chọn. Đây là lệch phạm vi cần đối chiếu với ủy quyền thực tế, không tự rollback ở phiên này. Hash6FBX/prefab không bảo vệ material dùng chung.

T13 mở lại IN_PROGRESS; T14 WAITING_VALIDATION vì phụ thuộc và kết luận đồ họa/hiệu năng cần đánh giá lại. Chưa mở task đồ họa mới.

## Bằng chứng và giới hạn

Evidence/TapFix_Recheck_20261012/: protected-before.json, fifty-tap-results.txt. Chờ full PlayMode/EditMode sau sửa và hash cuối. Chỉ Editor Windows và touch mô phỏng; chưa nghiệm thu native Device Simulator routing, Windows Player mới hoặc điện thoại thật. Không tuyên bố trải nghiệm chưa thử đã PASS.
