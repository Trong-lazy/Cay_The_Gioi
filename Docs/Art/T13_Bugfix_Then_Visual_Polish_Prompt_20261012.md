# Prompt gửi agent — sửa lỗi T13 trước, nâng đồ họa sau

Bạn tiếp tục dự án Cây Thế Giới tại `C:/Users/Asus/foder game 3D/Cay_The_Gioi`. Bản T13 vừa báo DONE chưa đạt nghiệm thu. Hãy đọc AGENTS.md, START_HERE.md, CURRENT_TASK.md, task T13, các quyết định liên quan và đặc biệt `Docs/Validation/T13_Independent_Review_20261012.md` cùng bằng chứng được dẫn trong đó. Đừng dựa vào chữ PASS cũ để bỏ qua lỗi nhìn thấy trong game.

Thực hiện hai giai đoạn theo đúng thứ tự. Chỉ một task hoạt động tại một thời điểm. Không cho phần nâng đồ họa chạy song song với sửa bug.

## Giai đoạn A — sửa bug và nghiệm thu lại

1. Xác nhận phiên Unity/project/scene và trạng thái task. Giữ Unity 6000.6.1f1, URP, Input System. Giữ GPU Resident Drawer Disabled trong PC_RPAsset; đọc báo cáo crash recovery. Không nâng phiên bản, xóa Library hoặc thêm package như bước sửa mặc định.
2. Bảo vệ save thật và asset của cả 3 cây bằng snapshot/hash trước/sau. Dùng save test riêng, kiểm tra ValidationDirectory/clock và khôi phục override khi xong. Không sửa mesh, rig, material, prefab, animation của cây chính để thích nghi môi trường.
3. Sửa B01: các tia nhọn và răng cưa ở đường đất/bờ cát. Xử lý biên mesh/material đúng, không che bằng cỏ. Không chỉ tăng độ phân giải tùy tiện rồi coi xong.
4. Sửa B02: dựng hồ, lòng hồ, bờ và thác thống nhất; không còn mép đĩa nước treo trên đất hay thác tách khỏi nguồn/đích. Đo lại các cạnh, kiểm tra ở nhiều góc. Nước được phép giao cắt bờ để kín, không được lộ rìa treo vô lý.
5. Sửa B03: bố cục/kích thước props phải tôn cây chính, không che thân/tán ở camera gameplay hợp lệ. Kiểm tra 3 stage × ít nhất 6 góc yaw, thêm pitch thấp/cao và xoay liên tục. Nếu cần chỉnh camera, giữ khả năng nhìn/tap cây và chứng minh không gây hồi quy; không tự đổi framing hàng loạt để né lỗi.
6. Sửa B04: hoàn thiện nền thành khối hình cầu/địa hình cầu kín đúng yêu cầu. Mesh hiện tại chỉ chỏm 82° có 96 cạnh biên hở. Kiểm tra normals, winding, collider, tiếp xúc gốc cây và đường chân trời; Cull Off không phải cách đóng đáy.
7. Sửa B05–B07: cụm hoa có tiếp xúc nền rõ ràng; normal thác hợp lệ, không dùng các tam giác đảo chiều chung vertex rồi tính normal; cơ chế lưu mesh không ghi đè biến thể nấm vì tên lặp. Preview, prefab và scene sau mở lại phải nhất quán.
8. UI là ưu tiên của người dùng: so sánh với baseline được chấp nhận gần nhất và ảnh nguồn. Nút tối đã xuất hiện trong Evidence/UI_Reference_20261010/final_main.png nên không tự kết luận là hồi quy mới hay thiết kế lại toàn bộ. Tìm và sửa đúng khác biệt; xử lý chữ thẻ Linh khí bị cắt. Giữ nguyên T12: nút nâng cấp hiển thị số nguyên liệu và tên tài nguyên, không +1. Nếu baseline mong muốn chưa có, hỏi đúng phần UI cần làm rõ và tiếp tục sửa môi trường độc lập.
9. Kiểm tra nghi vấn cây nổi bằng vị trí vertex/contact với mặt đất ở cả 3 stage; không dựa riêng vào shadow hay root anchor. Nếu sai, sửa nền/placement phù hợp, không sửa model cây được bảo vệ.
10. Chạy lại test EditMode/PlayMode có liên quan và thử trực tiếp MainScene: tap chuột/touch, kéo orbit không tính tap, UI chặn tap xuyên, upgrade/cap/thiếu tài nguyên, Evolution, Rebirth/DEAD, save/load, thời tiết/VFX/âm thanh. Với raycast, kiểm tra đường nhìn có props nằm trước cây; click vào một tảng đá khác phía cây không chứng minh chống click xuyên. Không đổi economy, HP, protection, xác suất sét hoặc thêm thời tiết.

Điều kiện kết thúc A:

- Tất cả lỗi xác nhận B01–B07 đã sửa hoặc có bằng chứng phản chứng cụ thể; UI theo baseline được xác định, không cắt chữ, chi phí nâng cấp đúng.
- MainScene cả 3 stage, 1920×1080 và 1366×768, các góc orbit/pitch: không gai đường đất, không mép nước treo, không props che cây hoặc camera clipping; gốc cây tiếp đất hợp lý.
- Compile sạch và Console không lỗi liên quan; test thực sự chạy, ghi số pass/fail và XML/log đúng bản cuối. Bổ sung test có ý nghĩa cho topology, normal và tính ổn định khi lưu mesh; không viết test chỉ để xác nhận hằng số hiện tại.
- Chụp Game View CÓ HUD và các panel chính, không chỉ camera render không UI. Chụp thêm góc kiểm tra hình học; lưu evidence trước/sau. Save thật và cây chính được bảo toàn.
- Mở lại scene sau lưu, vào/thoát Play Mode lặp lại để kiểm tra độ ổn định sau crash. Không tuyên bố hết mọi crash chỉ vì chạy ngắn không lỗi.
- Cập nhật task/validation đúng sự thật, sửa kết luận PASS cũ mâu thuẫn bằng chứng. Chạy Tools/Docs/update_progress.py. Nếu thiếu xác minh bắt buộc, ghi WAITING_VALIDATION; không chuyển sang B.

## Giai đoạn B — chỉ bắt đầu sau khi A đạt

Nâng chất lượng môi trường theo concept nhiệt đới huyền ảo người dùng chọn, giữ cây chính làm tâm điểm:

- Cỏ thấp mọc theo cụm có khoảng trống quanh gốc/lối mòn; độ cao và mật độ có cấu hình, bám pháp tuyến mặt cầu, không chen qua cây hay che UI.
- Dương xỉ, hoa và cây lá lớn dùng prefab/component có chất lượng tốt hơn; phối tỷ lệ lớn–vừa–nhỏ và tránh bố cục đơn điệu.
- Rêu/đá, bờ cát–đất–cỏ mềm hơn, màu xanh bớt gắt; nước ngọc lam, thác nhỏ và đom đóm vừa phải. Không thêm hệ thống ánh sáng cạnh tranh thời tiết.
- Được dùng asset/prefab/component/scatter tool sẵn có, không nhất thiết code toàn bộ. Kiểm tra tương thích Unity/URP, quyền sử dụng và hiệu năng; không mua asset hoặc nhập package lớn ngoài nhu cầu. Nếu phải thêm dependency, nêu cụ thể lý do và tác động.
- Mỗi nhóm thêm vào phải bật/tắt và thay thế dễ; scatter tất định, không tạo lại mỗi frame. Ưu tiên shared materials/meshes và giới hạn mật độ. Không sửa model cây chính hay gameplay.
- Bầu trời để sau cho người dùng chọn; không tự thay skybox/AllSky.

Sau B, nghiệm thu lại các điều kiện A để bảo đảm trang trí không tái tạo lỗi che cây, raycast, UI, save hoặc crash. Bàn giao rõ file thay đổi, lỗi đã sửa, ảnh trước/sau, test thực tế và giới hạn còn lại. Không đánh dấu DONE bằng mô tả mỹ thuật hoặc số test logic đơn thuần.
