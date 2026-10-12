> Hiện hành ngày 09/10/2026: prototype-6 theo D47; bảng/lời giải prototype1–5 bên dưới là lịch sử khi mâu thuẫn với mục này.

# Cấu hình prototype

Các số dưới đây là PROTOTYPE_DEFAULT để khung chạy được, chưa là kết quả balance/playtest. Ngoại lệ đã CONFIRMED: cap offline 86400 giây, offline factor 0.5, cap quỹ Sức sống 3600 giây, buff Sức sống 5% và luật +1 point đúng stage. Config hiện tại prototype-5 (T09A phân bổ tài nguyên nâng cấp); T00–T02 dùng prototype-1. Mỗi lần đổi tuning cập nhật version và Validation. T03 không đổi các số economy.

## Economy

- Start: Water 10, Growth 0, Root/Leaves/Trunk level 0, HP 100, stage seedling; Coin và point 0.
- Root mỗi level cung 1 Water mỗi giây.
- Leaves mỗi level dùng 0.25 Water mỗi giây hoạt động để tạo 1 Growth mỗi giây hoạt động. Không có Water, Leaves dừng phần chưa được cấp; lượng Water mới tạo trong tick được dùng.
- T09A/D47: Root dùng Water; Leaves L0→L1 dùng Water để khởi động khi Growth=0, từ L1→L2 trở đi dùng Growth; Trunk dùng Growth. Loại tài nguyên nằm trong `rootCostResourceId`, `leavesFirstCostResourceId`, `leavesCostResourceId`, `trunkCostResourceId`, chỉ nhận stable ID `water`/`growth`. Base vẫn 5/5/10; cost tại level hiện tại L là base nhân 1.6 mũ L, làm tròn lên một lần bằng hàm thống nhất. Đây là PROTOTYPE_DEFAULT, chưa qua balance/playtest người chơi.
- T02 dùng UpgradeSystem.TryCalculateCost/GetQuote/TryUpgrade. Cap0 khóa nhánh, cap>0 mở theo stage; quote cũ hoặc chuỗi K/M/B không cấp quyền giao dịch. `trunkProtectionPerLevel=0.05` nằm trong PrototypeRules/PrototypeConfig.asset; đưa số đã ghi ở phần Weather vào config, không đổi tuning/version. Quote Leaves là Growth tiềm năng khi đủ Water; consumption không buff. Protection T02 chỉ tính base Trunk; effect point triển khai T06.
- Passive production chưa có storage cap Water/Growth. Guard số hữu hạn/tràn số. Không lấy giá trị UI rút gọn để tính.
- Permanent effect: mỗi point seedling tăng Root Water 5%; mỗi point young_tree tăng Leaves Growth 5%; mỗi point large_tree cộng Protection 0.02. Point không đổi cap và không buff tất cả tài nguyên chung.

## Sức sống từ tap

- tapsPerReward 50, secondsPerReward 20: số tạm lấy từ ví dụ người dùng.
- maxBankSeconds 3600: cap đã chốt. tapProgress từ 0 đến 49. Tap thứ 50 reset counter về 0 và cộng min(20, phần còn trống); nếu đã đầy quỹ thì không vượt cap. Phần dư bị bỏ là PROTOTYPE_DEFAULT, cần hiển thị “Sức sống đã đầy”.
- Không trao Water thêm cho tap nếu người dùng chưa yêu cầu.
- vitalityOutputMultiplier 1.05 cho Water/Growth từ upgrades khi online ALIVE và bank >0, áp một lần. Không tăng Water consumption, Protection/HP/Coin/point. UI có đơn vị giây hoặc mm:ss và counter, tutorial giải thích nhóm tap/cap và tăng 5%.
- Consume một giây quỹ cho một giây online ALIVE, không pause chỉ vì mở panel; DEAD đóng băng là mặc định tạm. Offline không consume, không buff. Rebirth giữ bank và counter. Tick đi qua thời điểm quỹ cạn phải chia đoạn có buff/không buff, tránh cấp +5% cho toàn dt lớn.

## Stage

### Input T01 (PROTOTYPE_DEFAULT)

Trong `PrototypeConfig.asset/Input`: `dragMillimetres=2`, `fallbackDragScreenFraction=0.012`, `minimumDragPixels=4`, `maximumDragScreenFraction=0.04`, `maximumTapSeconds=0.4`. Ngưỡng lấy DPI khi hợp lệ, fallback theo cạnh ngắn màn hình và giới hạn theo kích thước màn hình. Thả sau 0.4s là giữ, không nhận tap; kéo vượt ngưỡng rồi quay lại cũng không nhận tap. Mouse trái/touch gửi cùng TryTap; chuột phải/touch kéo gửi Orbit. Hai ngón hủy gesture cho đến khi tất cả đã thả. Economy vẫn version `prototype-1`, không thay số T00.

Menu Editor `Cây Thế Giới/T01/Debug` đặt level/số dư/quỹ runtime để kiểm thử; không mua Upgrade, không sửa asset config hoặc dữ liệu lưu. Run mới vẫn Root/Leaves=0, Water=10; dùng Root 1 Leaves 0 để thử passive production trước T02.

T02 đã có nút Upgrade để mua Root/Leaves từ Water đầu run. Menu `Cây Thế Giới/T02/Debug/Young Tree State (no Evolution)` đặt stage young_tree/ALIVE/Water1000 để thử Trunk; `Seedling Reset` về run mới. Chỉ runtime, model không đổi và không trả thưởng Evolution. Mở panel vẫn consume Sức sống/tick như D11; manual tick chỉ dùng trong test/QA.

| ID | Cap Root Leaves Trunk | Unlock | Chi phí đi tới stage |
| --- | --- | --- | --- |
| seedling | 5 5 0 | Root Leaves | Run mới |
| young_tree | 10 10 5 | Trunk Rebirth Weather tốt | 50 Water và 25 Growth |
| large_tree | 20 20 10 | Giông và damage | 200 Water và 100 Growth |

Evolution điều kiện chi phí và cây ALIVE, không có level prerequisite tạm. Giữ level/resource còn lại. Camera/view thông số thử trực tiếp để cây đọc được dưới HUD; không đóng băng tỉ lệ pixel theo model tạm.

T03 lấy chi phí từ stage ĐÍCH: young_tree50 Water/25 Growth, large_tree200/100. GetQuote/TryEvolve là thao tác chủ động; stage cuối FINAL_STAGE. Transition lock tạm không lưu DTO, chặn Evolution/Upgrade/tap/orbit trong blend nhưng production và quỹ vẫn tick. Camera dùng yaw hiện tại; setting orbitLocked giữ qua chuyển stage. Panel có nút khóa orbit tối thiểu để thử trên điện thoại, chưa triển khai toàn bộ Settings T08C.

Presentation config T03: `evolutionTransitionSeconds=0.8` (unscaled), `cameraTargetHeight` theo stage0.65/1.1/1.9; giữ distance3.3/6.8/10.5, pitch25/25/28. Camera blend SmoothStep tới pose config; khi controller bị disable thì snap tới pose stage đã commit rồi mở khóa. TreeView giữ ModelSlot/anchors, thay prefab, chỉnh sphere collider và vị trí anchors theo Renderer bounds của model mới. Menu T03/Debug/Fund Next Evolution chỉ thêm số dư runtime cho thử nhanh, không tự Evolution hoặc lưu asset/save.

## Offline và Water

Offline dùng thời gian mô phỏng economy bằng min(elapsed, 86400) nhân 0.5. Áp cho cung Root, thời gian hoạt động/nhu cầu Water và đầu ra Leaves cùng một lần; không giảm tốc độ hai lần. Do đó Water cần cho Growth nhất quán với online và không có lỗi giảm đầu ra nhưng tiêu 100% Water.

Dùng neutral weather/status/vitality context, giữ permanent effects. Khi số dư Water thiếu, Growth có thể thấp hơn lấy rate HUD nhân giờ; phải tính lượng Water thực có. Với hệ số offline 50% và thiếu nước, không hứa mỗi tài nguyên cuối đều đúng 50% chênh lệch online 24h vì online có thể có bonus và số dư đầu kỳ; 50% là tốc độ mô phỏng cơ sở.

## Weather và Health

- Bình yên 120 giây, các Weather khác random 180–600 giây, chỉ một Weather tại một thời điểm.
- Nắng Growth multiplier 1.2, Mưa Root Water multiplier 1.2; Giông neutral production trong prototype nhưng có Risk.
- Stage exposure seedling 0, young_tree 0, large_tree 1.0; prototype chưa gây sét trước large_tree.
- Protection = TrunkLevel nhân 0.05 cộng permanent Protection effect. ProtectionModifier = 1 / (1 + Protection).
- Base strike rate Giông 0.001 mỗi giây; finalRate = baseRate nhân exposure nhân protectionModifier. Probability cho khoảng dt = 1 trừ exp của âm finalRate nhân dt. Không roll xác suất cố định mỗi frame.
- Strike cooldown 120 giây online; tối đa một Weather xấu liên tiếp, sau Weather xấu chuyển Bình yên; không lặp Weather khác Bình yên hai lượt liên tiếp.
- Burning 10 giây, damage 2 HP mỗi giây; nếu vùng Leaves thì Growth multiplier 0.7 trong Burning. Lượng damage phải dừng tại HP 0 và event chết một lần.
- Khi vào Bình yên xóa Burning, không hồi HP. Khi Rebirth HP về max, Weather Bình yên, status none.
- Để kiểm tra Forced Rebirth, có debug command Editor hạ HP hoặc ép strike. Giá trị tuning cố ý làm death hiếm, không bắt người kiểm thử chờ RNG tự giết cây.

## Meta

- Achievement tạm: Root lần đầu, Leaves lần đầu, Evolution tới young_tree, tới large_tree, Rebirth lần đầu, nhận thưởng Sức sống lần đầu. Mỗi cái Coin 10, claim một lần.
- Cosmetic glow_placeholder giá 20 Coin, chỉ thay màu/VFX view; không effect gameplay. Preview khác visual Burning.
- Music mặc định bật; orbit mặc định không khóa. Audio placeholder có nguồn hợp lệ hoặc controller chịu thiếu clip, không làm fail loop vì asset chưa có.

## Save T04

- `PrototypeRules.autosaveIntervalSeconds`: PROTOTYPE_DEFAULT 30 giây; cấu hình trong PrototypeConfig.asset. SaveController không ghi mỗi frame, dừng timer trong nền; pause/focus/quit và stage Evolution đã commit có save riêng.

## Config hiện hành sau T06 (03/10/2026)

PrototypeConfig.asset dùng `prototype-3`, giữ toàn bộ tuning production/upgrade/evolution/offline/input hiện có. `initialCalmSeconds=120` trong rules thay hard-code factory; run mới/Rebirth Water=initialWater10, Growth=initialGrowth0, HP100/level0. `pointEffects` gồm stable stage/target IDs: seedling/water/0.05, young_tree/growth/0.05, large_tree/protection/0.02 (target IDs xem PrototypeRules). Effect production cộng dồn độc lập thành 1+points×perPoint; Protection cộng points×perPoint. Validate finite/nonnegative/unique và đủ ba stage/đúng target. Không dùng point tăng cap. Runtime deep snapshot giữ tuning/preview/reset nhất quán dù Inspector đổi trong Play. Schema1 saves prototype-1/2 được validate và nâng stamp3 ở lần ghi kế; config lạ vẫn blocked. Xem D18 và Validation/T06_20261003.md.

## Config prototype-5 ngày 09/10/2026

T09A thay loại tài nguyên upgrade theo D47, không đổi production, Water consumption, Evolution, Coin, cap, giá hoặc Rebirth. Quote mang CostResourceId; TryUpgrade kiểm tra và trừ đúng số dư, commit trước event. Leaves đầu dùng Water nên Root→Leaves mua được từ Water10/Growth0 kể cả sau Rebirth; các lượt sau Leaves/Trunk dùng Growth. Save schema1/prototype-1–4 được validate và giữ toàn bộ state; lần ghi kế stamp prototype-5. UI đọc quote và ghi đúng Water/Growth, không tự trừ tài nguyên.


## Config prototype-6 (D47, hiện hành)

Tuning **PROTOTYPE_DEFAULT**; loại tiền Rễ/Water, Lá/Quang hợp, Thân/Growth và Evolution cả ba là **CONFIRMED**. Water10/Quang hợp5/Growth10 mỗi run mới, level0; Linh khí0/khóa. Rễ Water1/s/level; Lá Quang hợp1/s/level tiêu Water0.25/s/level; Thân Growth1/s/level tiêu Quang hợp0.25/s/level và Protection0.05/level. Base upgrade5/5/10, factor1.6. Cả ba nhánh mặc định có từ Mầm.

| Stage | Cap Rễ/Lá/Thân/Hấp thụ | Chi phí Evolution vào stage (Water/Quang hợp/Growth) |
|---|---|---|
| seedling | 5/5/5/0 | 0/0/0 |
| young_tree | 10/10/5/0 | 50/25/25 |
| large_tree | 20/20/10/0 | 200/100/100 |
| mature_tree | 30/30/20/10 | 500/250/250 |

Mature cho mua hấp thụ1000/1000/1000. Giao dịch mở cho hấp thụL1; output Linh khí1/s/level, cấp sau trả Linh khí với base10/factor1.6. Rebirth reset toàn bộ ví run/nhánh theo config, giữ meta/Sức sống/settings. Point mature target spirit0.05.

Nắng1.2 và Burning Lá0.7 áp Quang hợp. Growth point áp Thân; buff Sức sống1.05 áp các đầu ra, không tiêu hao/Protection. Offline24h50% tích phân cùng chuỗi, không weather/status/Sức sống. Visual mature tạm dùng large prefab, không sửa art sources.
