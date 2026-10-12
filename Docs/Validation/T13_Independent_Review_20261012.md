# Nghiệm thu độc lập T13 — 12/10/2026

Task: T13
Result: FAIL

Chỉ review, ghi bằng chứng và prompt; không sửa code, asset hay scene game. Kiểm tra trực tiếp bằng Unity MCP bắt đầu 11/10, hoàn tất đối chiếu tài liệu 12/10. Không coi test logic PASS là nghiệm thu hình ảnh PASS.

## Phạm vi và giới hạn

- Đọc task, validation T13, code sinh mesh/prefab, UI layout và bằng chứng T09B/T12/T13.
- Probe trực tiếp MainScene trong Editor và Play Mode dùng save riêng; chụp Game View 1920×1080 có HUD; đổi model trình bày qua 3 stage trong Play Mode để đọc camera/anchor, không thực hiện giao dịch Evolution.
- Không chạy lại toàn bộ test suite trong lần review. 230 EditMode PASS là kết quả agent bàn giao; 11 PlayMode PASS của đợt crash recovery là kết quả trước đó, không dùng thay cho nghiệm thu đầy đủ bản cuối T13.
- Khi tiếp tục 12/10, Unity MCP không còn được cung cấp; Unity hiện là process mới khởi động 12/10. Không thao tác phiên mới để tránh can thiệp công việc người dùng. Các override SessionState của phiên kiểm tra trước không được chủ động khôi phục trước gián đoạn; chúng thuộc phiên Editor cũ. Agent phải kiểm tra lại ValidationDirectory/clock khi bắt đầu.
- Đối chiếu hash ngày 12/10: save chính và backup đều khớp `Evidence/T13_Independent_Review_20261011/save-before.json`.

## Lỗi và thiếu sót được xác nhận

### B01 — P1: Đường đất và bờ cát bị răng cưa thành các tia nhọn

- Hiện rõ trong ảnh người dùng, Game View mới `Evidence/T13_Independent_Review_20261011/current-seedling-ui.png` và ảnh agent `Capture_large_tree_Orbit_0deg.png`, `Capture_large_tree_Orbit_180deg.png`.
- `SphericalWorldTerrainBuilder.cs:158–176` phân loại cả tam giác theo tâm tam giác vào material đất/cát/cỏ; biên bị khóa theo lưới vòng tròn, tạo các nêm dài thay vì đường chuyển tiếp ổn định.
- Probe: mesh lưu trên đĩa và mesh sinh mới có vertices và cả 4 bộ triangle indices giống nhau. Ground cũ có renderer disabled. Không có bằng chứng đây là lỗi mesh chưa được lưu hay Ground cũ chồng lên.
- Sửa topology/biên vật liệu hoặc cách blend phù hợp; không che lỗi bằng thêm cỏ. Kiểm tra tĩnh và khi orbit ở cả 3 stage.

### B02 — P1: Mặt hồ không khớp lòng hồ/bờ

- Mặt nước là đĩa ngang ở Y=-0,48 trong khi đất cong theo cầu và bị lõm bởi hàm khác. Các đường biên không lấy cùng mặt cắt nước–địa hình.
- Probe 40 đỉnh vành nước trên mesh thực tế: chênh nước–đất từ -0,522 m tới +0,486 m; 20 điểm nổi hơn 0,1 m, 16 điểm bị chôn hơn 0,1 m. Dữ liệu: `water-boundary.json`.
- Một phần đĩa nước có thể giao cắt bờ là bình thường, nhưng mép ngoài dừng khi còn cao gần nửa mét trên đất tạo mép treo, không phải bờ hồ kín. Cần thiết kế lòng hồ/bờ bao hợp lý và xác minh mọi cạnh nước kết thúc trong bờ hoặc tại cửa thoát được thiết kế.
- Thác cũng dùng các độ cao hardcode riêng; kiểm tra nối đỉnh thác–đá–hồ, không mặc định đã liền mạch.

### B03 — P1: Props chiếm tiền cảnh, che cây chính ở góc orbit

- Ảnh agent `Capture_large_tree_Orbit_120deg.png`: cây lá lớn tiền cảnh bên phải che một phần tán cây chính và chiếm diện tích đáng kể. Không phù hợp acceptance cây chính không bị props che ở các góc gameplay.
- Probe camera mặc định: Mầm distance 1,4; Cây Non 2,8; Cây Lớn 6,2. Bố cục props dùng vị trí/kích thước cố định; khoảng trống theo bán kính gốc chưa bảo đảm hành lang nhìn của camera.
- Cần kiểm tra orbit đủ 360°, pitch 10–70° cho từng stage, raycast tại phần cây thực sự thấy/không thấy. Chưa kết luận đã có click xuyên lá trang trí: đó là ca phải bổ sung, không coi phép tap tâm hồ/đá khác phía cây là bằng chứng chống che khuất.

### B04 — P2: Địa hình mới là chỏm cầu hở, báo cáo nói không hở đáy không đúng

- `SphericalWorldTerrainBuilder.cs:22` dừng tại góc cực 82°, không tạo phần dưới/nắp đáy.
- Probe mesh thực: 4.609 vertex, 9.120 triangle, 96 cạnh biên chỉ được dùng bởi một tam giác, minY=-13,773. Không có tam giác quay xuống trong mesh này.
- Đây là thiếu hình học kín, không khẳng định mọi góc camera hiện tại đều thấy đáy. Phải hoàn thiện hình cầu/khối nền kín theo yêu cầu, kiểm tra đường chân trời và silhouette từ camera gameplay. Hai mặt material không thay cho việc đóng mesh.

### B05 — P2: Hoa là các mặt màu rời, tạo cảm giác lơ lửng

- Ảnh người dùng thấy nhiều miếng màu san hô nổi tách nền. `TropicalPropsBuilder.BuildCoralFlowerMesh` chỉ tạo các đĩa cánh ở Y=0,12/0,17, không có thân/đế/lá nối xuống nền; placement chỉ lún 0,03.
- Cần cho cụm hoa tiếp xúc đất hợp lý hoặc dùng prefab hoa hoàn chỉnh; tránh thay model cây chính.

### B06 — P2: Mesh thác có các tam giác trùng ngược chiều dùng chung vertex

- `SphericalWorldTerrainBuilder.cs:301–310`: thêm cả hai chiều của từng tam giác trên cùng bộ vertex rồi RecalculateNormals. Đóng góp normal đối nhau triệt tiêu, không phải cách dựng mesh hai mặt có normal hợp lệ.
- Đây là lỗi cấu trúc code được xác nhận; chưa đo normal của asset thác cuối bằng probe nên không gán toàn bộ biểu hiện ánh sáng hiện tại cho lỗi này. Sửa winding/normal và kiểm tra asset sau lưu, ánh sáng khi camera xoay. Nếu material đã Cull Off, không cần nhân đôi tam giác kiểu này.

### B07 — P2: Lưu mesh nấm theo tên không duy nhất, ghi đè biến thể giữa các cụm

- `TropicalEnvironmentSetup.cs:122–129` dùng GameObject name + mesh name làm đường dẫn rồi CopySerialized ghi đè asset có sẵn.
- `PlaceMushroomCluster` tạo Shroom_0, Shroom_1... lặp lại giữa cụm, nhưng capRadius/stemHeight được random theo seed từng cụm. Cùng tên không đồng nghĩa cùng mesh.
- Trên đĩa chỉ có Shroom_0..3_Glow_Mushroom_Mesh.asset; prefab có nhiều cụm 301/302/303 cùng các tên đó. Các instance đã tham chiếu cùng asset bị đổi theo lần ghi cuối, mất biến thể và không bảo đảm preview trước lưu giống sau lưu.
- Dùng khóa mesh ổn định bao gồm tham số/biến thể hoặc shared mesh chủ đích với transform. Không sửa GUID thủ công. Test build/rebuild/save/reopen không đổi hình ngoài dự kiến.

### B08 — P1 về nghiệm thu: DONE/PASS không phản ánh chất lượng thực tế

- Validation khẳng định không hở đáy, chuyển tiếp mềm và không che cây; B01–B04 chứng minh ít nhất các kết luận đó chưa đạt.
- Bộ Capture_* không có HUD, nên không chứng minh UI giữ nguyên. Checklist ban đầu yêu cầu cả PlayMode đã được đổi thành chỉ EditMode; bản bàn giao cuối không cung cấp lần PlayMode đầy đủ tương ứng bản cuối.
- Mở lại T13, bỏ tích các tiêu chí bị ảnh hưởng; không xóa lịch sử. Chạy lại kiểm tra có liên quan với ảnh Game View có HUD và dữ liệu đo thực tế.

## UI: phải xử lý yêu cầu người dùng, nhưng chưa được quy là hồi quy T13

- `Evidence/UI_Reference_20261010/final_main.png` đã có nút Cài đặt/Thành tựu hình chữ nhật tối, điểm trùng sinh nền tối và bố cục giống ảnh hiện tại. `ReferenceUiLayout.cs:117–120` chủ động xóa sprite các nút đó. Vì vậy không đủ bằng chứng chúng bị đổi bởi T13.
- Thẻ Linh khí bị cắt thông tin mở khóa (hiện “Cây Trưởng”) ở cả ảnh cũ lẫn ảnh mới: lỗi dễ đọc đã tồn tại trước đó, cần đưa vào vòng sửa UI hiện tại. Không tự coi đây là một tên stage đầy đủ.
- Agent phải đối chiếu bản UI được người dùng chấp nhận gần nhất/ảnh nguồn và asset skin; khôi phục đúng phần khác biệt xác minh được. Nếu người dùng muốn một baseline khác ảnh nghiệm thu 10/10 mà project không có, ghi rõ thiếu baseline và hỏi đúng điểm đó; vẫn sửa độc lập các lỗi môi trường.
- Bắt buộc giữ nhãn số lượng + tài nguyên trên nút nâng cấp theo T12; không trả lại +1, không đổi giá hay giao dịch. Review này chưa mở đủ các panel để kết luận toàn bộ UI khác còn đúng.

## Chưa kết luận là bug

- Cây Mầm trông nổi: root anchor có offset nhỏ 0,02 nhưng anchor không đồng nghĩa đáy mesh. Chưa có phép đo contact hình học đủ để khẳng định model bị nâng. Agent cần đo mesh–mặt đất, giữ model cây, sửa placement nền nếu sai.
- Không phát hiện runtime error/warning trong Console ở thời điểm probe. Không suy rộng thành mọi chức năng đều PASS.
- GPU Resident Drawer vẫn Disabled trên asset PC tại 12/10. Giữ workaround crash.

## Nâng đồ họa sau khi sửa lỗi

Sau khi các lỗi trên được sửa và kiểm chứng: cỏ thấp theo cụm, dương xỉ/cây lá lớn có tỷ lệ vừa phải, rêu đá, bờ đất–cát chuyển tiếp và nước dịu hơn theo concept nhiệt đới huyền ảo. Có thể dùng prefab/component/scatter tool tương thích URP và mặt cầu; không bắt buộc code mọi mesh. Không dùng props để giấu lỗi nền, không sửa cây chính hoặc chọn skybox thay người dùng. Lập bước nghiệm thu riêng sau vòng bugfix.
