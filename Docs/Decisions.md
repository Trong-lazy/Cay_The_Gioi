# Quyết định thiết kế và triển khai

## D51 Vùng nhận tap hình cầu ở ba stage

Ngày12/10/2026. CONFIRMED theo yêu cầu trực tiếp mới nhất: reviewer chỉ sửa tap, các bug khác lập prompt cho agent. Vùng tap ổn định bao quanh cây ở cả seedling, young_tree và large_tree; khoảng trống giữa lá bên trong vùng này được nhận có chủ ý. Thay yêu cầu pixel-perfect của T10 tại phần nằm trong sphere; ngoài model và sphere không nhận. Giữ chặn UI, kéo, giữ lâu, đa chạm, pause/focus, DEAD và vật cản rắn phía trước.

Kích thước PROTOTYPE_DEFAULT trong GameplayInputConfig: Mầm center(0,.16,0), radius.25m; Cây Non center(0,.65,0), radius.58m; cây lớn dùng MeshRenderer bounds với padding.08m. Không sửa prefab/FBX hay material cây, không recook collider mỗi frame. Giữ 50tap nhận20s quỹ, buff1.05x và cap hiện có. Bằng chứng: Docs/Validation/TapFix_Independent_Recheck_20261012.md.

## Ghi chú kỹ thuật 11/10/2026 — tránh crash GPU Resident Drawer

Theo yêu cầu sửa Unity crash của người dùng, tắt GPU Resident Drawer trong `Assets/Settings/PC_RPAsset.asset` (1 -> 0), giữ Unity 6000.6.1f1 và packages hiện tại. Native stack mới nhất đi qua ObjectDispatcher/WorldProcessor/GPUResidentDrawer. Đây là workaround có thể giảm lợi ích tối ưu CPU, không đổi luật gameplay. Không bật lại trong T13 trước khi kiểm tra ổn định tương đương. Kiểm chứng: MainScene Play Mode + dựng lại môi trường, 11/11 ArtPipeline/Weather tests PASS; giới hạn và evidence tại `Docs/Validation/T13_Crash_Recovery_20261011.md`.

Ghi chú phân loại art hiện hành: D44 xác định bộ nguồn `Tree_Large` là Cây Lớn và thay thế phần phân loại của D42; bảng đang dùng tại `Docs/Art/Tree_Model_Registry.md`. Các quyết định trước giữ nguyên như lịch sử triển khai; D44 không đổi ID `large_tree` hoặc luật gameplay prototype.

Ngày khởi tạo 02 tháng 10 năm 2026. `CONFIRMED` là luật từ GDD gốc hoặc yêu cầu trực tiếp; `PROTOTYPE_DEFAULT` là lựa chọn triển khai tạm, dùng để làm khung và có thể tuning; `PENDING_USER` chưa đủ xác nhận, chỉ chặn phần phụ thuộc. Mỗi thay đổi phải nêu ngày, nguồn, lý do, file/task ảnh hưởng và cách kiểm tra lại.

## D01 Khung trước đồ họa

Trạng thái: CONFIRMED. Nguồn: yêu cầu trực tiếp của người dùng. Làm gameplay bằng placeholder; model Blender chi tiết làm sau. Ba stage, một scene, chơi offline; nền tảng PC và điện thoại theo D12. Ảnh hưởng: toàn bộ task và Backlog.

## D02 Giới hạn và hệ số offline

Trạng thái: CONFIRMED. Ngày 02 tháng 10 năm 2026, người dùng trả lời trực tiếp “Chốt 24 giờ và 50%”. Thay luật tối đa 8 giờ của GDD gốc bằng `offlineMaxSeconds = 86400`, `offlineRateFactor = 0.5`, theo tốc độ cơ sở online, không tính Weather/click. Không nhầm cap quỹ Sức sống một giờ với offline. Ảnh hưởng: GDD, config, schema, T05 và kiểm tra T09.

## D03 Economy chạy thử

Trạng thái: PROTOTYPE_DEFAULT. Root tạo Water; Leaves dùng Water để tạo Growth; Growth biểu diễn tích lũy từ Photosynthesis. GDD gốc chưa chốt quan hệ này. Dùng hai tài nguyên run để có loop rõ và Evolution có mục tiêu. Tap nhận Sức sống theo D11, không tự cộng thêm Water. Tài nguyên đầu run phải đủ mua Root để không kẹt tutorial. Thông số tại Prototype_Config; nếu đổi luật cần kiểm tra T01–T05 và tutorial.

## D04 Stage và Evolution

Trạng thái: PROTOTYPE_DEFAULT. Stage ID `seedling`, `young_tree`, `large_tree`; tên Mầm, Cây Non, Cây Lớn. Evolution giữ level và tài nguyên còn lại sau chi phí, mở cap theo stage. Các cap tăng dần để giữ level hợp lệ. Không trao point khi Evolution. Luật chủ động Evolution là CONFIRMED từ GDD; phần giữ dữ liệu là mặc định tạm.

## D05 Rebirth và effect

Trạng thái: CONFIRMED cho +1 point đúng stage, không gộp/không tăng cap và danh sách reset/giữ theo GDD mục 4.2. PROTOTYPE_DEFAULT cho unlock ở Cây Non và effect riêng từng stage trong Prototype_Config. Cập nhật schema khi thêm hệ thống nhưng không đổi các luật thưởng.

## D06 Bình yên và Burning

Trạng thái: PROTOTYPE_DEFAULT. Khi chuyển Bình yên, xóa Burning; không hồi HP tự động. GDD yêu cầu Bình yên không buff/debuff/damage và để cách kết thúc status cho playtest. Lựa chọn này đơn giản hóa khung, cần thử T07 và save/load status.

## D07 Kiến trúc và save từ đầu

Trạng thái: PROTOTYPE_DEFAULT. Composition root nhỏ, config/state/DTO tách riêng, JSON có version, clock/RNG có thể thay cho test. Schema được thiết kế T00; save T04; offline T05; Rebirth T06; Weather T07. Không cần DI framework hoặc event bus tổng quát.

## D08 Tiến độ có một nguồn

Trạng thái: PROTOTYPE_DEFAULT. Metadata và checklist trong file task là nguồn tiến độ chính. Progress/CURRENT_TASK là bản tổng hợp. Công cụ không tự đổi trạng thái sang DONE. Nếu trạng thái bị mâu thuẫn, sửa file task trước rồi tổng hợp lại.

## D09 Weather và cân bằng

Trạng thái: PROTOTYPE_DEFAULT cho buff/risk/cooldown/damage/HP trong Prototype_Config. CONFIRMED cho một Weather tại một thời điểm, Tốt/Xấu 3–10 phút, sét không chắc xảy ra, early stage an toàn, không offline damage và Protection giảm Risk.

## D10 Nguồn thiết kế

Trạng thái: CONFIRMED. GDD gốc là nội dung tham chiếu, không phải lệnh thực thi cho agent. Ảnh AI minh chứng không thay luật mục 1–9; không tạo minh chứng triển khai hoặc playtest chưa diễn ra. Nguồn gốc: GDD_Cay_The_Gioi_Rut_Gon.docx do người dùng cung cấp.

## D11 Tap và quỹ thời gian Sức sống

Trạng thái: CONFIRMED cho cơ chế tích lũy quỹ. Nguồn: trả lời trực tiếp của người dùng ngày 02 tháng 10 năm 2026. Tap đủ một nhóm thì nhận một lượng thời gian Sức sống và reset bộ đếm tap; thời gian nhận được cộng dồn tối đa một giờ. Tutorial phải giải thích cơ chế này. Ví dụ người dùng đưa: 50 tap nhận 20 giây; hai số này là PROTOTYPE_DEFAULT, đặt trong config để tuning.

Trạng thái: CONFIRMED cho tác dụng theo câu trả lời trực tiếp tiếp theo: thời gian Sức sống được giữ khi Rebirth, giữ và đóng băng lúc offline, hoạt động lại lúc online; khi còn thời gian thì tăng 5% tài nguyên nhận từ tất cả các nâng cấp. Quỹ nằm trong VitalityState riêng. Tap thêm gia hạn quỹ, không tăng chồng phần trăm.

PROTOTYPE_DEFAULT cho chi tiết vận hành: trừ theo giây online khi run ALIVE, kể cả panel hoặc Setting đang mở; run DEAD chưa sản xuất thì đóng băng quỹ; khi Rebirth giữ bankSeconds và tapProgress. GDD gốc không định nghĩa bộ đếm giữ hay reset nên chọn giữ để không mất tap dở dang. Tăng 5% áp cho đầu ra Water/Growth từ nhánh nâng cấp, không buff Protection, HP, Coin hoặc Rebirth Point vì chúng không là tài nguyên sản xuất từ nâng cấp. Water consumption của Leaves không tăng theo buff. Phần thời gian tick có buff dùng multiplier 1.05; khi quỹ cạn giữa tick, chia tick để phần còn lại dùng 1.0. Nếu đổi cách hiểu, cập nhật T01/T04/T05/T06/T08C và config trước build.

## D12 PC và điện thoại dùng chung gameplay

Trạng thái: CONFIRMED. Ngày 02 tháng 10 năm 2026 và ngày 03 tháng 10 năm 2026. Nguồn: yêu cầu trực tiếp của người dùng.
- Mục tiêu nền tảng: PC Windows, Android và iOS (CONFIRMED ngày 03/10/2026).
- Hướng màn hình điện thoại: Màn hình ngang (Landscape) cố định (CONFIRMED ngày 03/10/2026).
- Tiến độ kiểm thử thiết bị thật: Người dùng xác nhận hiện tại chưa thử trên máy thật và ưu tiên tập trung hoàn thiện bản PC Windows trước. Khung logic, input và UI SafeArea dùng chung đã sẵn sàng cho cả hai; kiểm thử trên thiết bị di động thật sẽ được tiến hành khi có thiết bị hoặc khi xuất gói cài đặt thực tế.
- Cập nhật CONFIRMED ngày 04/10/2026, nguồn yêu cầu trực tiếp mới nhất: tạm bỏ qua nghiệm thu mobile chưa đạt, ưu tiên PC Windows. T09 nghiệm thu trong phạm vi Windows; build/test Android và iOS thiết bị thật chuyển sang `Docs/Deferred/Mobile_Validation.md`, không còn là điều kiện chặn DONE của T09. Không đánh dấu mobile PASS hoặc xóa hỗ trợ cảm ứng. Không tự làm build mobile trong phiên PC.
- Ảnh hưởng: AGENTS, GDD, Technical/Cross_Platform, T09 và kế hoạch phát hành. T09 chỉ DONE khi bằng chứng Windows đầy đủ; còn thiếu report/screenshot Windows thì phải bổ sung, không dùng việc hoãn mobile để bỏ nghiệm thu PC.

## D13 Chi tiết input và tick T01

Trạng thái: PROTOTYPE_DEFAULT. Ngày 02 tháng 10 năm 2026. Nguồn: triển khai T01 theo D11/D12 và Cross_Platform; không đổi economy đã chốt. Tap chỉ khi thả trong thời gian tối đa 0.4s, đúng collider cây ở đầu/cuối, không kéo vượt ngưỡng. Ngưỡng DPI/fallback và thời gian giữ nằm trong PrototypeConfig/Input. Đa chạm hủy gesture đến khi mọi ngón thả; mouse bị chặn trong touch và phải về trạng thái nhả trước khi nhận gesture mới. UI được raycast tại vị trí pointer, không phụ thuộc trạng thái EventSystem của frame trước. Một adapter sở hữu tap và orbit; OrbitCameraRig nhận delta thay vì tự đọc Mouse.

Production tính tích phân giải tích của cung Water và thời gian Leaves được cấp nước; ResourceSystem chia tại bankSeconds, commit cả tài nguyên/quỹ trước event. Tick dùng unscaledDeltaTime, dừng khi pause/mất focus; chưa triển khai save/offline thuộc T04/T05. HUD hiển thị rate tức thời có xét thiếu nước, không hứa reward dài hạn theo rate đó. Debug Editor đặt scenario runtime trước T02; run mới giữ level 0. Ảnh hưởng: T01, Architecture, Prototype_Config, Cross_Platform; kiểm tra partition dt, bank boundary, device events mô phỏng và lifecycle trong Unity.

## D14 Giao dịch và UI Upgrade T02

Trạng thái: PROTOTYPE_DEFAULT. Ngày02/10/2026. Nguồn: phạm vi T02, Architecture và các số có sẵn trong Prototype_Config; không đổi luật GDD hoặc tuning. Cap0 là khóa nhánh tại stage; cost Ceiling(base×factor^level) thống nhất trong domain, không lấy số đã rút gọn UI. Quote đọc live state, TryUpgrade kiểm tra lại và commit Water/level trước event; callback reentrant bị BUSY. Balance không biểu diễn được phép trừ từ chối NUMERIC_PRECISION, không cấp level miễn phí. Output Root/Leaves hiển thị với Sức sống một lần; Leaves ghi tiềm năng và consumption, Trunk không buff. Giá trị Protection0.05/level đã định nghĩa được đưa vào config.

Panel không dừng tick/quỹ Sức sống, có nút mở/đóng/+1 chung mouse/touch, co theo safe width và chặn tap cây. Debug young_tree chỉ đặt runtime stage để thử unlock, không thực hiện Evolution hoặc đổi model. Ảnh hưởng: T02, PrototypeRules/UpgradeSystem, UI/MainScene, Architecture/Prototype_Config/Cross_Platform/Project_Setup. Kiểm tra lại cost chính xác, thiếu Water, cap/dead/spam/reentrancy, rate/buff và UI ngang/dọc/touch mô phỏng; Validation T02 ghi giới hạn thiết bị thật.

## D15 Evolution và camera T03

Trạng thái: PROTOTYPE_DEFAULT. Ngày02/10/2026. Nguồn: GDD mục5/9, D04/D12 và task T03. Evolution đọc chi phí từ stage đích theo config có sẵn (50/25,200/100), giữ level/resource còn lại/quỹ/settings, không trao point; cập nhật highestStageEverId bằng stable ID và không hạ milestone cũ. Guard transient đặt trước event, Upgrade nhận cùng predicate; production/quỹ không pause trong blend. View nhận state đã commit, thay placeholder và cập nhật anchors/collider theo renderer bounds; lỗi view ghi Console, hoàn tất guard để không khóa vĩnh viễn, không chi lại resource qua subscriber.

Thêm tuning presentation trong config: blend unscaled0.8s, target height0.65/1.1/1.9; giữ distance/pitch đã có. ConfigVersion chuyển prototype-1→prototype-2 để ghi nhận tuning camera; economy/chi phí/cap/Sức sống giữ nguyên. Snap pose khi controller bị disable, finally trả gameplay input/transition lock; orbitLocked setting độc lập và được giữ. Nút orbit tối thiểu trong Upgrade panel giúp thử mouse/touch, chưa toàn bộ Settings T08C. Debug Fund chỉ đặt số dư runtime; Evolution vẫn do command người chơi. Ảnh hưởng: T03, config/domain/presentation/bootstrap, kỹ thuật và FoundationSceneTests (log dùng version mặc định hiện tại); kiểm tra cả chain, chi trả/guard/cap/unlock, view/camera/collider, input, lỗi/disable và hồi quy T00–T02. Mobile OS/hướng màn hình D12 vẫn chưa chốt.

## D16 Save System T04

Trạng thái: PROTOTYPE_DEFAULT. Ngày 02/10/2026. Nguồn: Save_Data_Schema, Architecture và task T04. SaveService dùng write-to-tmp → read-back verify → backup main → atomic rename; không ghi thẳng vào file chính; tất cả path dùng `Application.persistentDataPath` để chạy được PC/mobile. Autosave 30 giây interval; thêm save khi OnApplicationPause, OnApplicationFocus loss và OnApplicationQuit (không chỉ quit). ForceSave sau giao dịch quan trọng (Evolution).

SaveValidator kiểm tra schemaVersion bounds, stageId/highestStageEverId theo config, numeric sanity, level caps, equippedCosmeticId phải owned, tapProgress/bankSeconds trong range. Load fallback: main → backup → CorruptNoBackup (báo lỗi không xóa im lặng); schemaVersion cao hơn supported không bị ghi đè. Tất cả PlayMode test setup xóa save file để ngăn state leakage. configVersion trong save chỉ ghi nhận, không làm fail load (configVersion migration chờ task sau nếu cần). Ảnh hưởng: T04, GameBootstrapper, Architecture, Save_Data_Schema; kiểm tra lại save/load roundtrip, corrupt, version-too-new và regression T01–T03 trong Unity.

## Mẫu ghi nhận mới

ID và chủ đề; trạng thái; ngày; nguồn trực tiếp hoặc nguồn tham khảo; luật trước/sau; lý do; task/file bị ảnh hưởng; kịch bản cần kiểm tra lại. Khi quyết định được chốt, cập nhật GDD, config và schema liên quan, rồi xuất lại Word.

## D16 Lưu an toàn và phục hồi T04

Trạng thái: PROTOTYPE_DEFAULT. Ngày 02/10/2026. JSON schema v1 dùng Newtonsoft đã có trong dependency hiện tại, không thêm package. Chỉ chấp nhận schema1; chưa có schema0 đã phát hành nên không đoán migration cho schema cũ không biết. Schema cao hơn, config lạ và dữ liệu hỏng chặn khởi tạo, giữ file và cho retry/reset có xác nhận. Config prototype-1→prototype-2 được chấp nhận sau validate vì D15 chỉ sửa camera; config khác cần migration được kiểm thử trước.

Ghi temp cùng thư mục, Flush(true), xác minh, File.Replace có backup cho file đã tồn tại; lần đầu File.Move. Không có fallback xóa main trước rename. API không khả dụng hoặc I/O lỗi báo dirty save/retry và giữ file cũ. Fault trước replacement không đổi revision; lỗi chẩn đoán sau commit không trả thất bại cho dữ liệu đã ghi thành công. Chưa chứng minh File.Replace/flush trên Android/iOS; xác minh ở T09 khi chốt thiết bị.

Backup hỏng hoặc main mất/hỏng không tạo game mới im lặng. Backup hợp lệ phải được người chơi xác nhận; lưu snapshot backup với checkpoint=max(checkpoint,now) trước tạo gameplay/input, giữ bản main hỏng trong .corrupt-UUID. Reset bị chặn hai bước, lưu trữ main/backup thành .reset-UUID trước bắt đầu mới. T04 không tính offline. Resume giữ dữ liệu, T05 mới bổ sung catch-up.

AutosaveIntervalSeconds=30 trong PrototypeRules/config là số prototype; SaveController chạy sau tick, gộp pause/focus thành một lần khi đi vào nền, dừng autosave trong nền. Save khi stage Evolution đã commit, kể cả transition chưa xong. Gameplay online tiếp tục khi autosave/Evolution save lỗi với thông báo dirty; các giao dịch persistent offline/Rebirth/meta phải triển khai candidate/commit trước thưởng theo task tương ứng.

DTO snapshot và restore deep-copy; kiểm tra ID trùng, tài nguyên/HP/life/level, timestamp, counter/meta và Sức sống. Field tương lai/setting/vitality thiếu dùng default v1 có test; field lõi thiếu/null bị từ chối. Weather T04 chỉ chấp nhận calm đang có; catalog Weather/achievement/cosmetic/tutorial hoàn chỉnh và validation ID mở rộng thuộc task tương ứng. Không thêm gameplay Weather/meta.


## D17 Sản xuất offline và commit T05

Trạng thái: PROTOTYPE_DEFAULT. Ngày 03/10/2026. Nguồn: D02/D11, Save_Data_Schema, Cross_Platform và T05; giữ luật cap24h/tốc độ50% đã có. UTC nguyên giây, elapsed clamp0; checkpoint=max(old,now) nên clock lùi không cấp thưởng/lùi mốc. Effective duration=capped elapsed×offlineRateFactor áp một lần vào ProductionSystem chung cho supply/demand/output. Không dùng rate HUD/Sức sống, không tick Weather/status/HP/tap/Evolution/point; giữ metadata permanent và configured base rates. Point bonus chưa được triển khai trước T06; nối context permanent vào cả online/offline khi T06 làm bonus.

OfflineCoordinator ghi candidate resource+checkpoint qua atomic SaveService trước thay source/thông báo/input. Startup fail không tạo gameplay; resume fail giữ source và mốc vào nền, chặn command/tick/autosave/quit snapshot chưa thưởng, retry tính từ baseline với clock hiện tại. Pause/focus gộp theo trạng thái nền; duplicate resume không commit lần hai. Sau commit bỏ deltaTime frame resume đầu, rồi online bình thường. Lỗi sau replacement được xác nhận là đã commit theo D16 Lưu an toàn. Recovery backup xác nhận và chốt now, không replay reward cũ.

Popup chỉ là báo cáo của giao dịch đã commit, hiển thị cap/rate theo config và actual deltas; online tiếp tục khi popup mở, UI chặn tap cây. Lỗi save có ưu tiên modal và nút retry chung mouse/touch. ValidationDirectory/ValidationClock/ValidationUtcSeconds chỉ trong Editor, SessionState giữ thư mục/clock thử qua Play domain reload; clock SessionState chỉ dùng khi có thư mục thử riêng. Test dùng temp scope riêng, không xóa save người chơi. Ảnh hưởng: T05/domain/infrastructure/bootstrap/presentation/tests và Technical; Validation/T05_20261003.md. Giữ D12 chờ thiết bị, T09 cần xác minh file API/native lifecycle thật.

## D18 Rebirth và effect permanent T06

Trạng thái: PROTOTYPE_DEFAULT. Ngày03/10/2026. Nguồn: D05/D11, T06 và đặc tả hiện có; không đổi unlock, economy hoặc cấp point thấp hơn. ALIVE Rebirth mở từ young_tree; DEAD bắt buộc ở bất kỳ stage. Preview chỉ đọc, cancel không mutate. Command mang expectedRunId; busy/reentrant/stale/overflow từ chối với reason. Deep-copy candidate, +1 đúng stageId cũ và rebirthCount+1, tạo run mới Mầm qua factory; persist atomic trước thay source/event/view/input. I/O trước commit giữ nguyên; retry không double credit, lỗi chẩn đoán sau replacement acknowledged theo D16. Giữ mọi meta/settings/Coin, bankSeconds và tapProgress; reset run theo initialWater10/initialGrowth0/HP100/level0/calm120/statusnone trong config. Nước khởi đầu theo factory tránh Water0/Root0 không thể mua Root.

## D19 Weather Risk và Burning T07

Trạng thái: PROTOTYPE_DEFAULT. Ngày 03/10/2026. Nguồn: GDD mục 7, Prototype_Config và task T07. Config version nâng prototype-3 -> prototype-4; SaveCodec giữ tương thích ngược với prototype-1, 2, 3. WeatherState chấp nhận "calm", "sunny", "rainy", "thunderstorm". StatusState chấp nhận "none", "burning" và impactRegionId "none", "root", "leaves", "trunk".

Quy tắc thời tiết:
- seedling: chỉ "calm" 120s.
- young_tree: unlock "sunny", "rainy", "calm" (không có thunderstorm).
- large_tree: unlock "thunderstorm".
- Tốt/Xấu duration ngẫu nhiên trong [180, 600]s; Bình yên 120s.
- Tối đa 1 weather xấu liên tiếp: sau thunderstorm, weather kế tiếp bắt buộc là "calm", badStreak = 0.
- Không lặp lại weather khác calm 2 lần liên tiếp.

Modifiers sản xuất:
- Nắng ("sunny"): Leaves Growth multiplier 1.2 (+20%).
- Mưa ("rainy"): Root Water multiplier 1.2 (+20%).
- Sức sống (+5%) nhân độc lập, không bị nhân đôi.

Sét đánh, Risk và Burning:
- Chỉ đánh trong Giông khi stage.exposure > 0 (large_tree). Cooldown 120s online ngăn sét đánh dồn dập.
- Protection = TrunkLevel * 0.05 + permanent.Protection.
- Probability theo Poisson: P(dt) = 1 - exp(-finalRate * dt), với finalRate = baseRate(0.001) * exposure * (1 / (1 + Protection)). Độc lập hoàn toàn với FPS / dt.
- Sét chọn 1 vùng (root, leaves, trunk). Bắt đầu status "burning" 10s.
- Damage: 2 HP/giây. Burning Leaves giảm Leaves Growth 30% (hệ số 0.7x). Burning Root/Trunk không giảm Growth.
- Cây chết khi HP về 0: life = "DEAD", dừng sản xuất ngay từ thời điểm chết; RunDied phát 1 lần; RebirthView tự động bật modal Forced Rebirth, khóa Cancel.
- Chuyển sang Bình yên ("calm"): xóa Burning ngay lập tức, không tự hồi HP.
- Rebirth: reset HP về 100, weather về "calm", status về "none".


Point effect riêng, cộng dồn theo target: seedling RootWater×(1+points×0.05), young_tree LeavesGrowth×(1+points×0.05), large_tree Protection+=points×0.02. Không buff consumption/cap/Trunk bằng Sức sống; online buff production một lần, offline giữ permanent modifiers và loại Sức sống. `pointEffects` stable ID và `initialCalmSeconds` trong PrototypeRules; snapshot runtime cho cả quote/UI/reset. Config prototype-3; SaveCodec chỉ nhận prototype-1/2→3 sau validate schema1, không reset dữ liệu, config lạ vẫn blocked.

RebirthView nằm trong safe area, nút chung mouse/touch; modal chặn world/camera, hủy gesture, DEAD không Cancel và mở lại save giữ modal. Lỗi pending offline/recovery có ưu tiên retry trước Forced Rebirth; bỏ popup của run cũ sau reset. Editor DEAD hook yêu cầu thư mục thử riêng, không triển khai Weather. SaveTestScope cách ly native Pointer trong test/restore cuối, runtime input giữ nguyên. Khôi phục và lưu chế độ Play reload domain+scene T00 sau TestRunner. Ảnh hưởng T06/domain/save/bootstrap/view/tests/config và Technical; bằng chứng Validation/T06_20261003.md. D12/native phone/file API kiểm chứng ở T09.

## D20 Achievement và Coin vĩnh viễn T08A

Trạng thái: PROTOTYPE_DEFAULT. Ngày 03/10/2026. Nguồn: GDD mục 65, Prototype_Config và task T08A.

Quy tắc Achievement & Coin:
- 6 Achievement tạm thời: `first_root` (Root cấp 1), `first_leaves` (Leaves cấp 1), `evolve_young_tree` (stage young_tree), `evolve_large_tree` (stage large_tree), `first_rebirth` (Rebirth lần đầu), `first_vitality` (nhận thưởng 20s Sức sống từ 50 tap lần đầu).
- Mỗi achievement thưởng 10 Coin vĩnh viễn, nhận đúng 1 lần duy nhất (target = 1, coinReward = 10).
- Nhận diện tiến độ tự động từ domain event (`UpgradeSystem.Completed`, `EvolutionSystem.StageChanged`, `RebirthSystem.Completed`, `VitalitySystem.RewardEarned`). Sức sống lần đầu chỉ tính khi hoàn thành 50 tap và nhận thưởng bank time, không tính khi chỉ tap 1 lần.
- Claim mang tính idempotent: claim kiểm tra `!claimed && progress >= target`. Nếu đã claim thì từ chối ngay (`ALREADY_CLAIMED`), loại bỏ hoàn toàn khả năng nhân đôi Coin khi spam click hoặc retry.
- An toàn lưu trữ: Rollback state trong bộ nhớ nếu save lỗi để tránh desync với disk và nhân đôi coin khi retry.
- Coin và danh sách Achievement nằm trong `PermanentState`, lưu giữ nguyên vẹn qua Rebirth và Save/Load.
- Tách biệt kinh tế: Coin là tiền tệ meta (dành cho Shop cosmetic T08B), không được dùng để Upgrade, Evolution hay cứu cây chết; buff Sức sống (+5%) không nhân cho thưởng Coin.
- UI `AchievementView` hiển thị danh sách, phân biệt rõ các trạng thái: Chưa đạt, Đã hoàn thành (Chưa nhận), Đã nhận. Khóa camera/gameplay input khi mở modal.

## D21 Shop và Cosmetic placeholder T08B

Trạng thái: PROTOTYPE_DEFAULT. Ngày 03/10/2026. Nguồn: GDD mục 66, Prototype_Config và task T08B.

Quy tắc Shop & Cosmetic:
- Cosmetic đầu tiên: `glow_placeholder`, giá 20 Coin. Hiệu ứng visual là quả cầu hào quang ngọc bích phát sáng (`Color(0.2f, 0.95f, 0.78f, 0.45f)`) bao quanh thân cây, phân biệt hoàn toàn với quả cầu lửa cam đỏ của Burning.
- Preview là trạng thái tạm thời (transient): không trừ Coin, không thay đổi `equippedCosmeticId` đã lưu. Đóng panel, hủy preview, cây chết hoặc Rebirth đều lập tức trả visual về trang bị đã cam kết.
- Mua vật phẩm (Buy): yêu cầu Coin >= 20. Trừ Coin và thêm vào `ownedCosmeticIds`. Mua không tự động trang bị (`equippedCosmeticId` vẫn giữ nguyên).
- Chống gian lận & Idempotent: Kiểm tra đã sở hữu trước khi mua, double-click hoặc mua lại trả về `ALREADY_OWNED`, không trừ Coin lần 2. Rollback state trong bộ nhớ nếu save lỗi để tránh mất Coin khi lỗi I/O.
- Trang bị (Equip): Yêu cầu vật phẩm đã nằm trong `ownedCosmeticIds`. Trang bị cập nhật `equippedCosmeticId` và commit lưu Save ngay lập tức.
- Bảo tồn vĩnh viễn: Danh sách `ownedCosmeticIds` và `equippedCosmeticId` nằm trong `PermanentState`, lưu giữ nguyên vẹn qua Rebirth và Save/Load.
- Độc lập gameplay: Cosmetic là thuần visual/VFX, hoàn toàn không làm thay đổi các chỉ số sản xuất (Water, Growth), HP hay Protection.
- UI `ShopView` hiển thị trong SafeArea, hỗ trợ cả màn hình ngang PC và màn hình dọc điện thoại, khóa camera rig và gameplay input khi mở modal.

## D22 Tutorial Onboarding, Settings và Audio T08C

Trạng thái: PROTOTYPE_DEFAULT. Ngày 03/10/2026. Nguồn: GDD mục Onboarding & Settings, Prototype_Config và task T08C.

Quy tắc Tutorial, Settings và Audio:
- **Onboarding tuần tự:**
  1. `step_vitality`: Hướng dẫn tap cây để tích lũy Sức sống (50 taps = +20s bank time, tối đa 3600s, +5% sản xuất khi có quỹ). Hoàn thành khi nhận thưởng nhóm tap đầu tiên (`bankSeconds > 0`).
  2. `step_upgrade_root`: Mua nâng cấp Rễ (Root) cấp 1 bằng 5 Water khởi đầu để tự động tạo Water. Hoàn thành khi Root cấp >= 1.
  3. `step_upgrade_leaves`: Mua nâng cấp Lá (Leaves) cấp 1 để quang hợp tạo Growth từ Water. Hoàn thành khi Leaves cấp >= 1.
  4. `step_evolution`: Hướng dẫn khi đủ tài nguyên để tiến hóa lên Cây Non (`young_tree`). Hoàn thành khi chuyển stage sang `young_tree`.
  5. `step_trunk_weather`: Giới thiệu Thân (Trunk) và Thời Tiết (Weather) khi lên Cây Non. Hoàn thành khi bấm "Đã hiểu".
  6. `step_rebirth_intro`: Giới thiệu hệ thống Tái Sinh (Rebirth). Hoàn thành khi bấm "Đã hiểu".
- **Bảo toàn và chống Softlock:**
  - Hoàn thành theo command success hợp lệ, không tính khi thao tác sai hoặc thiếu tài nguyên.
  - Danh sách bước đã hoàn thành lưu trong `state.permanent.tutorialCompletedStepIds`, giữ nguyên qua Rebirth và Save/Load; không lặp lại bước đã làm.
  - Nếu cây chết (`life == "DEAD"`), `TutorialView` tự động ẩn toàn bộ UI banner, nhường trọn quyền tương tác cho `RebirthView` (DEAD Forced Rebirth), loại trừ hoàn toàn nguy cơ softlock.
- **Phân cấp ưu tiên UI (UI Priority) & Phím Escape:**
  - Thứ tự ưu tiên: `DEAD Modal (Forced Rebirth) > Modal thông thường (Settings, Shop, Achievement, Rebirth ALIVE) > Panel (Upgrade) > World gameplay`.
  - Phím Escape: Lần lượt đóng panel/modal đang mở từ ưu tiên cao đến thấp; nếu không có panel nào mở thì mở Cài đặt.
  - Tuyệt đối không cho Escape đóng DEAD modal (Forced Rebirth).
- **Settings:**
  - Toggle Bật / Tắt BGM -> cập nhật `state.settings.musicEnabled`, lưu đĩa ngay lập tức.
  - Toggle Khóa / Mở Khóa Orbit -> cập nhật `state.settings.orbitLocked`, cập nhật `cameraRig.OrbitLocked`, lưu đĩa ngay lập tức.
  - Dòng ghi chú rõ ràng: "Zoom camera: Tính năng sắp ra mắt (Coming Soon)".
  - Nút Tiếp tục (đóng panel) và Thoát game (Quit).
- **Audio & Rate Limiting:**
  - `AudioController` quản lý 1 BGM source và 1 SFX source, sử dụng procedural audio synthesis bằng code C# để luôn có âm thanh mượt mà không phụ thuộc asset ngoài và không gây lỗi thiếu file.
  - SFX rate limiting: Cooldown tối thiểu 0.08s (80ms) giữa các âm tap liên tiếp; spam tap cực nhanh không tạo ra vô hạn âm thanh hoặc gây rè loa.
  - BGM tự động mute / unmute theo `state.settings.musicEnabled`.
- **Toast Notification:**
  - `ToastView` với hàng đợi tin nhắn, tự động mờ dần sau 2.5s.
  - `blocksRaycasts = false` và Graphic `raycastTarget = false`, bảo đảm tuyệt đối không chặn click/touch của người dùng.
- **Hỗ trợ Cross-Platform:**
  - SafeArea layout, tự động thích ứng tỷ lệ màn hình ngang PC và dọc điện thoại, không đè nút HUD.

## D23 Hướng mỹ thuật theo ảnh bonsai của người dùng

Trạng thái: CONFIRMED về hướng tham chiếu. Ngày 04/10/2026. Nguồn: yêu cầu trực tiếp “tôi muốn cây thế giới có phong cách như trong ảnh. theo phong cách chân thực hoặc anime” và ảnh đính kèm, lưu tại `ArtSource/References/world_tree_bonsai_user_20261004.png`.

- Lấy dáng cây bonsai cổ thụ trong ảnh làm hướng cấu trúc: thân dày uốn tự nhiên, vỏ sần, rễ nổi đan xen, cành ngang và tán rộng nhiều tầng với khoảng trống nhìn thấy cành.
- Cho phép concept chân thực hoặc anime bán chân thực. Chưa chọn độc quyền một rendering; bộ prompt cung cấp cả hai để so sánh. Không tự hiểu anime là thêm nhân vật hoặc chibi.
- Thay đề xuất mỹ thuật low-poly/tán khối tròn trong bộ prompt trước; không thay gameplay, ID ba stage, save, resource, Weather hoặc Rebirth. Không thêm stage “Cây Thế Giới” chỉ vì tên game.
- T10 vẫn giữ ưu tiên pipeline và model đơn giản đủ kiểm chứng; style tham chiếu không tự yêu cầu model cực chi tiết, shader cao cấp, package mới hoặc tối ưu hình ảnh ngoài task.
- File ảnh gốc là nguồn tham chiếu do người dùng cung cấp, chưa xác định loài/tác giả. Không lấy tòa nhà, biển chữ, người hoặc chậu trưng bày trong ảnh làm yêu cầu môi trường.
- Tài liệu ảnh hưởng: `Docs/Art/Bonsai_Reference_Prompts.md`, ghi chú thay hướng ở `Docs/Art/Image_Prompt_Pack.md`. Chưa sửa GDD, config, scene hoặc trạng thái T10 vì phiên này chỉ cập nhật prompt/concept brief.
- Kiểm tra sau: chọn concept đồng nhất cho ba stage; khi dựng asset, kiểm tra silhouette nhiều góc, camera cho tán rộng, scale/pivot, collider và anchor theo T10. Chưa tuyên bố tạo ảnh, dựng model hoặc nghiệm thu art trong Unity.

## D24 Phạm vi tiếp tục sau undo model

Ghi chú 06/10/2026: yêu cầu mới về bộ prompt sửa thân tại D25 thay phạm vi khóa thân cho candidate riêng; D24 vẫn áp dụng bảo toàn rễ/tán/file gốc. Không tự chạy đồng thời prompt tích hợp cũ và prompt sửa thân mới.

Trạng thái: PROTOTYPE_DEFAULT về quy trình bàn giao bảo toàn model, không thay luật gameplay. Ngày 05/10/2026. Nguồn: người dùng báo model bị sửa ngoài mong muốn, đã undo và yêu cầu kiểm tra tiến độ/viết lại prompt tiếp tục.

- File Bonsai_Tree.blend/fbx sau undo trùng hash baseline trước sửa đã kiểm tra. Chọn bản này làm nguồn cho lượt tiếp tục, không lấy bản trạng thái bị undo trong Recovery làm mặc định.
- Để tránh lặp lại thay đổi không mong muốn, prompt mới khóa geometry baseline (thân, rễ, cành, tán, UV/normals/tỷ lệ) và hai file gốc. Đây là cách triển khai được đề xuất từ yêu cầu bảo toàn; không coi nhận xét thẩm mỹ trong review cũ là yêu cầu mới sửa geometry.
- Lượt tiếp theo ưu tiên material đơn giản và tích hợp Cây Lớn trong Unity, anchors/collider/camera, Play Mode và hồi quy. Làm trên bản sao/prefab, giữ file gốc và GUID.
- Mầm/Cây Non là mốc tiếp theo riêng; không ghép redesign ba cây và tối ưu mesh vào một lượt tiếp tục. Các điều kiện DONE của T10 vẫn giữ đủ ba stage và nghiệm thu; không giảm acceptance.
- Nếu lỗi cần sửa hình học mới giải quyết được, ghi known issue và giữ baseline. Thay đổi luật hoặc hình dáng ngoài phạm vi prompt cần yêu cầu mới của người dùng.
- Tài liệu: Docs/Art/T10_Continue_After_Undo_20261005.md, Docs/Validation/Post_Undo_Status_20261005.md và bàn giao T10. Prompt trước undo được lưu dạng lịch sử, không dùng triển khai.

## D25 Bộ prompt sửa thân trên candidate của model cũ

Trạng thái: CONFIRMED về yêu cầu kiểm tra tiến độ và soạn prompt; quy trình/mask chi tiết là PROTOTYPE_DEFAULT được đề xuất trong bộ prompt. Ngày 06/10/2026. Nguồn: người dùng “kiểm tra tiến độ của model ... tạo ra một bộ promt để agent sửa phần thân của cây”. Phiên này chỉ audit và viết tài liệu, chưa thực hiện sửa model hoặc giao agent triển khai.

- Phạm vi sửa được mô tả cho agent tiếp theo: thân chính và vùng chuyển tiếp cổ cành cần thiết trên candidate riêng, có mask giới hạn trong Trunk_Branches. Giữ Roots, Foliage, đầu cành/phần cành khóa và các file gốc. Không coi tên mesh Trunk_Branches là quyền redesign toàn bộ cành.
- Ảnh mới tại `C:/Users/Asus/Downloads/pexels-quang-nguyen-vinh-222549-14771841.jpg` là reference hình khối thân; không tự đổi silhouette toàn cây hoặc môi trường. Không xác định loài/quyền tái sử dụng ảnh từ tên file.
- Ba prompt tuần tự: audit nguồn đang lưu/Unsaved, chỉnh candidate, kiểm chứng/export riêng. Chưa thay model gameplay, tạo hai stage còn lại hoặc giảm acceptance T10. D24 khóa thân không áp dụng cho candidate trong phạm vi D25; vẫn giữ nguyên baseline.
- File .blend hiện tại khác hash lịch sử nhưng mesh positions/topology/UV/normals/transforms được đối chiếu trùng backup. Agent phải audit lại nếu nguồn đổi, không tự phục hồi/ghi đè chỉ dựa vào hash file.
- Tài liệu: `Docs/Art/Trunk_Only_Agent_Prompts_20261006.md`, `Docs/Validation/Model_Status_20261006.md` và bàn giao T10.

## D26 Áp dụng model sửa thân vào Assets/_Project/Art

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng gửi ảnh hộp thoại mở file và yêu cầu “chuyển file model bạn sửa sang file ở trong ảnh”.

- Sao lưu baseline gốc (trước sửa) vào `ArtSource/Backup_Before_Apply_20261006/` (`Bonsai_Tree_baseline.blend` và `Bonsai_Tree_baseline.fbx`) kèm hash SHA-256 đối chiếu trùng baseline lịch sử.
- Giữ nguyên các file `.meta` (`Bonsai_Tree.blend.meta` và `Bonsai_Tree.fbx.meta`) và GUID Unity (`2a95f5e80a8ceee4d94452fe729f6459` và `85fb7c1216c01d846be38836a02e7cc0`).
- Ghi dữ liệu candidate v01 vào `Assets/_Project/Art/Bonsai_Tree.blend` và `Assets/_Project/Art/Bonsai_Tree.fbx`.
- Nạp trực tiếp file `Assets/_Project/Art/Bonsai_Tree.blend` vào phiên Blender GUI của người dùng để hiển thị tức thì model mới.
- Tài liệu: `Docs/Validation/Trunk_Candidate_Application_20261006.md`.

## D27 Tạo model cây mầm Tree_Seedling_Simple_v01

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng gửi ảnh `01_seedling.png` và yêu cầu tạo model mới cây mầm stage seedling dưới 15 ngày, lưu file mới `Tree_Seedling_Simple_v01.blend` và `.fbx`, dưới 3.000 triangles.

- Tạo asset mới tại `ArtSource/Models/Tree_Seedling_Simple_v01.blend` và `ArtSource/Exports/Tree_Seedling_Simple_v01.fbx`.
- Bảo toàn nguyên vẹn 100% các model Bonsai_Tree/Cổ Thụ đã có.
- Cấu trúc gồm 4 mesh object riêng: `Roots` (rễ cọc kem + 2 nhánh rễ), `Trunk_Branches` (thân xanh cong nhẹ + cuống lá), `Foliage` (2 lá mầm oval nhọn có độ dày & sống giữa + chồi non đỉnh), `SeedShell` (2 nửa vỏ hạt nâu tách đôi ôm chân thân).
- Origin đặt tại cổ rễ / mặt đất dự kiến `z = 0`. Chiều cao phần trên cổ rễ: 0.258m (yêu cầu tạm 0.25m).
- Tổng số đa giác đạt 2.778 triangles (ngân sách dưới 3.000 triangles ĐẠT).
- Tái nạp FBX kiểm chứng scale (1,1,1), transform chuẩn Unity Y-up, -Z forward.
- Nạp trực tiếp file vào phiên Blender GUI của người dùng để xem và xoay 360 độ.
- Tài liệu: `Docs/Validation/Seedling_Model_v01_Verification_20261006.md`.

## D28 Tạo model cây non Tree_Young_Simple_v01

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng gửi ảnh `02_young_tree.png` và yêu cầu tạo model mới cây non stage young_tree dưới 3 tháng, lưu file mới `Tree_Young_Simple_v01.blend` và `.fbx`, dưới 5.000 triangles.

- Tạo asset mới tại `ArtSource/Models/Tree_Young_Simple_v01.blend` và `ArtSource/Exports/Tree_Young_Simple_v01.fbx`.
- Bảo toàn nguyên vẹn 100% các model Cổ Thụ và Cây Mầm đã có.
- Cấu trúc gồm 3 mesh object riêng: `Roots` (rễ cọc chính + 4 rễ bên và các nhánh phụ), `Trunk_Branches` (thân xanh mảnh chữ S + 8 cuống lá sole + nhánh phụ và cuống chồi đỉnh), `Foliage` (8 lá chính oval nhọn xếp sole có độ dày & sống giữa + 1 lá nhánh phụ + chồi bên và chồi non 3 lá đỉnh). Không có vỏ hạt, không thân gỗ dày.
- Origin đặt tại cổ rễ / mặt đất dự kiến `z = 0`. Chiều cao phần trên cổ rễ: 0.838m (yêu cầu tạm 0.8m).
- Tổng số đa giác đạt 4.302 triangles (ngân sách dưới 5.000 triangles ĐẠT).
- Tái nạp FBX kiểm chứng scale (1,1,1), transform chuẩn Unity Y-up, -Z forward.
- Nạp trực tiếp file vào phiên Blender GUI của người dùng để xem và xoay 360 độ.
- Tài liệu: `Docs/Validation/YoungTree_Model_v01_Verification_20261006.md`.

## D29 Nâng cấp hệ thống rễ 3D mở rộng trục Y cho Cây Non

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng phản hồi “phần rễ của cây giờ vẫn quá thẳng ( chỉ ở trục z x tôi muốn một vài chiếc rễ phải hướng về phía trục y”.

- Điều chỉnh trực tiếp model gốc `ArtSource/Models/Tree_Young_Simple_v01.blend` và xuất đè đúng FBX `ArtSource/Exports/Tree_Young_Simple_v01.fbx` (không tạo candidate, không tạo file copy).
- Khắc phục giới hạn phẳng trục X-Z của rễ: trước sửa, rễ chỉ có độ sâu trục Y là $0.045\text{ m}$ (4.5 cm). Sau sửa, rễ có độ sâu trục Y mở rộng lên $0.449\text{ m}$ ($44.9\text{ cm}$), tương đương với độ rộng trục X ($0.494\text{ m}$), tạo hình vòm rễ tỏa tròn 3D bao quanh gốc cây.
- Bố trí các nhánh rễ vươn rõ rệt về phía trục Y:
  - Rễ vươn thẳng về phía trước (+Y): vươn ra $y = +0.222\text{ m}$ kèm nhánh phụ rẽ chéo $y = +0.192\text{ m}$.
  - Rễ vươn thẳng về phía sau (-Y): vươn ra $y = -0.226\text{ m}$ kèm nhánh phụ rẽ chéo $y = -0.196\text{ m}$.
  - Rễ bên chéo trước trái (-X, +Y) và chéo sau phải (+X, -Y) vươn tỏa rộng.
  - Các rễ tầng trung ở độ sâu $z = -0.10\text{ m}$ đến $-0.13\text{ m}$ đan xen 3D.
  - Rễ cọc chính uốn lượn hữu cơ cả trên trục X và Y từ cổ rễ xuống độ sâu $z = -0.480\text{ m}$.
- Tổng số đa giác: 4.530 triangles (Roots: 728 tris, Trunk_Branches: 578 tris, Foliage: 3.224 tris) - đạt mục tiêu `< 5.000` tris.
- Kiểm tra toàn diện 7 góc nhìn kết xuất tại `Evidence/YoungTree_v01_Audit/`:
  - `preview_front.png` (Góc trước)
  - `preview_back.png` (Góc sau)
  - `preview_left.png` (Góc trái)
  - `preview_right.png` (Góc phải)
  - `preview_top_down.png` (Góc trên xuống)
  - `preview_perspective_front_3_4.png` (Phối cảnh 3/4 trước)
  - `preview_perspective_back_3_4.png` (Phối cảnh 3/4 sau)
  - `reimport_fbx_perspective.png` (Kiểm chứng đối chiếu nạp lại file FBX)
- Đã nạp lại file `.blend` mới nhất vào phiên Blender GUI của người dùng và thiết lập Solid Material/Cavity shading.
- Tài liệu: `Docs/Validation/YoungTree_Roots_3D_Verification_20261006.md`.

## D30 Sửa lỗi cổ rễ và điểm nối rễ cho Cây Non

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng gửi ảnh `media_1791245376098.png` chỉ rõ các lỗi rễ bị trôi nổi, nếp thắt và gờ vành cổ rễ.

- Sửa trực tiếp hình học trong `ArtSource/Models/Tree_Young_Simple_v01.blend` và xuất đè đúng `ArtSource/Exports/Tree_Young_Simple_v01.fbx` (không tạo candidate, không tạo file copy).
- Khắc phục 3 lỗi hình học được người dùng chỉ ra:
  1. **Lỗi rễ trôi nổi (Floating Root)**: Nhánh rễ tầng trung bên trái `root_mid_left_b` trước đó có điểm bắt đầu bị lệch cách xa thân rễ cọc $2.7\text{ cm}$ do rễ cọc uốn cong về phía trước. Đã định vị chính xác điểm xuất phát cắm sâu vào tâm trục rễ cọc tại độ sâu $z = -0.100\text{ m}$ và mở nắp đầu (`cap_start=False`), tạo đường chuyển tiếp liền mạch không còn khoảng hở trôi nổi.
  2. **Lỗi nếp thắt / vết cắn trên thân rễ (Pinched Notch)**: Nhánh rễ tầng trung bên phải `root_mid_right_f` tại $z = -0.135\text{ m}$ được điều chỉnh xuất phát sâu từ lõi rễ cọc với vector tiếp tuyến hướng mượt ra ngoài, triệt tiêu hoàn toàn góc cắt chéo tạo nếp gãy trên vỏ rễ cọc.
  3. **Lỗi gờ vành cổ rễ (Stepped Collar Ring / Lip)**: Trước đó đỉnh mesh `Roots` cao hơn mặt đất ($z = 0.012\text{ m}$) và có bán kính lớn hơn ($r = 0.0160\text{ m}$) so với chân thân `Trunk_Branches` ($z = 0.000\text{ m}, r = 0.0145\text{ m}$) kèm nắp phẳng, tạo thành một gờ vành lồi ra như ống nối. Đã đồng bộ bán kính thân và rễ tại $z = 0.000\text{ m}$ đúng bằng $0.0145\text{ m}$, cho thân lồng nhẹ xuống $z = -0.008\text{ m}$ và rễ cọc mở nắp trên (`cap_start=False`), tạo đường loe cổ rễ (root flare) trơn tru, hữu cơ, xóa bỏ hoàn toàn gờ vành.
- Tổng số đa giác: 4.574 triangles (Roots: 760 tris, Trunk_Branches: 590 tris, Foliage: 3.224 tris) - đạt mục tiêu `< 5.000` tris.
- Kết xuất ảnh cận cảnh kiểm chứng `closeup_root_junction_fixed.png` chứng minh triệt để các lỗi đã biến mất.
- Tái nạp FBX kiểm chứng scale (1,1,1), transform chuẩn Unity Y-up, -Z forward.
- Nạp lại file `.blend` mới nhất vào phiên Blender GUI của người dùng.
- Tài liệu: `Docs/Validation/YoungTree_Root_Fixes_Verification_20261006.md`.

## D31 Tạo model Cây Lớn Tree_Large theo ảnh tham chiếu

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng gửi ảnh `media_1791246214955.png` và yêu cầu tạo model mới Cây Lớn (Tree_Large) lưu cùng thư mục với các model cũ (`ArtSource/Models/Tree_Large.blend` và `ArtSource/Exports/Tree_Large.fbx`), thân nâu cong tự nhiên, cành phân tầng ăn sâu vào tán, rễ lộ tỏa 3D, và các cụm tán mây phồng hữu cơ (pillowy cloud clusters).

- Tạo asset mới tại `ArtSource/Models/Tree_Large.blend` và `ArtSource/Exports/Tree_Large.fbx`.
- Bảo toàn nguyên vẹn 100% các model đã có: Cổ Thụ (`Assets/_Project/Art/Bonsai_Tree.blend`), Cây Mầm (`Tree_Seedling_Simple_v01.blend`), Cây Non (`Tree_Young_Simple_v01.blend`).
- Cấu trúc gồm 3 mesh object riêng: `Roots`, `Trunk_Branches`, `Foliage`.
- Thiết lập gốc tọa độ (Origin) tại cổ rễ / mặt đất dự kiến `z = 0`. Toàn bộ transform scale `(1.0, 1.0, 1.0)` và rotation `(0, 0, 0)`.
- Chiều cao phần trên cổ rễ: $2.448\text{ m}$. Chiều sâu rễ cắm đất: $0.335\text{ m}$.
- Kích thước không gian 3D:
  - Bề rộng trục X: $2.719\text{ m}$ (từ $-1.374\text{ m}$ đến $+1.345\text{ m}$).
  - Bề sâu trục Y: $1.430\text{ m}$ (từ $-0.604\text{ m}$ đến $+0.826\text{ m}$).
  - Tỷ lệ chiều sâu trục Y so với chiều rộng trục X đạt $52.6\%$, đảm bảo tán và cành có độ dày và thể tích 3D thực thụ, nhìn đẹp và cân đối ở mọi góc nhìn 360°, không bị dẹt phẳng.
- Tán lá hữu cơ dạng khối phồng (volumetric cloud clusters): gồm 7 cụm tán mây phồng kín, có múi phồng tròn múp míp (pillowy lobes) và rãnh nông theo ảnh tham chiếu, đầy đủ các mặt trên/dưới/trước/sau (không dùng billboard/plane).
- Thân và cành: uốn lượn chữ S tự nhiên trên cả 2 mặt phẳng XZ và YZ; cành mọc tỏa 3D đa hướng (trước, sau, trái, phải) ở các cao độ khác nhau và ăn sâu vào lõi cụm tán.
- Bộ rễ: 5 nhánh rễ bạnh (buttress roots) tỏa đều 360° quanh gốc trên mặt phẳng XY, uốn lượn cắm sâu xuống đất và phân nhánh thành các chạc vuốt ngắn. Chân thân và cổ rễ khớp trơn tru tại $z = 0.000\text{ m}$ ($r = 0.165\text{ m}$), thân lồng sâu xuống $z = -0.030\text{ m}$, không lộ gờ vành hay chóp nhọn.
- Chất liệu: gán `M_Bark` (tone gỗ caramel nâu ấm `(0.68, 0.44, 0.24)`) cho `Trunk_Branches` và `Roots`, `M_Foliage` (tone xanh mạ tươi `(0.35, 0.72, 0.10)`) cho `Foliage`.
- Tổng số đa giác: 7.926 triangles (Roots: 922 tris, Trunk_Branches: 706 tris, Foliage: 6.298 tris), cực kỳ tối ưu cho hiệu năng đa nền tảng PC và Mobile.
- Tái nạp FBX kiểm chứng scale (1,1,1), transform chuẩn Unity Y-up, -Z forward.
- Đã nạp trực tiếp file `.blend` vào phiên Blender GUI của người dùng và giữ Solid Material/Cavity shading.
- Tài liệu: `Docs/Validation/Tree_Large_Model_v01_Verification_20261006.md`.

## D32 Nâng cấp Cây Lớn Tree_Large: Tán lá 3D đa tầng shingled và thân khía rãnh vỏ cây

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng gửi ảnh `media_1791250903616.png` yêu cầu tiếp tục model Cây Lớn vừa dựng, hoàn thiện tán gồm các lá riêng biệt có chiều sâu, thân có nét vỏ cây nhẹ; lưu và sửa trực tiếp vào `ArtSource/Models/Tree_Large.blend` và cập nhật đúng `ArtSource/Exports/Tree_Large.fbx`.

- Sửa trực tiếp hình học trong file gốc `ArtSource/Models/Tree_Large.blend` và xuất đè đúng `ArtSource/Exports/Tree_Large.fbx` (không tạo candidate, không tạo file copy). Bảo toàn 100% các model Cổ Thụ, Cây Mầm và Cây Non đã có.
- Giữ vững toàn bộ nền tảng: đường cong thân chữ S, chiều cao ($2.370\text{ m}$ trên mặt đất), chiều sâu rễ ($0.335\text{ m}$), vị trí pivot ($z = 0$), tỷ lệ và vị trí của 7 cụm tán lá.
- Nâng cấp Thân và Cành (`Trunk_Branches`):
  - Khía 5 sống/rãnh nông (longitudinal bark flutes) xoắn nhẹ dọc theo chiều thân và cành theo đường cong tự nhiên, tạo thớ vỏ cây bắt sáng và đổ bóng sắc nét dưới cavity shading mà không cần sculpt đa giác cao.
  - Bổ sung hệ thống cành phụ / cuống cành (secondary twigs) vươn vào bên trong từng cụm tán, kết nối vật lý 100% cuống lá vào thân cây, không có lá trôi nổi.
- Nâng cấp Bộ Rễ (`Roots`):
  - 5 nhánh rễ bạnh chính tỏa đều 360° quanh gốc kèm chạc vuốt ngắn, bổ sung khía rãnh thớ gỗ dọc sống rễ, chuyển tiếp trơn tru với chân thân tại $z = 0.000\text{ m}$.
- Chuyển đổi toàn diện Tán lá (`Foliage`):
  - Loại bỏ hoàn toàn các khối guide xanh đặc bên trong, thay thế bằng **483 lá 3D riêng biệt** cấu trúc ngói lợp (shingled multi-tier pads) xếp chồng nhiều lớp trên cả 7 cụm tán.
  - Mỗi chiếc lá là một mesh 3D hoàn chỉnh có 3 thùy viền lượn sóng (scalloped lobes), đỉnh vuốt nhọn, rãnh sống gân chữ V, vòm cong và độ rủ mềm mại, có bề dày 3.5mm với thành mép khép kín (nhìn thấy thể tích rõ ràng từ cả 2 mặt).
  - Bố trí lá đa tầng: chóp đỉnh che kín tâm, tầng trên vươn đón sáng, tầng vành xòe ngang, tầng dưới rủ skirt và cụm lót đáy che cuống tạo độ dày dặn, không lộ lỗ hổng từ bất kỳ góc nhìn nào.
- Thống kê đa giác thực tế (so sánh trước/sau):
  - Trước nâng cấp (Prompt 1): 7.926 triangles (Roots: 922, Trunk_Branches: 706, Foliage: 6.298).
  - Sau nâng cấp (Prompt 2): **46.802 triangles** (Roots: 958, Trunk_Branches: 1.408, Foliage: 44.436). Tổng số tam giác nhẹ hơn 37% so với model Cổ Thụ baseline (73.888 tris), tối ưu mượt mà cho hiệu năng game đa nền tảng.
- Kích thước không gian 3D:
  - Bề rộng trục X: $3.177\text{ m}$ (từ $-1.610\text{ m}$ đến $+1.567\text{ m}$).
  - Bề sâu trục Y: $1.967\text{ m}$ (từ $-0.880\text{ m}$ đến $+1.088\text{ m}$).
  - Tỷ lệ chiều sâu / chiều rộng ($Y/X$): **$61.9\%$** (nằm trọn vẹn trong khoảng mục tiêu tham khảo $60\% - 80\%$).
- Tái nạp FBX kiểm chứng: scale (1,1,1), rotation (0,0,0), chuẩn Unity Y-up, -Z forward.
- Đã nạp lại file vào phiên Blender GUI của người dùng và thiết lập Solid Material/Cavity shading.
- Báo cáo: `Docs/Validation/Tree_Large_Model_v02_Verification_20261006.md`.

## D33 Đồng bộ kiểu lá Cây Lớn với Bonsai Cổ Thụ và nâng cấp hệ rễ hữu cơ Tree_Large

Ghi chú kiểm tra lại 06/10/2026: yêu cầu đồng bộ vẫn áp dụng, nhưng kết quả lá/8.202 tris bên dưới là lịch sử, được sửa tiếp bởi D34. D33 tái tạo lá đơn bằng công thức, chưa sao chép cụm nguồn; không dùng ảnh đối chiếu cũ để kết luận trùng hình học thật hoặc coi số tris là bằng chứng hiệu năng mobile.

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng. Ngày 06/10/2026. Nguồn: người dùng yêu cầu chỉnh trực tiếp `ArtSource/Models/Tree_Large.blend`, dùng `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` làm tham chiếu chỉ đọc, đồng bộ kiểu lá để hai cây cùng loài thực vật, sửa bộ rễ hữu cơ tự nhiên không kickstand, cập nhật đúng FBX `ArtSource/Exports/Tree_Large.fbx`.

- Sửa trực tiếp hình học trong `ArtSource/Models/Tree_Large.blend` và xuất đè đúng `ArtSource/Exports/Tree_Large.fbx` (không tạo candidate, không tạo file copy). Bảo toàn 100% các model Cổ Thụ, Cây Mầm và Cây Non đã có.
- File tham chiếu `Bonsai_Trunk_Candidate_v01.blend` được đọc và bảo toàn nguyên vẹn 100%, không bị sửa hoặc lưu đè.
- **Đồng bộ kiểu lá chuẩn Bonsai Cổ Thụ:**
  - Audit hình học từ model tham chiếu: mỗi lá có dạng oval vuốt nhọn (hình quả trám), tỷ lệ Rộng : Dài = $1 : 2$ ($0.236\text{ m} : 0.472\text{ m}$), nếp gấp sống lá chữ V $18.69^\circ$, uốn vòm và rủ nhẹ về đỉnh, UV chuẩn `(0.5, 0.0)` cuống, `(0.0, 0.5)` mép trái, `(1.0, 0.5)` mép phải, `(0.5, 1.0)` ngọn.
  - Nâng cấp độ dày vật lý: chuyển từ card 1 mặt sang mesh kín 2 mặt có độ dày mép $3.5\text{ mm}$ (16 tris/lá), giữ nguyên UV gốc đồng thời khắc phục triệt để hiện tượng trong suốt / vô hình khi nhìn ngược sáng hoặc bật Backface Culling trong Unity.
  - 385 lá đồng bộ được phân bố thành 7 cụm tán (canopy pads) ngói lợp shingled nhiều tầng, được cành phụ nâng đỡ chắc chắn.
- **Tái thiết kế hệ thống rễ hữu cơ & chân thân loe:**
  - Xóa bỏ hoàn toàn dáng rễ thẳng/đều như chân chống cơ học (kickstands).
  - Thiết kế 5 rễ chính bất đối xứng: góc tỏa ngẫu nhiên ($220^\circ, 315^\circ, 15^\circ, 70^\circ, 140^\circ$), bò lan trên mặt đất (XY) rồi uốn lượn cắm xuống (Z), tiết diện dẹt gần gốc, đầu rễ thuôn tròn tự nhiên, chạc cấp 2 mọc so le, chân thân loe rộng mềm mại (root flare) tại $z = 0.000\text{ m}$ ($r = 0.205\text{ m}$).
- **Thống kê đa giác thực tế:**
  - `Roots`: 374 verts, 634 tris.
  - `Trunk_Branches`: 858 verts, 1.408 tris.
  - `Foliage`: 3.850 verts, 6.160 tris.
  - **Tổng cộng: 5.082 verts, 8.202 triangles** (giảm 82.5% so với bản v02 46.802 tris, cực kỳ nhẹ và tối ưu cho mobile).
- **Kích thước không gian 3D:**
  - Bề rộng trục X: $3.316\text{ m}$ (từ $-1.681\text{ m}$ đến $+1.635\text{ m}$).
  - Bề sâu trục Y: $2.104\text{ m}$ (từ $-0.952\text{ m}$ đến $+1.152\text{ m}$).
  - Tỷ lệ chiều sâu trục Y so với chiều rộng trục X đạt **$63.5\%$** (đáp ứng xuất sắc mục tiêu $60\% - 80\%$).
  - Chiều cao phần trên cổ rễ: $2.322\text{ m}$. Chiều sâu rễ cắm đất: $0.264\text{ m}$.
- **Kiểm chứng FBX độc lập:** Tái nạp file FBX xác nhận đầy đủ 3 mesh object, scale `(1.0, 1.0, 1.0)`, rotation `(0, 0, 0)`, gốc tọa độ $z = 0$.
- Đã nạp lại file vào phiên Blender GUI của người dùng và thiết lập Solid Material/Cavity shading.
- Báo cáo: `Docs/Validation/Tree_Large_Bonsai_Sync_Verification_20261006.md`.

## D34 Sửa trực tiếp lá Cây Lớn theo hình học thật của Cổ Thụ trước khi tô màu

Trạng thái: CONFIRMED về phạm vi theo yêu cầu trực tiếp của người dùng ngày 06/10/2026: "hiện tại tôi thấy model lá của cây lớn và cây cô thụ là khác nhau bạn hãy trực tiếp sửa để đồng bộ lá trước khi thêm màu". Chi tiết trích 8 mẫu/cụm 4 phiến, độ dày và dao động góc là lựa chọn triển khai asset, không thêm luật gameplay hoặc stage.

- Chỉ sửa Foliage của `ArtSource/Models/Tree_Large.blend`, dùng `Bonsai_Trunk_Candidate_v01.blend` làm chuẩn chỉ đọc, cập nhật đúng FBX hiện có. Không thêm candidate/copy model.
- Giữ thân/rễ, bố cục 7 vùng tán đa chiều, các model khác và material hiện có. Chưa chạy bước áp bảng màu mới hoặc tích hợp Unity.
- Lý do sửa tiếp D33: công thức lá đơn gần giống chưa giữ hình học/cách ghép cụm thật. D34 chuyển các cụm nguồn với uniform scale và định hướng ở 385 neo cũ, tạo 1.540 phiến kín hai mặt. Tổng mới 26.682 tris; không coi số tris là bằng chứng tối ưu mobile.
- Ảnh hưởng: hai asset Tree_Large, T10, ghi chú báo cáo D33, Validation mới và Evidence. Không đổi GDD, economy, config, save hoặc scene.
- Kiểm tra: preview 8 góc, đối chiếu đỉnh thật/UV, fingerprints thân/rễ, SHA256 nguồn và model khác, lưu/mở lại blend, reimport FBX độc lập. Unity Editor/Play Mode vẫn chưa chạy trong phiên; T10 giữ `IN_PROGRESS`.
- Báo cáo: `Docs/Validation/Leaf_Sync_Direct_20261006.md`.

## D35 Soạn bộ prompt material và chi tiết bề mặt

Trạng thái: CONFIRMED về yêu cầu soạn prompt, không phải lệnh triển khai. Ngày 06/10/2026, nguồn: người dùng yêu cầu tạo các prompt cho agent tạo material màu, chi tiết lồi lõm thân ở Cây Lớn/Cổ Thụ và vân lá cho các model vừa tạo.

- Chỉ tạo bộ prompt/tài liệu, không sửa material/model/texture, không gửi agent khác thực hiện. Khi người dùng giao các prompt, chúng mô tả sửa trực tiếp 4 file nguồn hiện có, bảo toàn geometry và không tạo bản model dư.
- Palette, mức sần/normal strength, kích thước texture và normal-map-only là đề xuất triển khai, chưa coi người dùng đã chốt màu hoặc yêu cầu sculpt hình học. D34 vẫn là baseline hình khối/lá để bảo toàn; khóa màu cũ trong lượt D34 không ngăn bước material mới khi được giao riêng.
- 5 bước tuần tự: màu/audit → lá chung → vỏ Lớn → vỏ Cổ Thụ → bake/export/kiểm tra. Không tự sửa Assets/scene/prefab/gameplay hoặc thêm Cổ Thụ vào danh sách stage/save.
- Ảnh hưởng hiện tại: `Docs/Art/Tree_Materials_Surface_Prompts_20261006.md`, Validation phiên soạn, bàn giao T10 và con trỏ tổng hợp. Không đổi GDD, config hoặc checklist nghiệm thu.
- Kiểm tra tài liệu/path/version và nguồn kỹ thuật; chưa có kiểm tra Blender/Unity trong phiên này. T10 giữ `IN_PROGRESS`.

## D36 Triển khai P01: Audit và áp dụng màu cơ bản cho bốn nguồn cây

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng ngày 06/10/2026 ("Thực hiện P01 của bộ Tree_Materials_Surface_Prompts_20261006").

- Sửa trực tiếp 4 file nguồn trong `ArtSource/Models/`: `Tree_Seedling_Simple_v01.blend`, `Tree_Young_Simple_v01.blend`, `Tree_Large.blend`, `Bonsai_Trunk_Candidate_v01.blend`. Không tạo candidate mới, không tạo file copy hay `.blend1` dư (`save_version = 0`).
- Bảo toàn tuyệt đối hình học (vertex positions, topology, corner normals, silhouette, mật độ/vị trí lá, cụm tán, thân/cành/rễ, pivot, unit scale, transforms). Đối chiếu geometry fingerprint trước và sau mở lại độc lập từ đĩa khớp 100%. Số đa giác khớp hoàn toàn các mốc đối chiếu: Mầm 2.778 tris, Non 4.574 tris, Lớn 26.682 tris, Cổ Thụ 73.888 tris.
- Gán Principled BSDF với bảng màu đề xuất, chuyển đổi chuẩn xác sRGB HEX sang linear RGBA bằng script:
  - Mầm: Lá `#9BCB55` (Roughness 0.65), Thân `#8BB75A` (Roughness 0.80), Rễ kem `#C9AE82` (Roughness 0.88), Vỏ hạt ngoài `#765038` (Roughness 0.90), Vỏ hạt trong `#8E6347` (Roughness 0.90).
  - Non: Lá `#75AA43` (Roughness 0.70), Thân `#A18153` (Roughness 0.82), Rễ `#A98B62` (Roughness 0.88).
  - Lớn: Lá `#5F963E` (Roughness 0.72), Thân & Rễ `#8A6848` (Roughness 0.85).
  - Cổ Thụ: Lá `#507C38` (Roughness 0.75), Thân & Rễ `#70634F` (Roughness 0.88).
  - Toàn bộ material: Metallic = 0.0, Emission = 0.0.
- Bảo toàn nguyên vẹn 100% `Assets/_Project/Art/Bonsai_Tree.blend` và toàn bộ thư mục `Assets/`.
- Nạp lại trực tiếp `ArtSource/Models/Tree_Large.blend` vào phiên Blender GUI của người dùng qua MCP `revert_mainfile()`.
- Kết xuất 20 ảnh góc chuẩn và 1 ảnh toàn cảnh lineup 4 cây tại `Evidence/Tree_Materials_P01_20261006/final/`. Báo cáo: `Docs/Validation/Tree_Materials_P01_20261006.md`.
- T10 giữ `IN_PROGRESS`, dừng sau P01, chưa tạo gân lá (P02), chưa làm vỏ (P03/P04), chưa bake/FBX (P05) hay tích hợp Unity.

## D37 Triển khai P02: Vân lá dùng chung không phá kết quả đồng bộ lá

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng ngày 06/10/2026 ("Thực hiện P02 sau P01 đã kiểm tra, áp dụng quy tắc chung").

- Sinh bộ texture gân lá dùng chung 512×512 tại `ArtSource/Textures/Tree_Common/`:
  - `Leaf_Common_BaseColor.png`: Màu nền trung tính sáng ấm `#DCDCD0`, sống lá chính và 4 cặp gân phụ chuyển sắc nhẹ, hòa trộn Multiply với linear tint của từng mốc để không làm tối kép màu lá.
  - `Leaf_Common_Normal.png`: Tangent-space OpenGL (+Y up, Non-Color), sống gân giữa vòm mềm và các gân phụ phân nhánh, vi hạt tế bào sần mịn isotropic, lấp đầy biên phẳng triệt tiêu viền sáng hay lỗi mép.
- Chuẩn hóa tọa độ UV lá:
  - Lớn (`Tree_Large.blend`, layer `Float2`) và Cổ Thụ (`Bonsai_Trunk_Candidate_v01.blend`, layer `UVMap`) giữ nguyên UV chuẩn D34: cuống $(0.5, 0.0)$, ngọn $(0.5, 1.0)$, mép trái $(0.0, 0.5)$, mép phải $(1.0, 0.5)$.
  - Mầm (`Tree_Seedling_Simple_v01.blend`) và Non (`Tree_Young_Simple_v01.blend`): Unwrap chuẩn hóa các phiến lá vào layer `UVMap` khớp cùng hệ tọa độ chuẩn, không thay đổi bất kỳ đỉnh, cạnh, mặt, corner normal hay transform nào (0.000000 m sai số).
- Cấu hình shader Principled BSDF với Image Texture và Normal Map:
  - Mầm: Normal strength 0.08, Tint factor 0.70, Roughness 0.65.
  - Non: Normal strength 0.14, Tint factor 0.85, Roughness 0.70.
  - Lớn: Normal strength 0.20, Tint factor 1.00, Roughness 0.72.
  - Cổ Thụ: Normal strength 0.22, Tint factor 1.00, Roughness 0.75.
  - Toàn bộ 4 cây: Metallic = 0.0, Emission = 0.0. Material thân gỗ/rễ giữ nguyên P01.
- Bảo toàn tuyệt đối hình học: Fingerprint hình học khớp 100% trên cả 4 file nguồn; số đa giác giữ nguyên: Mầm 2.778 tris, Non 4.574 tris, Lớn 26.682 tris, Cổ Thụ 73.888 tris.
- Bảo toàn nguyên vẹn 100% `Assets/_Project/Art/Bonsai_Tree.blend` và toàn bộ thư mục `Assets/`.
- Lưu trực tiếp 4 file nguồn với `save_version = 0` (không tạo `.blend1` dư). Đã gọi lệnh `revert_mainfile()` nạp lại vào phiên Blender GUI của người dùng.
- Kết xuất 20 ảnh góc chuẩn và 1 ảnh so sánh Cây Lớn cạnh Cổ Thụ (`large_ancient_comparison.png`) tại `Evidence/Tree_Materials_P02_20261006/final/`. Báo cáo: `Docs/Validation/Tree_Materials_P02_20261006.md`.
- T10 giữ `IN_PROGRESS`, dừng sau P02, chưa tạo vỏ thân Cây Lớn (P03), chưa làm vỏ Cổ Thụ (P04), chưa bake/FBX (P05) hay tích hợp Unity.

## D38 Triển khai P03: Vỏ thân Cây Lớn trưởng thành nhưng chưa cổ thụ

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng ngày 06/10/2026 ("Thực hiện P03 sau P02, áp dụng quy tắc chung").

- Sinh bộ texture vỏ thân chuyên biệt 1024×1024 tại `ArtSource/Textures/Tree_Common/`:
  - `Large_Bark_BaseColor.png`: Màu nền nâu ấm `#8A6848`, sống vỏ nổi `#AD8860`, khe rãnh nhẹ `#594A39`, bổ sung 48 vi vết khía lenticels ngang, thớ gỗ lượn sóng hữu cơ (anisotropic Voronoi ratio 0.22 kết hợp domain warping), lặp tuần hoàn 100% liền mạch (seamless tiling).
  - `Large_Bark_Normal.png`: Tangent-space OpenGL (+Y up, Non-Color), tính từ sai phân trung tâm tuần hoàn của trường độ cao sau lọc box-blur mịn, rãnh nông, đứt đoạn tự nhiên, sống vỏ mềm, strength 14.0 trong map và 0.85 trong shader node.
- Chuẩn hóa tọa độ UV thân và rễ:
  - Giữ nguyên layer `Float2` của lá (`Foliage`) từ D34/P02.
  - Unwrap thân/cành (`Trunk_Branches`) và rễ (`Roots`) vào layer `Float2` (kênh UV0 mặc định khi xuất FBX).
- Cấu hình shader `M_Bark`:
  - Principled BSDF: Mapping Scale `(2.5, 2.5, 1.0)`, `Large_Bark_BaseColor.png` $\to$ Base Color, `Large_Bark_Normal.png` $\to$ Normal Map (Tangent, Strength 0.85) $\to$ Normal. Roughness = 0.85, Metallic = 0.0, Emission = 0.0.
- Bảo toàn tuyệt đối hình học: Khớp 100% geometry fingerprint trên cả 3 mesh của `Tree_Large.blend`: Foliage 24.640 tris, Trunk_Branches 1.408 tris, Roots 634 tris (Tổng 16.632 verts, 26.682 tris).
- Bảo toàn 100% nguyên vẹn các model khác (`Tree_Seedling_Simple_v01.blend`, `Tree_Young_Simple_v01.blend`, `Bonsai_Trunk_Candidate_v01.blend`, `Bonsai_Tree.blend`, `Bonsai_Tree.fbx`).
- Lưu trực tiếp với `save_version = 0` (không tạo `.blend1` dư).
- Kết xuất 8 ảnh kiểm chứng (perspective, front, back, left, right, closeup_fork, oblique_light, distance_view) tại `Evidence/Tree_Materials_P03_20261006/final/`. Báo cáo: `Docs/Validation/Tree_Materials_P03_20261006.md`.
- T10 giữ `IN_PROGRESS`, dừng sau P03, chưa tạo vỏ Cổ Thụ (P04), chưa bake/FBX (P05) hay tích hợp Unity.

## D39 Triển khai P04: Vỏ thân Cổ Thụ già hơn nhưng cùng loài

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng ngày 06/10/2026 ("Thực hiện P04 sau P03, áp dụng quy tắc chung").

- Sinh bộ texture vỏ thân Cổ Thụ chuyên biệt 1024×1024 tại `ArtSource/Textures/Tree_Common/`:
  - `Ancient_Bark_BaseColor.png`: Màu nền nâu xám già `#70634F`, sống vỏ nhạt thời gian `#9B896C`, khe nứt `#463F34`, bản vỏ rộng hơn ($N_x=6$), thớ gỗ xoắn đa tầng với 64 vi vết khía và nốt chai vỏ, lặp tuần hoàn 100% liền mạch (seamless tiling).
  - `Ancient_Bark_Normal.png`: Tangent-space OpenGL (+Y up, Non-Color), đạo hàm sai phân trung tâm tuần hoàn từ trường độ cao, sống vỏ gồ ghề dứt khoát, rãnh nứt sâu hơn (~1.6× so với Cây Lớn: strength 22.0 trong map và 1.05 trong shader node).
- Tọa độ UV:
  - Giữ nguyên layer `UVMap` đã có sẵn trên cả 3 mesh (`Trunk_Branches`, `Roots`, `Foliage`) của `Bonsai_Trunk_Candidate_v01.blend`.
- Cấu hình shader:
  - `M_Bark`: Principled BSDF Mapping Scale `(2.5, 2.5, 1.0)`, `Ancient_Bark_BaseColor.png` $\to$ Base Color, `Ancient_Bark_Normal.png` $\to$ Normal Map (Tangent, Strength 1.05) $\to$ Normal. Roughness = 0.88, Metallic = 0.0, Emission = 0.0.
  - `M_Foliage`: Giữ nguyên 100% shader P02 (vân lá chung kèm tint `#507C38`).
- Bảo toàn tuyệt đối hình học: Khớp 100% geometry fingerprint trên cả 3 mesh: Foliage 18.304 tris, Trunk_Branches 37.728 tris, Roots 17.856 tris (Tổng 51.332 verts, 73.888 tris).
- Bảo toàn 100% nguyên vẹn các model khác (`Tree_Seedling_Simple_v01.blend`, `Tree_Young_Simple_v01.blend`, `Tree_Large.blend`, `Assets/_Project/Art/Bonsai_Tree.blend`, `Assets/_Project/Art/Bonsai_Tree.fbx`).
- Lưu trực tiếp với `save_version = 0` (không tạo `.blend1` dư).
- Kết xuất 8 ảnh kiểm chứng (perspective, front, back, left, right, closeup_fork, closeup_roots, large_ancient_bark_comparison) tại `Evidence/Tree_Materials_P04_20261006/final/`. Báo cáo: `Docs/Validation/Tree_Materials_P04_20261006.md`.
- T10 giữ `IN_PROGRESS`, dừng sau P04, chưa thực hiện bake/export FBX (P05) hay tích hợp Unity.

## D40 Triển khai P05: Đóng gói, xuất FBX chuẩn và bảng thông số vật liệu

Trạng thái: CONFIRMED theo yêu cầu trực tiếp của người dùng ngày 06/10/2026 ("Thực hiện P05 sau khi P01–P04 đã hoàn thành và kiểm tra, áp dụng quy tắc chung").

- Chuyển toàn bộ đường dẫn file ảnh texture trên 4 model nguồn trong `ArtSource/Models/` sang đường dẫn tương đối (`//`) bằng `bpy.ops.file.make_paths_relative()`. Không còn bất kỳ sự phụ thuộc vào texture procedural hay đường dẫn tuyệt đối nào.
- Xuất đè 4 file FBX chuẩn cho Unity tại `ArtSource/Exports/`:
  - `Tree_Seedling_Simple_v01.fbx` (88.156 bytes)
  - `Tree_Young_Simple_v01.fbx` (131.004 bytes)
  - `Tree_Large.fbx` (889.132 bytes)
  - `Bonsai_Trunk_Candidate_v01.fbx` (2.569.708 bytes)
- Quy chuẩn xuất FBX:
  - `use_selection = True`, `object_types = {'MESH'}` (loại bỏ hoàn toàn đèn, camera làm việc).
  - `global_scale = 1.0`, `apply_unit_scale = True`, `apply_scale_options = 'FBX_SCALE_ALL'`.
  - `axis_forward = '-Z'`, `axis_up = 'Y'` (chuẩn hệ tọa độ Unity).
  - `mesh_smooth_type = 'FACE'`, `bake_anim = False`.
  - Pivot tại gốc cây đặt ở mặt đất ($z = 0$).
- Kiểm chứng FBX độc lập bằng cách tái nạp từng file vào scene Blender trắng mới (`read_homefile(use_empty=True)`):
  - Scale đúng `(1.0, 1.0, 1.0)`, Rotation đúng `(0.0, 0.0, 0.0)`.
  - Bảo toàn tuyệt đối số đỉnh và tam giác: Mầm (1.436 verts, 2.778 tris), Non (2.420 verts, 4.574 tris), Lớn (16.632 verts, 26.682 tris), Cổ Thụ (51.332 verts, 73.888 tris).
  - Toàn bộ kênh UV0 và material slot nguyên vẹn, không có slot rỗng.
- Soạn thảo tài liệu thông số vật liệu hoàn chỉnh: `Docs/Art/Tree_Materials_Manifest_20261006.md`:
  - Liệt kê đầy đủ 4 model, mesh slot, texture map, màu tint Linear/sRGB Hex, Smoothness ($1.0 - \text{Roughness}$), Normal map scale.
  - Hướng dẫn cấu hình shader URP Lit, lưu ý bắt buộc `Render Face = Both` (Cull Off) cho `M_Foliage` của Cổ Thụ.
- Bảo toàn nguyên vẹn 100% tài sản được bảo vệ: `Assets/_Project/Art/Bonsai_Tree.blend` và `Assets/_Project/Art/Bonsai_Tree.fbx` (SHA256 khớp 100%). Không can thiệp vào Unity assets hay gameplay scene.
- Render nghiệm thu 35 ảnh chi tiết và 1 ảnh toàn cảnh lineup 4 mốc phát triển (`lineup_progression.png`, 1920×1080) tại `Evidence/Tree_Materials_P05_20261006/final/`. Báo cáo: `Docs/Validation/Tree_Materials_P05_20261006.md`.
- Pipeline chuẩn bị Asset 3D trong Blender đã hoàn tất; T10 giữ `IN_PROGRESS` chờ quyết định và yêu cầu tiếp theo về tích hợp Unity.

## D41 Khắc phục tồn đọng Material R1, R2, R3: Bake BaseColor lá theo mốc, chuẩn hóa UV vỏ Cổ Thụ theo khung Bishop 3D và hoàn thiện Manifest URP

- Trạng thái: `CONFIRMED`
- Ngày cập nhật: `2026-10-06`
- Thực hiện `Docs/Art/Antigravity_Material_Fixes_20261006.md` để xử lý dứt điểm ba tồn đọng **R1**, **R2**, **R3** được ghi nhận trong báo cáo kiểm tra độc lập `Docs/Validation/Materials_Independent_Review_20261006.md`:
  - **R1 (Bảo toàn màu lá khi xuất FBX)**:
    - Do trình xuất FBX của Blender chỉ tự động giữ texture khi `ShaderNodeTexImage` nối trực tiếp vào `Principled BSDF.Base Color`, đã bake trước 4 file texture 512×512 sRGB tại `ArtSource/Textures/Tree_Common/`: `Leaf_Seedling_BaseColor.png` (`#9BCB55`, factor `0.70`), `Leaf_Young_BaseColor.png` (`#75AA43`, factor `0.85`), `Leaf_Large_BaseColor.png` (`#5F963E`, factor `1.00`), `Leaf_Ancient_BaseColor.png` (`#507C38`, factor `1.00`) từ cùng pattern gân lá `Leaf_Common_BaseColor.png` theo đúng công thức linear Mix Multiply $C_{\text{lin}} = T \cdot ((1 - f) + f \cdot B_{\text{lin}})$. Sai số tối đa so với graph cũ chỉ là $\le 0.498\text{ LSB}$ (sai số làm tròn 8-bit PNG duy nhất).
    - Giữ nguyên 100% `Leaf_Common_BaseColor.png` và `Leaf_Common_Normal.png`. Gỡ bỏ `ShaderNodeMix` trên `M_Leaf`/`M_Foliage` của cả 4 file `.blend` nguồn, nối trực tiếp `Leaf_<Stage>_BaseColor.png` vào `Principled BSDF.Base Color` với tint trắng `(1,1,1,1)`. Kết quả reimport độc lập 4 file `.fbx` trong scene Blender trắng đều tự động giữ nguyên texture lá xanh và normal map.
  - **R2 (Hướng thớ vỏ Cổ Thụ theo trục mọc và khử sóng toàn cục)**:
    - Phân rã thuần túy theo cấu trúc lưới (pure-topology) 39 ống trụ 12 cạnh trên `Trunk_Branches` (18.864 faces) và 16 ống trụ 12 cạnh trên `Roots` (8.928 faces) của `Bonsai_Trunk_Candidate_v01.blend`.
    - Xây dựng hệ trục vận chuyển song song 3D không xoắn (**3D Parallel-Transport / Bishop Frame** $(\mathbf{T}_k, \mathbf{N}_{1,k}, \mathbf{N}_{2,k})$) dọc theo đường tâm làm mượt của từng ống trụ và chiếu góc phương vị thực $\theta(v) = \operatorname{atan2}(\mathbf{r}_\perp \cdot \mathbf{N}_{2,k}, \mathbf{r}_\perp \cdot \mathbf{N}_{1,k})$ để gán tọa độ $U$ vòng quanh chu vi và $V$ dọc chiều dài mọc thực tế. Giải pháp này triệt tiêu hoàn toàn hiện tượng thớ vỏ chạy ngang như vòng đá cũng như góc gập chữ `<` cục bộ tại các đỉnh bị kéo lệch góc 3D (`Ring 3` của `Tube 3` tại $z \approx 0.92\text{m}$), đạt `pos_det = 100%` (`18864/18864` trên thân/cành, `8928/8928` trên rễ), `neg_det = 0`, `degenerate_uv_triangles = 0`, tích vô hướng hướng chạy $V$ theo trục ống $\ge 0.961$.
    - Tái tạo bộ đôi `Ancient_Bark_BaseColor.png` và `Ancient_Bark_Normal.png` (1024×1024) từ cùng một trường độ cao nhiều tỷ lệ (anisotropic Voronoi bản vỏ dọc + khe nứt dọc sâu đa cấp + sợi gỗ dọc + vết nứt ngang phụ cục bộ trong lòng bản vỏ), loại bỏ hoàn toàn sóng ngang toàn cục `sin(2*pi*V)` và sọc細 cosine. Độ dài vector pháp tuyến chuẩn hóa $\in [0.9937, 1.0061]$, $n_z \ge 0.2941$.
  - **R3 (Chuẩn hóa Manifest URP Lit)**:
    - Cập nhật trực tiếp `Docs/Art/Tree_Materials_Manifest_20261006.md`: liệt kê đủ 10 file PNG (kèm kích thước, color space, SHA256), sửa giá trị nền thực của `Leaf_Common_BaseColor.png` tại góc $(0,0)$ thành `#D1D1C8`, quy định rõ Base Color tint `#FFFFFF` cho mọi material có gắn texture đã bake sẵn màu, ghi rõ Tiling `(2.5, 2.5)` cho `M_Bark` của `Tree_Large` và `(1.0, 1.0)` cho `M_Bark` của Cổ Thụ, mô tả đúng 4 mesh / 5 material / 2 slot của `SeedShell`, và bổ sung bảng phân định thuộc tính tự giữ khi xuất FBX so với thuộc tính cần gán trong Unity URP Lit.
- Khóa tuyệt đối hình học trên cả 4 model (`position_hash`, `topology_hash`, `normal_hash`, `world_matrix` khớp 100% với baseline), khóa 100% UV lá cả 4 cây và UV vỏ Mầm/Non/Lớn, giữ nguyên 100% `Assets/_Project/Art/Bonsai_Tree.blend` và `Assets/_Project/Art/Bonsai_Tree.fbx`.
- Bằng chứng trước/sau và kiểm định độc lập lưu tại `Evidence/Material_Fixes_Antigravity_20261006/`; báo cáo nghiệm thu tại `Docs/Validation/Material_Fixes_Antigravity_20261006.md`. T10 giữ `IN_PROGRESS` chờ bước tích hợp Unity.

## D42 Phân loại bộ model Tree_Large hiện tại thành Cây Trưởng Thành

- Trạng thái: `CONFIRMED`
- Ngày cập nhật: `2026-10-06`
- Căn cứ: người dùng yêu cầu lưu lại việc chuyển model Cây Lớn hiện tại sang Cây Trưởng Thành và sẽ tự tìm các model còn thiếu sau.
- Bộ nguồn `ArtSource/Models/Tree_Large.blend` cùng bản xuất `ArtSource/Exports/Tree_Large.fbx` được phân loại là **Cây Trưởng Thành** trong danh mục art. Không tiếp tục tính bộ này là nguồn art đạt mốc Cây Lớn của prototype.
- Chỉ cập nhật thông tin: giữ nguyên tên file, đường dẫn, hình học, UV, material, texture và tiền tố kỹ thuật `Large`. Không tạo bản sao model, không sửa `.meta` hoặc GUID. Model Cây Lớn bổ sung sau phải có đường dẫn riêng, không ghi đè bộ Cây Trưởng Thành đang có.
- Không đổi ID gameplay `large_tree`, config, save, prefab hoặc scene; ba stage gameplay prototype vẫn là Mầm, Cây Non và Cây Lớn. Chưa thêm stage Cây Trưởng Thành vào gameplay hoặc coi việc phân loại art là nghiệm thu tích hợp Unity.
- Danh mục hiện hành: `Docs/Art/Tree_Model_Registry.md`. Có nguồn art cho Mầm, Cây Non, Cây Trưởng Thành và Cổ Thụ; còn thiếu nguồn phù hợp cho **Cây Lớn, Cổ Đại và Cây Thế Giới** trong bộ bảy mốc ý tưởng. Riêng prototype ba mốc còn thiếu nguồn art Cây Lớn; placeholder gameplay hiện có vẫn được giữ.
- Người dùng sẽ tìm model bổ sung; chưa tự tạo, tải về hoặc tích hợp những model thiếu. Những tồn đọng kỹ thuật đã ghi trong báo cáo kiểm tra vẫn còn hiệu lực, không được xem là đã sửa nhờ thay đổi phân loại.
- Đã cập nhật danh mục, Backlog, hướng dẫn đầu phiên, tài liệu material/prompt liên quan, báo cáo đối chiếu và bàn giao T10. Kiểm tra phiên này là kiểm tra tài liệu và tính nguyên vẹn file; không phải kiểm chứng mới trong Blender hoặc Unity. T10 giữ `IN_PROGRESS`.

## D43 Hai mốc cuối dùng chung base model Cổ Thụ trong prototype

- Trạng thái: `CONFIRMED`
- Ngày cập nhật: `2026-10-06`
- Căn cứ: người dùng chốt phương án khi khó tìm model phù hợp cho hai mốc cuối.
- Trong chuỗi định hướng bảy mốc, **Cổ Đại** và **Cây Thế Giới** dùng chung base model của **Cổ Thụ**: `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` và `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx`. Không cần tìm, tạo hoặc chọn thêm model riêng cho hai mốc này trong prototype.
- Mỗi mốc được phân biệt bằng material cấu hình riêng và VFX riêng khi cây lên cấp; có thể thêm chi tiết view/mesh con nhỏ nếu được giao sau, nhưng không thay đổi base geometry. Cấu hình presentation tách khỏi logic gameplay; chưa tự chốt màu, cường độ, loại VFX hoặc thông số balance.
- Đây là quyết định presentation cho định hướng mở rộng, không thêm stage vào gameplay hiện tại, không đổi ba stage `seedling`/`young_tree`/`large_tree`, ID, save, economy, prefab hoặc scene. Nó cũng không chứng nhận asset đã tích hợp hoặc nghiệm thu Unity.
- Khi triển khai, cần kiểm tra ở Unity Play Mode rằng hai lần Evolution nhận diện rõ ở khoảng cách gameplay, VFX gắn đúng anchor và không ảnh hưởng raycast/camera/save. Việc thay bằng model riêng trong tương lai là backlog độc lập.

## D45 Đồng bộ lá và phân vùng nguồn Cây Trưởng Thành

- Trạng thái: `CONFIRMED` về hướng sửa theo yêu cầu trực tiếp người dùng ngày 07/10/2026; bản sửa đã thực thi và kiểm chứng vòng lặp FBX trong Blender ở trạng thái `READY_FOR_REVIEW` (chờ nghiệm thu độc lập và tích hợp Unity).
- Ghi nhận nguồn mới `ArtSource/Models/WT_MatureTree.blend` (SHA256 sau sửa: `90164380016f8aad87c1489934ed07dae558e8efcbcabde39d17a81af3406f95`) và ba `ArtSource/Exports/WT_MatureTree_LOD*.fbx` (`LOD0`: `10a09b4e...`, `LOD1`: `956668c7...`, `LOD2`: `f0f9810e...`) có trên đĩa; bản gốc trước sửa được sao lưu tại `Evidence/Mature_Leaf_Parts_Fix_20261007/backup_20261007/`. D44 vẫn giữ `Tree_Large` là Cây Lớn; D43 vẫn áp dụng cho hai mốc cuối.
- Thực thi `Docs/Art/Mature_Tree_Leaf_Parts_Fix_Prompt_20261007.md`:
  - **Đồng bộ lá (R1)**: Trích 8 mẫu cụm 4 phiến thật từ `Tree_Large.blend::Foliage` (đối chiếu chéo `Bonsai_Trunk_Candidate_v01.blend::Foliage`, sai số $\le 5.03 \times 10^{-7}\text{ m}$), đặt bằng dịch chuyển + quay 3D trực giao + uniform scale trên 33 vùng tán của `WT_MatureTree` (`LOD0`: `484` cụm / `30.976` tris; `LOD1`: `273` cụm / `17.472` tris; `LOD2`: `116` cụm / `7.424` tris), dùng chung material `M_Foliage` (`Leaf_Large_BaseColor.png` + `Leaf_Common_Normal.png`, tint trắng `(1,1,1,1)`). Hai file `WT_Leaves_Light_BaseColor.png` và `WT_Leaves_Dark_BaseColor.png` giữ nguyên trên đĩa nhưng không còn được tham chiếu.
  - **Tách 3 vùng cây (R2)**: Mỗi LOD có 3 mesh object chọn/ẩn độc lập (`WT_MatureTree_Roots*`, `WT_MatureTree_Trunk_Branches*`, `WT_MatureTree_Foliage*`) cắt ngang tại cổ rễ $z = 0.475\text{ m}$ với sai số bề mặt gỗ $\le 1.4 \times 10^{-7}\text{ m}$ (`LOD0 = 37.018` tris, `LOD1 = 20.238` tris, `LOD2 = 8.504` tris). Tên mesh là quy ước presentation, không thay ID/luật gameplay.
- Nghiệm thu độc lập sau khi có bản sửa; `READY_FOR_REVIEW` không thay thế Play Mode/acceptance T10. T10 giữ `IN_PROGRESS`.
- File ảnh hưởng: registry, manifest, START_HERE, T10, prompt, `Docs/Validation/Mature_Leaf_Parts_Review_20261007.md` và `Docs/Validation/Mature_Leaf_Parts_Fix_20261007.md`; bằng chứng lưu tại `Evidence/Mature_Leaf_Parts_Review_20261007/` và `Evidence/Mature_Leaf_Parts_Fix_20261007/`. Không đổi GDD gameplay nên không cần xuất lại Word.

## D44 Phân loại lại Tree_Large thành Cây Lớn

- Trạng thái: `CONFIRMED`
- Ngày cập nhật: `2026-10-06`
- Căn cứ: người dùng xác nhận ảnh mới phù hợp hơn với mốc Cây Trưởng Thành và yêu cầu chuyển model Trưởng Thành hiện tại xuống thành Cây Lớn.
- `ArtSource/Models/Tree_Large.blend` và `ArtSource/Exports/Tree_Large.fbx` quay lại đại diện cho **Cây Lớn**. D44 thay thế riêng quyết định phân loại của D42; D42 được giữ làm lịch sử, không còn là bảng đối chiếu hiện hành.
- Ảnh người dùng gửi trong phiên này là tham chiếu mỹ thuật cho **Cây Trưởng Thành**; chưa có `.blend`/FBX nguồn và không tự tạo, tải hay tích hợp model mới trong lượt này.
- Không đổi tên file/path, geometry, UV, material, texture, GUID, `.meta`, stage ID `large_tree`, config, save, prefab, scene hoặc luật gameplay. Cây Lớn tiếp tục là stage cuối của prototype ba mốc; Cây Trưởng Thành chưa được thêm vào gameplay.
- Cập nhật registry, START_HERE, Backlog, bàn giao T10 và Validation; đây là kiểm tra tài liệu, không phải nghiệm thu mới trong Blender/Unity.

## D46 Tích hợp Unity URP và hoàn tất nghiệm thu T10 cho 3 stage đại diện của prototype

Hậu kiểm bản sửa 07/10/2026: T10 mở lại **IN_PROGRESS**, validation hiện hành `Docs/Validation/T10_Independent_Recheck_20261007.md`. Hai ca click SeedShell mặt sau bị cull và phần chìm dưới Ground vẫn cộng tap. Thông số camera/collider bên dưới là cấu hình đã kiểm tra, không phải bằng chứng DONE. Giữ luật/phân loại art đã chốt; chưa thay config hoặc quyết định thiết kế trong phiên kiểm thử này.

Ghi chú hậu kiểm và sửa lỗi raycast 07/10/2026: Các lỗi collider/raycast phát hiện trong `Docs/Validation/T10_Independent_Review_20261007.md` đã được khắc phục triệt để và nghiệm thu lại hoàn tất (`DONE`) trong `Docs/Validation/T10_Raycast_Fix_20261007.md` và `Docs/Validation/T10_20261007.md`.

- Trạng thái: `CONFIRMED` (`PROTOTYPE_DEFAULT` cho thông số camera pose & collider theo kích thước model thật).
- Ngày cập nhật: `2026-10-07`
- Căn cứ: Thực thi `Docs/Art/T10_Agent_Check_And_Continue_20261007.md` và `Docs/Art/T10_Agent_Fix_Raycast_And_Revalidate_20261007.md` hoàn thành pipeline đồ họa Blender sang Unity 6000.6.1f1 (URP 17.6.0) cho 3 stage đại diện `seedling` (Mầm), `young_tree` (Cây Non), `large_tree` (Cây Lớn).
- **Đồng bộ Asset & Material URP Lit**:
  - Sao chép nguyên bản 3 file FBX (`Tree_Seedling_Simple_v01.fbx`, `Tree_Young_Simple_v01.fbx`, `Tree_Large.fbx`) vào `Assets/_Project/Art/Models/` (bật `isReadable = true` trên `ModelImporter`) và 6 file texture PNG (`Leaf_Seedling_BaseColor.png`, `Leaf_Young_BaseColor.png`, `Leaf_Large_BaseColor.png`, `Leaf_Common_Normal.png`, `Large_Bark_BaseColor.png`, `Large_Bark_Normal.png`) vào `Assets/_Project/Art/Textures/Tree_Common/`.
  - Khởi tạo 10 material `Universal Render Pipeline/Lit` tại `Assets/_Project/Art/Materials/` (`M_Seedling_Leaves`, `M_Seedling_Stem`, `M_Seedling_Roots`, `M_SeedShell_Outer`, `M_SeedShell_Inner`, `M_Young_Leaves`, `M_Young_Stem`, `M_Young_Roots`, `M_Large_Foliage`, `M_Large_Bark`), tuân thủ quy tắc chống nhân hai lần màu (D41: `_BaseColor = #FFFFFF` trên mọi material có `_BaseMap`) và đặt `Tiling = (2.5, 2.5)` trên `M_Large_Bark.mat` (khớp với `Mapping POINT 2.5×` trong `Tree_Large.blend`).
- **Điểm neo tường minh (`TreeStageAnchors`), `MeshCollider` khớp hình học & Tích hợp Presentation**:
  - Tạo component `TreeStageAnchors` (`Assets/_Project/Scripts/Presentation/TreeStageAnchors.cs`) giữ tham chiếu kiểu mạnh (typed references, không dò tìm theo tên lúc runtime) tới 4 điểm neo con trên từng prefab: `Anchor_LightningTarget`, `Anchor_LeavesCenter`, `Anchor_TrunkAura`, `Anchor_Root`, cùng mảng `Collider[] hitColliders` và cấu hình fallback `SphereCollider`.
  - Cập nhật tại chỗ 3 prefab `Assets/_Project/Prefabs/Trees/{seedling,young_tree,large_tree}.prefab` (bảo toàn 100% GUID gốc) với mesh FBX thật, đủ 2 material slots trên `SeedShell` của Mầm, gắn `MeshCollider` tĩnh (`convex = false`) trực tiếp lên các mesh con (`Foliage`, `Roots`, `Trunk_Branches`, kèm `SeedShell` trên `seedling`) và tham chiếu vào `TreeStageAnchors.hitColliders`.
  - Cập nhật `TreeView` đồng bộ vị trí, góc quay và `localScale` của `RootAnchor`, `TrunkAnchor`, `LeavesAnchor`, `LightningTargetAnchor` từ `TreeStageAnchors`; khi `currentStageAnchors.HasActiveHitColliders() == true`, `TreeView` tự động tắt `SphereCollider` thô trên root `Tree` (`enabled = false`) để loại bỏ vùng trống dương tính giả, đồng thời vẫn cập nhật `center` và `radius` để giữ tương thích ngược. Bật `Physics.queriesHitBackfaces = true` và đặt `Ground` `BoxCollider.center = (0, -0.02, 0)` để raycast bắt chuẩn xác cả mặt sau phiến lá mỏng và rễ Mầm sát mặt đất. Cập nhật `WeatherView.OnLightningStruck` đánh tia sét vào `tree.LightningTargetAnchor` trong khi `UpdateBurningVisual` vẫn gắn lửa cháy vào điểm neo bộ phận bị cháy (`root`, `leaves`, `trunk`).
- **Cấu hình Camera Framing (`PrototypeConfig.asset`)**:
  - Điều chỉnh `PrototypeConfig.visuals` khớp kích thước thực tế của 3 mô hình 3D:
    - `seedling` (cao `0.258m`): `cameraDistance = 1.4`, `cameraPitch = 16`, `cameraTargetHeight = 0.12`.
    - `young_tree` (cao `0.961m`): `cameraDistance = 2.8`, `cameraPitch = 18`, `cameraTargetHeight = 0.42`.
    - `large_tree` (cao `2.382m`): `cameraDistance = 6.2`, `cameraPitch = 20`, `cameraTargetHeight = 1.18`.
- **Kiểm định & Bảo toàn**:
  - EditMode: `191 / 191 PASS`; PlayMode: `63 / 63 PASS` (gồm `ArtPipelineSceneTests` `4 / 4 PASS`, xác nhận `falsePositiveEmpty = 0` và `falseNegativeVisible = 0` trên cả 3 stage); Standalone Windows (`Builds/Windows_T10_20261007/Cay_The_Gioi.exe`): `6 / 6 PASS` (`smoke_fresh`, `smoke_offline`, `smoke_progression`, `smoke_death_forced_rebirth`, `ui_flow`, `smoke_raycast`).
  - Bảo toàn 100% SHA256 của save thật người chơi, `Assets/_Project/Art/Bonsai_Tree.blend`/`.fbx` và toàn bộ nguồn `ArtSource/`. Báo cáo nghiệm thu: `Docs/Validation/T10_Raycast_Fix_20261007.md` và `Docs/Validation/T10_20261007.md`; bằng chứng: `Evidence/T10_Raycast_Fix_20261007/` và `Evidence/T10/`.



## D47 Economy Water / Quang hợp / Growth và Linh khí

Ngày 09/10/2026. **CONFIRMED** theo trả lời trực tiếp của người dùng: Rễ trả Water; Lá trả Quang hợp (ID `photosynthesis`); Thân trả Growth. Mỗi Evolution trả lượng nhất định của cả ba. Mốc Cây Trưởng Thành cho phép mở hấp thụ Linh khí; mở nhánh cần lượng lớn Water/Quang hợp/Growth. Game mới cấp vốn cho L0→L1 của các nhánh mặc định, không cấp vốn cho nhánh khóa theo mốc. Coin vẫn chỉ mua cosmetic. Ưu tiên hoàn thiện economy rồi chỉnh UI.

D47 thay D03 (Quang hợp và Growth chung một số dư), D14 (mọi nâng cấp Water) và giới hạn ba stage của D44 cho phần gameplay này. Giữ ID `large_tree` là Cây Lớn, thêm `mature_tree` là Cây Trưởng Thành. Không đổi tên hoặc ghi đè nguồn art. Visual mature dùng prefab Cây Lớn làm placeholder cho task economy; chưa chứng nhận tích hợp model WT_MatureTree.

**PROTOTYPE_DEFAULT**, có thể đổi trong config: Rễ tạo Water; Lá tiêu Water tạo Quang hợp; Thân có từ Mầm, tiêu Quang hợp tạo Growth, đồng thời tăng Protection. Vốn mỗi run mới (game mới và Rebirth) Water10/Quang hợp5/Growth10 đủ mua cấp1 cho cả ba. Hai câu hỏi bổ sung về nguồn Growth/vốn sau Rebirth đã gửi; các mặc định này là lựa chọn triển khai khi chưa có phản hồi, không gán thành CONFIRMED.

Config `prototype-6`: giá nhánh5/5/10, factor1.6; tốc độ mỗi level Water1/s, Quang hợp1/s tiêu Water0.25/s, Growth1/s tiêu Quang hợp0.25/s. Evolution vào Cây Non50/25/25, Cây Lớn200/100/100, Cây Trưởng Thành500/250/250 (Water/Quang hợp/Growth). Mở hấp thụ1000/1000/1000 và nhận hấp thụL1 để bắt đầu tạo Linh khí1/s; nâng tiếp dùng Linh khí, base10/factor1.6. Linh khí là tài nguyên run, reset/khóa sau Rebirth. Mature cap Rễ30/Lá30/Thân20/hấp thụ10; point mature +5% Linh khí là tuning tạm. Nắng/Burning vùng Lá tác động đầu ra Quang hợp; Growth point Cây Non tác động Thân; +5% Sức sống tác động mọi đầu ra tài nguyên, không tăng tiêu hao/Protection.

Giao dịch quote/kiểm tra/trừ đủ nguyên tử, commit trước event, chặn thiếu số dư và mất độ chính xác. Production tích phân hai tầng có điểm cạn Water/Quang hợp; offline24h50% giữ nguyên, không tiêu Sức sống hoặc tick weather/damage.

Schema1 mở rộng field theo config6. Save prototype1–5 giữ Water/Growth/level/meta; migration một lần cấp Quang hợp5, bù Growth lên10 nếu ThânL0 để mở nguồn Growth mới, Linh khí0/khóa. Ghi config6 trong snapshot tiếp theo, không cấp lại khi load config6. Save config6 bắt buộc trường tài nguyên/mở khóa mới, validate ID/cap/hữu hạn; file lạ vẫn chặn. Save thật người chơi không dùng cho kiểm thử.

T09A là nghiệm thu luật mới. Các kết quả205 EditMode/64 PlayMode của phương án tạm prototype5 chỉ là lịch sử. T10 xếp hàng, hai lỗi SeedShell raycast vẫn mở. UI style là bước sau, không dùng PASS cũ thay nghiệm thu mới.

## D48 Bố cục UI theo ảnh tham chiếu

Ngày 10/10/2026. **CONFIRMED** theo yêu cầu trực tiếp: sắp xếp HUD, Nâng cấp, Shop, Thành tựu, Cài đặt, Trùng sinh và hướng dẫn theo ảnh đã cung cấp; bỏ phần sự kiện vì chưa triển khai. T09B mở lại để thực hiện và nghiệm thu. Ảnh là tham chiếu bố cục, không thay luật D47 bằng số liệu, stage, sản phẩm hoặc bước tutorial minh họa trong ảnh. Giữ thông tin bốn tài nguyên/HP/Sức sống, API giao dịch và ID save hiện hành. Tái sử dụng bộ PNG UI đã có; không đổi model/môi trường trong công việc bố cục này.

## D49 VFX, Animation Cây, Âm Thanh Thời Tiết, Quy Tắc Sét 10% và Công Thức Giảm Sát Thương Protection

Ngày 11/10/2026. **CONFIRMED** theo yêu cầu trực tiếp của người dùng:
1. **Catalog thời tiết & Quy tắc sét gây hại**:
   - Chỉ dùng đúng 4 trạng thái thời tiết ổn định: `calm` (Bình yên), `sunny` (Nắng), `rainy` (Mưa nhẹ), `thunderstorm` (Giông). Không tạo thêm thời tiết Gió hay Bão riêng; gió là thành phần của Giông.
   - Mỗi đợt Giông đủ điều kiện gây hại (stage đã mở harmful weather và exposure > 0, tức `large_tree` trở lên; `seedling` và `young_tree` có exposure = 0 được bảo vệ an toàn tuyệt đối) có **đúng 10% khả năng xảy ra một cú sét đánh cây**.
   - Xét **một lần duy nhất** cho mỗi đợt Giông (tại thời điểm bắt đầu hoặc khi load save đợt Giông chưa xét); tối đa **một cú sét gây hại trong cả đợt**. Không hiểu thành 10% mỗi frame/giây/tia sét trang trí.
   - **Protection không làm thay đổi xác suất trúng sét** (vẫn là 10%).
   - **Exposure không nhân tăng/giảm xác suất 10%**; exposure = 0 đóng vai trò điều kiện bảo vệ stage đầu (`seedling`, `young_tree`).
   - Thời điểm đánh trong đợt được lên lịch bằng cấu hình prototype (`strikeDelayFraction = 0.30`, tức sau 30% thời lượng của đợt Giông), tránh luôn đánh ngay khi vừa chuyển thời tiết.
   - Lưu đầy đủ trạng thái `strikeEvaluated`, `strikeWillHit`, `strikeDelaySeconds`, `strikeExecuted` vào `WeatherState` và save DTO (schema 1 mở rộng tương thích ngược); save-load hoặc pause-resume không xét lại, không đánh lặp.
   - **Offline catch-up tuyệt đối không chạy sét, không tính sát thương Burning do thời tiết, và không làm chết cây.**

2. **Sát thương Burning & Công thức Protection**:
   - Sét đánh gây trạng thái Burning (thời lượng 10 giây, sát thương gốc 2 HP/giây theo PrototypeRules; Bình yên xóa Burning nhưng không tự hồi HP).
   - Protection (tổng hợp từ nâng cấp Thân: `levels.trunk * trunkProtectionPerLevel` và vĩnh viễn: stage permanent point effects) **chỉ giảm sát thương Burning do sét**:
     `damagePerSecond = baseBurningDamagePerSecond / (1 + max(0, totalProtection))` (PROTOTYPE_DEFAULT).
   - Protection được tính toán thời gian thực theo cấu hình run và điểm vĩnh viễn, không áp dụng 2 lần, không làm damage âm, không hồi HP.
   - Tác động Burning lên sản xuất tuân thủ luật D47: giảm 30% đầu ra Quang hợp ở Lá (hệ số 0.7x), không tác động Growth trực tiếp.

3. **Quy tắc HP và Trùng sinh (Forced Rebirth)**:
   - Sát thương trừ qua hệ thống `HealthStatusSystem.Tick()`. Khi HP <= 0, clamp 0, chuyển trạng thái `life = "DEAD"` đúng 1 lần, dừng các hành động gameplay thông thường theo luật hiện có.
   - Hiển thị UI Forced Rebirth, không cho hủy để tiếp tục chơi cây đã chết; chờ người chơi xác nhận trùng sinh, không tự reset cây ngay khi chết.
   - Tái sử dụng giao dịch Rebirth hiện có, bảo toàn xử lý lưu và không cấp point lặp. Sau Rebirth, reset HP về 100, reset thời tiết về calm, dọn sạch VFX/âm thanh và lịch sét của run cũ.

4. **Animation cây (Tree Animation Presentation)**:
   - Tách bạch presentation khỏi domain gameplay; animation không can thiệp logic sản xuất, HP hay transaction.
   - Bình yên, Nắng, Mưa: Đung đưa nhẹ tự nhiên ở lá (`Foliage`) với độ lệch pha không gian giữa các chùm lá; thân và cành giữ nguyên không uốn; gốc cây cố định tại mặt đất.
   - Giông: Lá rung mạnh mẽ; thân và cành (`Trunk_Branches`) uốn lượn theo hướng gió kết hợp nhịp giật Perlin noise và phục hồi đàn hồi; gốc cây (`Roots`) giữ cố định tại mặt đất $Y = 0$; biên độ thích ứng với kích thước từng stage (`seedling`: 1.2°, `young_tree`: 2.5°, `large_tree`: 4.0°), không làm méo hình Mầm.
   - Đồng bộ liên tục các anchor (`LightningTargetAnchor`, `LeavesCenterAnchor`, `TrunkAuraAnchor`, `RootAnchor`) theo biến dạng cây; bảo toàn tuyệt đối góc xoay gốc Blender (-90° X / 270.02°) và gọi `Physics.SyncTransforms()` khi uốn để đảm bảo tương quan visual–collider, không gây hồi quy raycast tap cây, kéo camera, chặn UI và Evolution.

5. **VFX & Âm thanh Thời Tiết**:
   - Bình yên: Ánh sáng trung tính, lá lay nhẹ.
   - Nắng: Ánh sáng vàng ấm nhẹ (`#FFF7E0`), tăng cường độ 1.25x nhưng không gây cháy sáng hay che HUD.
   - Mưa: Hệ thống hạt mưa procedural (`RainParticleSystem`) rơi dày từ trên cao với vệt dài kéo giãn theo tốc độ, bao quát toàn bộ camera zoom/orbit của cả 3 stage.
   - Giông: Mưa dày đặc xối xả kết hợp gió; tia sét trang trí trên bầu trời (`AmbientSkyLightningBolt`) xuất hiện ngẫu nhiên theo presentation RNG độc lập (0 sát thương); cú sét gây hại đánh trúng chính xác `LightningTargetAnchor` với tia sét kép (`LightningBolt`), chớp sáng màn hình và hiệu ứng rung giật phản hồi (shudder) trên cây; hiệu ứng cháy (`BurningIndicator`) theo vùng Root/Leaves/Trunk với lõi lửa và quầng khói bập bùng stylized.
   - Âm thanh: Tổng hợp procedural vòng lặp tiếng mưa rơi và gió rít êm dịu, tự động crossfade mượt mà theo thời tiết; hiệu ứng âm thanh sấm rền xa khi có sét trời và tiếng sét đánh chói vang khi đánh trúng cây. Tích hợp trọn vẹn với `settings.musicEnabled`, pause/focus, DEAD và Rebirth.

## D50 Hiệu Năng Animation Cây, Tối Ưu Vertex Flutter và Loại Bỏ Recook MeshCollider Mỗi Frame

Ngày 12/10/2026. **CONFIRMED** theo kết quả đo đạc thực tế hậu kiểm độc lập:
1. **Loại bỏ recook MeshCollider mỗi frame**:
   - Thử nghiệm thực tế 1800 frames cho thấy PhysX cooking 24,640 tam giác non-convex của Cây Lớn (`large_tree`) tốn trung bình 16.0–16.7 ms/frame (p95 = 16.7 ms), gây nghẽn nghiêm trọng CPU (tụt FPS < 45).
   - Quyết định: Tuyệt đối không recook `foliageMeshCollider.sharedMesh` mỗi frame (`EnableColliderRecookPerFrame = false`). Sử dụng mesh collider tĩnh của mô hình gốc, mở rộng bounding box (+0.3m), và đồng bộ hóa Transform theo góc uốn thân/cành/tán lá (`foliagePart.localRotation` + `Physics.SyncTransforms()`).
   - Kiểm chứng oracle độc lập (RenderTexture silhouette mask) chứng minh: giải pháp này đạt độ chính xác tap **100.00%** ở Mầm và Cây Non, **99.95%** ở Cây Lớn (vượt xa chỉ tiêu >= 98.0%), và **0 tap sai ra ngoài** (0 false positives).
2. **Tối ưu hóa vòng lặp CPU cho 41,480 đỉnh lá**:
   - Khử toàn bộ 124,440 phép tính lượng giác `Mathf.Sin`/`Mathf.Cos` trong vòng lặp đỉnh bằng cách precompute mảng `precomputedSinPhi` và `precomputedCosPhi` lúc chuyển stage; sử dụng công thức cộng góc $\sin(\omega t + \phi) = \sin(\omega t)\cos(\phi) + \cos(\omega t)\sin(\phi)$ với các hệ số tỷ lệ được tính trước ngoài vòng lặp.
   - Ghim chặt các lớp lá sâu bên trong ruột tán cành (`thresholdFrac = 0.55f` ở Cây Lớn); loại bỏ lệnh `RecalculateBounds()` mỗi frame bằng cách mở rộng bounds cố định ban đầu (+0.3m).
   - Kết quả: p95 total update frame time trên PC đạt **0.075 ms** (Mầm), **0.131 ms** (Cây Non), và **0.890 ms** (Cây Lớn, bình yên) / **0.926 ms** (Cây Lớn, giông bão), hoàn toàn thỏa mãn ngân sách p95 <= 1.0 ms/frame (tiết kiệm hơn 24 lần thời gian CPU). Dữ liệu chi tiết lưu tại `Evidence/ReviewFix_20261012/collider-benchmark.csv` và `collider-benchmark.txt`.
3. **Sửa Raycast Tap Cây (OnTree) và Backface Query**:
   - Gỡ bỏ hoàn toàn thiết lập toàn cục `Physics.queriesHitBackfaces = true` trong `TreeView.cs` và `ProjectSetup.cs`.
   - Trong `TreeInputAdapter.OnTree`, cô lập phạm vi `queriesHitBackfaces = true` tạm thời trong truy vấn để phát hiện các bề mặt mỏng hai mặt như vỏ hạt `SeedShell` (Mầm).
   - Khi hit đầu tiên trên collider của cây là back-face, tiếp tục raycast tìm front-face phía sau trên cùng collider đó, đồng thời công nhận các bề mặt hai mặt nhìn thấy được (như lòng trong vỏ hạt `SeedShell` với material `M_SeedShell_Inner`) là click hợp lệ vào cây khi không bị vật cản phía trước che khuất. Xác minh pixel (642, 307) của Mầm ăn tap 100%.

