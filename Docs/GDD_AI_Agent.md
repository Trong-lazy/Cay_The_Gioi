# GDD Cây Thế Giới dành cho AI Agent

Tài liệu này xác định khung game cần triển khai, các luật phải giữ và đường dẫn để agent bắt đầu công việc. Ưu tiên một vòng chơi hoàn chỉnh bằng placeholder trong Unity trước khi làm đồ họa Blender. Đọc `START_HERE.md`, lấy task hiện tại từ `CURRENT_TASK.md` và chỉ chuyển bước sau khi nghiệm thu đạt.

Phiên bản tài liệu 1.3 ngày 09 tháng 10 năm 2026. Phạm vi là prototype dùng chung PC Windows, Android và iOS, một người chơi, một gameplay scene, chơi offline. Điện thoại Landscape cố định theo D12. Ưu tiên nghiệm thu Windows trước; build/test mobile thiết bị thật tạm hoãn tại Docs/Deferred/Mobile_Validation.md và không chặn T09 Windows. Đây là đặc tả thực hiện; tiến độ thực tế nằm trong các file task.

## 1 Nguồn luật và cách sử dụng

GDD rút gọn của Cây Thế Giới là nguồn thiết kế ban đầu. Bộ này bổ sung kiến trúc, mô hình dữ liệu, task và kiểm tra. Quyết định trực tiếp mới nhất của người dùng được ghi tại `Docs/Decisions.md`; giá trị tạm dùng để chạy prototype ghi là `PROTOTYPE_DEFAULT` trong `Docs/Technical/Prototype_Config.md`.

Ảnh hội thoại và góp ý bên ngoài là tham khảo. Không lấy con số, multiplier hoặc nhận xét trong đó thay luật mà chưa ghi quyết định. `PENDING_USER` chỉ chặn công việc phụ thuộc quyết định đó, không chặn toàn bộ dự án.

Markdown này là bản GDD chính. Word là bản xuất để đọc. Kiến trúc, schema và các task được duy trì tại file liên kết tương ứng, không sao chép thành những bản trạng thái khác nhau.

## 2 Mục tiêu khung game

Người chơi chăm một cây, tap để tích thời gian Sức sống tăng sản lượng và dùng sản xuất thụ động để nâng Root, Leaves và Trunk. Khi đủ điều kiện, người chơi chủ động Evolution để tăng stage, cap và unlock. Rebirth kết thúc vòng đời, trả cây về Mầm và trao đúng một point của stage vừa kết thúc. Weather tạo buff, debuff và rủi ro có kiểm soát; sét có thể gây Burning, giảm HP và buộc Rebirth.

Core loop: tap tích Sức sống và sản xuất thụ động → Upgrade → Evolution → cân đối sản lượng với Protection → Rebirth → hiệu ứng vĩnh viễn → vòng đời mới.

Khung giai đoạn hiện tại đạt yêu cầu khi loop, UI chức năng, save, offline, Weather, Rebirth và meta systems hoạt động, có kiểm tra và build Windows được nghiệm thu. Giữ code dùng chung điện thoại; build và nghiệm thu Android/iOS trên máy thật làm sau, không coi Windows PASS là mobile PASS. Chưa yêu cầu model chi tiết, texture, shader, hiệu ứng đẹp hoặc đầy đủ bảy stage concept.

## 3 Phạm vi và môi trường

- Unity hiện tại 6000.6.1f1, C#, URP, Input System, uGUI và Test Framework đã có trong project.
- Một `MainScene` chứa cây, đất placeholder, camera, Canvas, nguồn audio và bootstrapper.
- Logic dùng chung PC/điện thoại; chuột và cảm ứng gửi cùng command, UI thích ứng tỷ lệ màn hình/safe area. Tap không lẫn kéo camera hoặc chạm UI; save/checkpoint khi ứng dụng vào nền. Xem `Technical/Cross_Platform.md`.
- Bốn stage theo D47: Mầm, Cây Non, Cây Lớn, Cây Trưởng Thành. Mốc Trưởng Thành mở nhánh Linh khí; hình ảnh mốc này dùng placeholder trong task economy.
- Weather nhỏ gồm Bình yên, Nắng, Mưa nhẹ và Giông; một status Burning.
- Sáu Achievement và một cosmetic tạm để chứng minh toàn bộ luồng; cấu hình cho phép mở rộng lên 6–10 Achievement và 1–2 cosmetic như GDD gốc.
- Ngoài phạm vi: backend, tài khoản, ads, IAP, multiplayer, nhiều map và zoom thủ công. Setting ghi zoom Coming Soon.

## 4 Tài nguyên và bộ phận cây

Theo D47, Water, Quang hợp (`photosynthesis`) và Growth là ba số dư riêng. Rễ tạo Water; Lá tiêu Water tạo Quang hợp; Thân tiêu Quang hợp tạo Growth và tăng Protection. Chuỗi sản xuất/tiêu hao là mặc định prototype trong config, không phụ thuộc model cây.

Linh khí (`spirit`) mở ở Cây Trưởng Thành qua giao dịch mua nhánh hấp thụ bằng lượng lớn ba tài nguyên mặc định. Mở nhánh cấp hấp thụ1 để bắt đầu sản xuất Linh khí, các cấp sau trả Linh khí. Water/Quang hợp/Growth/Linh khí, level, HP và status thuộc vòng đời; Coin và stage point vĩnh viễn. Game mới cấp đủ vốn cho cấp0→1 của Rễ/Lá/Thân. Mặc định cấp lại vốn này khi Rebirth; không cấp Linh khí hoặc tự mở nhánh khóa.

Tap cây tích lũy bộ đếm để nhận thời gian Sức sống. Mỗi khi đủ nhóm tap, cộng thời gian vào quỹ rồi reset bộ đếm; quỹ tối đa 3600 giây. Ví dụ cấu hình tạm: 50 tap nhận 20 giây. Không mặc định tap còn trao Water; luật tap mới thay phần click yield tạm trước đó. Chỉ đếm khi cây còn sống và input gameplay được phép; click UI, modal hoặc ngoài mục tiêu tutorial không được đếm. Không tạo tap lúc giữ nút hoặc từ nhiều đường input cho cùng một click.

Sức sống là quỹ thời gian riêng, không phải HP hoặc giới hạn offline. Khi quỹ còn thời gian, tài nguyên sinh từ các nâng cấp được tăng 5%. Tap thêm chỉ kéo dài thời gian, không cộng chồng phần trăm. Quỹ được giữ qua Rebirth và giữ nguyên, không hoạt động hoặc bị trừ trong offline; khi online lại buff tiếp tục. D11 quy định chi tiết phạm vi và cách tiêu.

Production dựa trên thời gian đã trôi, level và modifier đúng phạm vi. Mỗi modifier được áp một lần. Logic production là hàm có thể kiểm thử, dùng chung quy tắc giới hạn Water cho online và offline; không phụ thuộc frame hoặc hiệu ứng hiển thị.

## 5 Upgrade và Evolution

Upgrade nâng một level khi đủ đúng tài nguyên, nhánh đã mở, cây sống và level nhỏ hơn cap. Rễ trả Water, Lá trả Quang hợp, Thân trả Growth ở mọi cấp. Giá tăng theo cấp và nằm trong config. Evolution trả cả Water/Quang hợp/Growth trong cùng giao dịch. Không cho số dư âm, vượt cap, trừ nhầm ví hoặc cấp miễn phí do mất độ chính xác. UI đọc quote để hiển thị giá, output, level/cap và lý do khóa; số K/M/B chỉ để hiển thị.

Evolution chỉ xảy ra khi bấm nút hợp lệ. Đủ tài nguyên không tự chuyển stage. Evolution đổi stage, cap, unlock, hình cây placeholder và camera trong cùng scene. Không trao point; chi tài nguyên không làm tụt stage. Quy tắc prototype giữ level và số tài nguyên còn lại sau khi trừ chi phí; stage cuối không có nút tiến hóa khả dụng.

Cây có thể chỉ là cylinder/cube và các khối tán màu. Mỗi stage có prefab/view config riêng để thay bằng model Blender mà không viết lại logic. Camera orbit quanh pivot ổn định, chuyển vị trí theo stage, tạm khóa input trong chuyển cảnh rồi mở lại.

## 6 Rebirth và tiến trình vĩnh viễn

Voluntary Rebirth mở theo config, có xác nhận và hủy. Forced Rebirth khi HP bằng 0 đặt run ở trạng thái DEAD, chặn sản xuất, click, Upgrade, Evolution và modal khác; phải xác nhận Rebirth mới tiếp tục.

Mỗi lần Rebirth hợp lệ trao đúng `+1` point của stage trước reset. Không cộng point các stage thấp hơn, không gộp ví point, không thưởng theo thời gian và không tăng cap. Thực hiện ghi điểm và reset như một giao dịch; bấm nhiều lần hoặc load lại không thưởng thêm.

Reset: Water, Quang hợp, Growth theo vốn config; Linh khí0/khóa, level, thống kê run, stage về Mầm, HP/status, unlock/cap theo Mầm và Weather về Bình yên. Giữ: point từng stage, effect, Coin, ownership/equip cosmetic, Achievement/claim, tutorial đã hoàn thành, setting, tổng Rebirth, mốc từng đạt và quỹ thời gian Sức sống.

UI xác nhận phải nêu stage hiện tại, đúng loại point nhận, dữ liệu giữ/reset. Coin không bị phạt khi chết và không dùng để cứu cây. Point effect riêng từng stage được cấu hình; không mặc định một multiplier chung.

## 7 Weather Protection và Burning

Chỉ một Weather gameplay hoạt động tại một thời điểm trong 4 trạng thái: Bình yên (`calm`), Nắng (`sunny`), Mưa nhẹ (`rainy`), Giông (`thunderstorm`). Trước unlock, môi trường trung tính. Weather kéo dài 180–600 giây; thời lượng Bình yên (120s) và hệ số buff/debuff là cấu hình prototype.

Nắng buff Photosynthesis (+20%); Mưa nhẹ buff Water (+20%). Theo quyết định D49: mỗi đợt Giông đủ điều kiện gây hại (stage mở harmful weather và exposure > 0, tức Cây Lớn trở lên; Mầm và Cây Non exposure = 0 được bảo vệ an toàn tuyệt đối) có **đúng 10% khả năng xảy ra một cú sét đánh cây**. Xác suất 10% được xét **duy nhất một lần** cho mỗi đợt Giông; tối đa 1 cú sét gây hại trong đợt. Protection không thay đổi xác suất trúng; Exposure không nhân tỉ lệ mà chỉ là điều kiện mở khóa. Cú sét gây hại được lên lịch đánh sau 30% thời lượng đợt Giông (`strikeDelayFraction = 0.30`) và lưu bền vững vào `WeatherState` / save DTO; offline catch-up tuyệt đối không chạy sét, không tính sát thương Burning và không làm chết cây.

Sét trúng chọn một vùng Root/Leaves/Trunk bằng ID và anchor view, gây trạng thái Burning (10 giây, gốc 2 HP/giây). Protection từ Thân và point vĩnh viễn giảm sát thương Burning:
`damagePerSecond = baseBurningDamagePerSecond / (1 + max(0, totalProtection))` (PROTOTYPE_DEFAULT).
Nếu trúng Leaves, Burning giảm 30% sản lượng Quang hợp (hệ số 0.7x theo D47). Vào Bình yên dập tắt Burning nhưng không tự hồi HP.

HealthSystem chỉ phát sự kiện chết một lần khi HP <= 0. Cây DEAD dừng gameplay thông thường, hiện UI Forced Rebirth (không cho hủy để tiếp tục chơi cây chết), lưu trạng thái qua save/load và không nhận sản xuất offline. Sau Rebirth, HP hồi về 100, weather reset về calm, dọn sạch VFX/âm thanh và lịch sét run cũ.

Presentation thời tiết (D49):
- Animation cây: Lá đung đưa nhẹ tự nhiên ở Bình yên/Nắng/Mưa; ở Giông, lá rung mạnh, thân và cành uốn lượn theo gió giật Perlin noise, gốc cây giữ cố định tại mặt đất; anchor vị trí bám sát biến dạng visual; bảo toàn góc xoay -90° X gốc Blender và đồng bộ Physics.
- VFX & Âm thanh: Hạt mưa procedural rơi dày; sét trang trí trên trời độc lập 0 damage; sét đánh trúng anchor có chớp sáng và rung giật cây; hiệu ứng cháy lõi lửa/khói rõ rệt; tiếng mưa/gió loop procedural crossfade mượt; sấm rền và sét đánh theo event; tích hợp đầy đủ pause, DEAD, Rebirth và setting âm thanh.

## 8 Save và offline

Schema được thiết kế ở task nền tảng trước khi viết gameplay. Save cơ bản triển khai sau Evolution; offline triển khai sau Save; Rebirth và Weather bổ sung giao dịch/schema rồi kiểm tra hồi quy save.

Lưu run, vĩnh viễn, tutorial, setting, timestamp, version, config ID và Weather/status. Không lưu GameObject, reference ScriptableObject, Coroutine, event subscription hoặc preview tạm. Dùng JSON với DTO có version và ID ổn định; file save nằm trong `Application.persistentDataPath`, có ghi tạm, backup và xác minh trước thay file chính.

Offline đã được người dùng chốt ở D02: tối đa 24 giờ, bằng 50% tốc độ sản xuất cơ sở online, không tính Weather hoặc click/tap. Giới hạn thời gian là 86400 giây; hệ số sản xuất là 0.5. Luật này thay giới hạn 8 giờ trong GDD gốc.

Các quy tắc offline đã rõ: không tính click/tap, buff Sức sống, buff/debuff Weather, Burning damage, random sự cố, tự Evolution, point hoặc cây chết khi vắng mặt. Giữ effect vĩnh viễn đúng phạm vi trong base production. Cây DEAD nhận 0. Weather và status đã lưu tiếp tục khi trở lại online với thời gian còn lại, không chạy nhanh toàn bộ thời gian vắng mặt.

Tính thưởng offline đúng một lần, ghi resource và checkpoint mới thành công trước khi hiện thông báo. Thời gian UTC âm không tạo thưởng; thời gian vượt giới hạn chỉ nhận phần cho phép rồi cập nhật checkpoint cho toàn khoảng đã xét. Thiết kế xử lý lỗi và test tại Save_Data_Schema.

Tap cộng thời gian Sức sống có cap một giờ theo D11; cap chỉ áp cho quỹ Sức sống. Bộ đếm tap reset sau mỗi nhóm hợp lệ, không dùng cửa sổ chơi một giờ và không giới hạn thời gian offline. Tutorial phải giải thích bộ đếm, thời gian nhận và cap.

## 9 UI tutorial và camera

HUD gồm tài nguyên run, stage, HP/status khi cần, Coin và các nút panel. Upgrade ở dưới phải, Shop dưới trái, Setting/Achievement trên trái, Weather trên phải, Rebirth gần trên giữa. Ưu tiên chữ rõ và thao tác đúng, chưa cần style chi tiết.

Mỗi lần mở panel đóng panel trước. UI input không xuyên cây. Chuột phải kéo orbit; khóa khi setting yêu cầu, Evolution đang chuyển hoặc tutorial chặn. Escape đóng theo ngữ cảnh nhưng không bỏ qua modal DEAD hoặc bước tutorial bắt buộc.

Độ ưu tiên tương tác: DEAD modal → tutorial target → modal thông thường → panel → world. Toast ở lớp hiển thị cao nhất, dùng hàng đợi và không chặn raycast. Khi DEAD xuất hiện trong tutorial hoặc preview, đóng preview, khôi phục cosmetic đã equip và ưu tiên xác nhận Rebirth.

Tutorial chính: mở Upgrade → nâng Root → nâng Leaves → mua Thân cấp1 bằng Growth khởi đầu → Evolution đầu tiên. Bước hoàn thành theo kết quả hành động hợp lệ, không theo chỉ click nút. Sau đó hướng dẫn từng hệ thống khi unlock/tiếp cận. Giữ các bước đã hoàn thành qua Rebirth và load.

## 10 Achievement Coin Shop và âm thanh

Coin chủ yếu nhận từ Achievement, chỉ mua cosmetic. Claim mỗi Achievement một lần, cập nhật Coin và trạng thái claim trong cùng giao dịch. Tiến độ vĩnh viễn theo milestone/counter có ID, không reset nhầm theo run.

Shop hỗ trợ preview, mua và equip. Preview không trừ Coin, không lưu và phải khôi phục equip đã cam kết khi đóng, chết, Rebirth hoặc đổi panel. Mua cập nhật ownership; equip là thao tác cam kết riêng. Cosmetic không tăng production, HP hoặc Protection.

Setting giữ nhạc nền và khóa orbit. Audio tối thiểu gồm loop và SFX ngắn nếu có asset hợp lệ; đặt giới hạn SFX click. Ghi nguồn/quyền sử dụng asset vào Evidence. Không tạo phụ thuộc asset ngoài mạng cho build offline.

## 11 Kiến trúc và dữ liệu

Đọc `Docs/Technical/Architecture.md` cho ranh giới hệ thống và API; `Data_Model.md` cho config/state/DTO; `Save_Data_Schema.md` cho schema, checkpoint, giao dịch và lỗi; `Prototype_Config.md` cho số tạm.

Bootstrapper nối các hệ thống, TickDriver cung cấp thời gian online. Các service thực hiện mutation qua API; view chỉ phản ánh state. State commit trước event; subscriber không ghi resource hoặc trao thưởng. Dùng dependency trực tiếp và event C# nhỏ, không xây framework tổng quát.

GameState có run ALIVE/DEAD. Khóa UI và chuyển cảnh thuộc presentation state riêng. Không dùng một enum duy nhất ép tutorial, weather và panel thành các trạng thái loại trừ nhau.

## 12 Thứ tự công việc

1. T00 nền tảng kiến trúc, schema và placeholder.
2. T01 tài nguyên, tap/Sức sống, passive production và HUD.
3. T02 ba nhánh Upgrade.
4. T03 Evolution và camera stage.
5. T04 save/load cơ bản, gồm bộ đếm tap và quỹ Sức sống.
6. T05 offline production theo D02 với cap 24 giờ và hệ số 50%.
7. T06 Rebirth và permanent effects.
8. T07 Weather, Risk, Protection, Burning và Forced Rebirth.
9. T08A Achievement/Coin; T08B Shop cosmetic; T08C tutorial/Setting/audio.
10. T09 tích hợp, tuning và nghiệm thu build Windows placeholder. Mobile thiết bị thật tạm hoãn theo D12. Đồ họa Blender là công việc sau khi khung Windows đạt nghiệm thu.

Trạng thái và phụ thuộc chính xác nằm trong từng file `Docs/Tasks/`. Code xong chuyển `WAITING_VALIDATION`; chỉ `DONE` sau compile, Unity Play Mode, acceptance, hồi quy và ghi bằng chứng. Khi không có Unity, bàn giao rõ phần đã làm và phần chưa thử.

## 13 Kiểm tra và tiêu chí khung hoàn chỉnh

- Không resource âm, cost sai, cap vượt hoặc modifier tính hai lần; kiểm tra với thời gian tick khác nhau. Sức sống +5% chỉ áp khi còn thời gian, không stack, dừng đúng ranh giới thời gian và giữ qua offline/Rebirth.
- Evolution chủ động đổi đúng state/view; không thưởng point và không chạy lặp khi đang chuyển.
- Rebirth nhận đúng point stage, reset/giữ đúng, không thưởng lặp.
- Offline bị giới hạn theo quyết định đã chốt, mô phỏng cả bốn tài nguyên theo luật economy, không nhận lại khi reopen ngay.
- Save/load khôi phục run, vĩnh viễn, tutorial, setting, DEAD, Weather/status; preview không thành equip.
- Giông/Risk không phụ thuộc FPS; Bình yên trung tính; Burning kết thúc đúng và chết chỉ phát một lần.
- Coin claim/mua/equip đúng; UI và tutorial không cho thao tác xuyên hoặc vượt điều kiện.
- Build Windows chạy offline, một scene; UI, mouse, pause/resume/save/offline không lỗi chặn loop, có bằng chứng Standalone thật. Giữ kiểm tra hồi quy input touch/safe area dùng chung; nghiệm thu Android/iOS trên máy thật DEFERRED, không chặn T09 Windows và không được coi Editor/PC PASS là mobile PASS.

Ghi kết quả thực tế theo `Docs/Validation/Template.md`. Mời 3–5 người playtest khi có build; đo thời gian tới Evolution, nhịp nâng cấp, lựa chọn Protection và tần suất sự cố. Không ghi kết quả giả để hoàn tất tài liệu.
