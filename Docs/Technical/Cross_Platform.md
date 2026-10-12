# Code dùng chung PC và điện thoại

Theo D12, game có hai nhóm nền tảng: PC Windows và điện thoại (Android và iOS). Hướng màn hình điện thoại đã chốt là Landscape (màn hình ngang) cố định; ưu tiên hoàn thiện và nghiệm thu PC Windows trước. Gameplay, config, state, schema save và MainScene dùng chung; chỉ input, bố cục, lifecycle và chất lượng render thích ứng thiết bị. Chưa chuyển build target hoặc cài SDK mobile khi đang làm PC.

Ưu tiên cập nhật 04/10/2026: nghiệm thu T09 Windows trước, mobile thiết bị thật DEFERRED tại Docs/Deferred/Mobile_Validation.md. Giữ code/input/UI dùng chung; chưa build/test mobile không chặn T09 Windows và không được ghi mobile PASS.

## Input và camera

Theo D51 (12/10/2026), cây có vùng tap hình cầu ổn định ở mọi stage, tách khỏi mesh animation. Các khe lá trong sphere là mục tiêu hợp lệ; bên ngoài model và sphere không nhận. Vẫn raycast vật cản rắn phía trước và chặn UI/gesture kéo. GameplayInputConfig chứa tâm/bán kính stage nhỏ và padding tự tính từ bounds stage lớn; thông số giữ trong snapshot cấu hình runtime.

Input adapter dùng Input System, gửi command chung cho domain. PC: click trái cây để tap, kéo chuột phải để orbit. Điện thoại: chạm cây để tap, kéo một ngón trên vùng chơi để orbit. Quy ước cảm ứng là PROTOTYPE_DEFAULT: ghi nhận tap khi thả nếu không vượt ngưỡng kéo; ngưỡng theo kích thước màn hình/DPI có fallback và nằm trong cấu hình, không dùng pixel cố định cho mọi thiết bị. Kéo không đồng thời nhận thưởng tap; chạm UI không đi vào cây/camera. Theo dõi pointerId và thao tác bắt đầu trên UI, hủy khi pause/focus loss; không nhận đôi touch và mouse mô phỏng, không tự thưởng khi giữ ngón. Đa chạm không tăng thêm tap hoặc giành quyền gesture đang hoạt động.

OrbitCameraRig T00 trước đây chỉ nhận Mouse.current. T01 đã chuyển đọc input sang TreeInputAdapter để tap và drag không xung đột; camera chỉ nhận delta. Input config có ngưỡng DPI/fallback và maximumTapSeconds; đa chạm hủy gesture, pause/focus loss hủy pointer, UI raycast theo vị trí. T03 tiếp tục kiểm tra orbit/khóa/chuyển stage với cả hai input. Touch T01 được kiểm tra bằng device events mô phỏng trong Editor, chưa là bằng chứng điện thoại thật.

## UI và ứng dụng vào nền

CanvasScaler thích ứng tỷ lệ màn hình; nội dung thao tác nằm trong safe area, nút dễ chạm và chữ đọc được. Không ép mọi điện thoại dùng 1920×1080. Chức năng thiết yếu có nút UI, không chỉ hover/chuột phải/Escape. T08C điều chỉnh hint theo loại input; hướng màn hình Landscape cố định đã được chốt và thiết lập trong ProjectSettings (tắt xoay dọc).

T02: Upgrade mở/đóng/+1 qua uGUI InputSystemUIInputModule cho mouse/touch; UI không sửa số dư và raycast chặn cây bên dưới. Panel/nút trong SafeArea, footer dọc nhường nút mở; chiều rộng panel co theo safe width. Đã kiểm tra Game View ngang1920×1080/dọc540×960 và inset mô phỏng, kể cả dòng consumption Leaves. Test Touchscreen UI chỉ là Editor simulation; chưa chứng minh kích thước chạm/notch trên máy thật. Panel không pause gameplay hoặc quỹ Sức sống.

Khi ứng dụng pause hoặc mất focus: chốt checkpoint/save theo chính sách T04/T05, ngừng tick online và thời gian Sức sống. Resume tính offline đúng một lần từ checkpoint; Sức sống đóng băng trong khoảng offline. Pause/focus callbacks có thể cùng xảy ra, không lưu/tính thưởng lặp. Không chỉ dựa vào OnApplicationQuit hoặc đường dẫn Windows. Không giả định iOS/Android có cùng API file replacement; kiểm tra cách ghi an toàn trên nền tảng mục tiêu.

## Kiểm tra và phạm vi nghiệm thu

- T01: click/chạm đều tăng counter đúng một lần; UI, giữ, kéo và đa chạm không thưởng sai; có test touch trong Editor, ghi rõ mô phỏng.
- T02: chuột/touch UI mở/mua/đóng, một command/level, chặn tap cây; safe area ngang/dọc và thông tin cost/output/consumption không cắt. Validation/T02_20261002.md ghi Editor PASS và giới hạn thiết bị.
- T03: camera orbit/lock/transition đúng với mouse và touch, không lẫn tap.
- T03 đã kiểm tra bằng Mouse/Touchscreen device events trong Editor: nút Evolution/Orbit dùng chung, UI-start không orbit/tap, touch drag không tap, transition khóa rồi trả input và giữ setting. Game View ngang/dọc/inset đã xem; chưa điện thoại thật. Khóa orbit tối thiểu ở panel Upgrade, toàn bộ Settings thuộc T08C.
- T04/T05: pause/resume/focus và offline không double claim, Sức sống không tiêu trong nền.
- T08C: panel/tutorial/setting chạm được, safe area/tỷ lệ màn hình không cắt nút; có đường thao tác không cần bàn phím.
- T09: build chạy độc lập trên PC và hệ điều hành điện thoại đã chốt; kiểm tra trên thiết bị thật, ghi model/OS/build/config, hiệu năng và lỗi. Editor simulation không thay bằng chứng thiết bị thật.

T00 đã nghiệm thu trên Editor/PC. Chưa có bản build điện thoại hoặc kiểm tra cảm ứng. Các bằng chứng lịch sử giữ nguyên; không tự tích DONE cho yêu cầu nền tảng chưa được thử.

## Nghiệm thu T04 trong Editor

Validation/T04_20261002.md:93 EditMode và28 PlayMode đạt ở Game View ngang1920×1080 và dọc540×960. Mouse/Touchscreen RetrySave nhận một command, UI không tap xuyên cây; lifecycle SendMessage pause+focus chỉ một save, không tick/tiêu quỹ trong nền, resume chưa có thưởng T04. Modal recovery và HUD đã xem ở ngang/dọc/inset mô phỏng. Save path Application.persistentDataPath, atomic File.Replace hoặc báo lỗi giữ main; chưa chứng minh Android/iOS. T05 mới thêm offline candidate/commit. Mô phỏng Editor không thay kiểm tra điện thoại/native lifecycle/build T09.


## Nghiệm thu T05 trong Editor

Validation/T05_20261003.md:109 EditMode,34 PlayMode ngang1920×1080 và34 PlayMode dọc540×960 PASS. Offline startup/resume lưu candidate/checkpoint trước input; lỗi giữ baseline và khóa gameplay, retry touch mô phỏng một lần, không tap xuyên popup/UI. Pause/focus SendMessage gộp một khoảng offline, bank/counter freeze, online +5% trở lại sau commit. Play→Stop→Play trực tiếp bằng clock/thư mục thử riêng cho report1800/1h và reopen0; popup safe area/inset Editor đã xem. Đã bỏ override cuối phiên. Không thay target/SDK; chưa có điện thoại thật/native lifecycle/atomic replacement mobile, vẫn cần T09. Popup thông tin sau commit không dừng online.

## Bằng chứng T06 Editor (03/10/2026)

Rebirth dùng một domain transaction chung PC/mobile, cùng Button UI cho mouse/touch; modal raycast chặn cây, cancel gesture và khóa camera, DEAD không đóng bằng Escape/chuột phải. SafeArea layout tested1920×1080 và540×960; ảnh dọc có simulated inset20,35,500,880. 41/41 PlayMode mỗi viewport, marker Screen thật; chuột/cảm ứng qua Input System device events, UI confirm không tap cây hoặc double point. Native Pointer tạm cách ly chỉ trong SaveTestScope, restore cuối; runtime adapter giữ nguyên.

Pause/focus/pending offline chặn Rebirth, retry catch-up phải commit trước modal Forced; reopen DEAD và Rebirth giữ bank/tap/settings. Tất cả là Windows Unity Editor hoặc touch/safe area/lifecycle mô phỏng, không bằng chứng điện thoại thật. D12 đã CONFIRMED PC/Android/iOS và Landscape cố định, ưu tiên PC Windows trước; nghiệm thu thiết bị điện thoại thật thực hiện khi có thiết bị. Validation/T06_20261003.md và Evidence/T06 là nguồn kết quả.
