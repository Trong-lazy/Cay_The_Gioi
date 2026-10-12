# Kiến trúc kỹ thuật Cây Thế Giới

T01 triển khai `ProductionSystem.Evaluate(RunState, PrototypeRules, seconds, outputMultiplier)` thuần dữ liệu; `ResourceSystem.Tick` chia tại thời điểm Sức sống hết và commit trước event; `VitalitySystem.TryTap(inputAllowed)` trả CommandResult. `ResourceSystem.Rates()` hiển thị rate tức thời có xét thiếu Water. Bootstrap tạo snapshot config cho phiên, khởi tạo/nối service, HUD, OnlineTickDriver và TreeInputAdapter; không giữ công thức production hoặc đọc input. TreeInputAdapter sở hữu arbitration mouse/touch/UI và gọi `OrbitCameraRig.Orbit(delta)`; camera không còn tự đọc Mouse. SafeAreaHudLayout xử lý tỷ lệ/safe area.

T02 bổ sung UpgradeSystem.GetQuote/TryUpgrade dùng stable branch ID, stage cap làm unlock, cost Ceiling(base×factor^level), kiểm tra lại live state mỗi command và commit tài nguyên được quote/level trước ResourcesChanged/Completed. Callback reentrant nhận BUSY; lỗi không mutation/event. UpgradePanelView chỉ gửi API và đọc quote/event; có mở/đóng/+1, reason và output tiềm năng/consumption. HUD rate thực do ResourceSystem cung cấp. Trunk Protection đọc config0.05/level, không buff Sức sống; permanent effect thuộc T06. Debug stage runtime nằm trong Editor, chưa Evolution. Bằng chứng ở Validation/T02_20261002.md.

## Mục tiêu

T03 triển khai EvolutionSystem.GetQuote/TryEvolve/CompleteTransition. Kiểm tra stage, ALIVE, chi phí stage đích, cap giữ level, số hữu hạn và guard IsTransitioning; commit Water/Growth/stage/highestStageEverId trước ResourcesChanged/StageChanged, không trao point. UpgradeSystem nhận predicate busy từ composition để chặn command trong transition. EvolutionTransition nghe StageChanged, thay TreeView, cập nhật HUD và blend OrbitCameraRig với config; finally/OnDisable luôn trả input/transition lock, giữ setting orbitLocked riêng. UI EvolutionPanelView đọc quote/event, chỉ gửi command hoặc đổi setting qua nút orbit. GameBootstrapper chỉ tạo/nối; Save được nối ở T04, chưa Weather/Rebirth. Validation/T03_20261002.md ghi nghiệm thu.

Khung nhỏ, dễ kiểm thử và thay model. Kiến trúc dưới đây là hợp đồng triển khai cho các task. T00 đã bổ sung nền tảng config/state/DTO, bootstrap và view; các service gameplay trong bảng là công việc kế tiếp, chưa mặc định đã có. Xem Project_Setup.md và Validation để biết phần đã kiểm chứng. Không thêm DI framework, service locator hoặc event bus chung nếu không có nhu cầu thực tế.

## Các lớp trách nhiệm

- Config: ScriptableObject hoặc cấu hình dữ liệu cho economy, stage, weather, point, cosmetic, achievement và tutorial. Chỉ đọc lúc chơi.
- Domain: state C# và các service tính toán/kiểm tra/mutation; không reference Canvas, model hoặc MonoBehaviour view.
- Infrastructure: JSON save, clock UTC, RNG và chuyển config sang dữ liệu domain.
- Presentation: controller MonoBehaviour, view cây/camera/Weather, uGUI, audio. Chỉ gửi command và đọc snapshot/event.
- Composition: `GameBootstrapper` nối hệ thống, đăng ký subscriber, load save và chuẩn bị view. `TickDriver` cung cấp thời gian online. Không xử lý công thức economy tại đây.

Có thể tổ chức Scripts theo `Domain`, `Infrastructure`, `Presentation`, `Bootstrap` khi bắt đầu T00. Đừng tạo assembly/interface cho mọi class; chỉ tách assembly domain/test khi đem lại khả năng kiểm thử thực tế.

## Hợp đồng hệ thống

| Hệ thống | Trách nhiệm và API dự kiến |
| --- | --- |
| ProductionSystem | `Evaluate(state, config, elapsedSeconds, context)` tính Water/Growth với giới hạn nước; online và offline dùng chung luật, không mutation view. |
| ResourceSystem | `Tick(dt)`, đọc resource/rate, chi trả một giao dịch đã kiểm tra; không cho UI ghi số dư. |
| VitalitySystem | `TryTap()` tăng counter và cộng quỹ có cap; tiêu giây online ALIVE, cung cấp khoảng buff +5%; giữ khi offline/Rebirth. Tap UI không đi vào API. |
| UpgradeSystem | `GetQuote(branchId)`, `TryUpgrade(branchId)` kiểm tra sống/unlock/cap/cost rồi commit một level. |
| EvolutionSystem | `GetRequirements()`, `TryEvolve()` đổi stage và chi phí một lần; phát thông báo để view/camera chuyển. |
| RebirthSystem | `GetPreview()`, `TryRebirth(runId)` thưởng đúng stage và reset atomically; runId ngăn thao tác cũ chạy lại. |
| WeatherSystem | `Tick(dt)`, quản lý Weather, cooldown, RNG rate theo thời gian và thông báo impact; không tự trừ HP trong view. |
| HealthStatusSystem | HP, Burning, vùng tác động, kết thúc status và DEAD chỉ một lần. |
| AchievementSystem | cập nhật từ sự kiện/counter, `TryClaim(id)` trao Coin và claim cùng giao dịch. |
| ShopSystem | `TryBuy(id)`, `TryEquip(id)`; preview thuộc presentation và không mutation equip đã cam kết. |
| SaveService | `Load()`, `Save(snapshot)` có version/backup/atomic replacement và kết quả lỗi rõ. |
| OfflineService | tính catch-up theo checkpoint, neutral modifiers, cap 24h/rate 0.5; commit thưởng cùng checkpoint trước thông báo. |
| TutorialController | bước/target hợp lệ; hoàn thành dựa trên kết quả command, tương tác unlock theo priority. |
| UIController | panel, modal, toast, reason text, input arbitration; không xử lý economy. |

API trên là tên/hành vi đề xuất. Nếu thay tên, cập nhật tài liệu và giữ hợp đồng. `CommandResult` tối thiểu có success và reason; các reason như DEAD, LOCKED, CAP_REACHED, INSUFFICIENT_RESOURCE, BUSY và SAVE_FAILED hiển thị được.

## Luồng dữ liệu và event

Input hợp lệ → service kiểm tra → tính mutation → commit state → event nhỏ → HUD/view/audio phản ánh kết quả. Subscriber không tự thưởng resource/point lần nữa. Các thay đổi cần nhiều trường như Rebirth, buy/claim, offline phải commit một lần, không qua nhiều subscriber.

Event dự kiến: ResourcesChanged, UpgradeCompleted, StageChanged, VitalityChanged, WeatherChanged, ImpactOccurred, HealthChanged, RunDied, RebirthCompleted, AchievementClaimed, CosmeticEquipped. Dùng C# event có unsubscribe lúc disable/dispose. Không phát event thay đổi khi command thất bại.

Online tick thống nhất: xét Weather và status đang có, cập nhật HP/DEAD rồi production nếu ALIVE; nếu HP về 0 trong tick phải dừng production từ thời điểm chết. Có thể dùng bước mô phỏng cố định nhỏ và chia phần tick; ghi lựa chọn tại Decisions và test dt lớn. Offline không gọi vòng tick Weather/Health.

## Khởi động và shutdown

1. Nạp và xác minh config, ID/cap/cost hợp lệ.
2. Tạo domain service với clock/RNG được inject khi cần test.
3. Load/migrate save hoặc khởi tạo run mới đúng nguyên tắc lỗi.
4. Tính offline cho save cũ; commit thưởng/checkpoint thành công trước event thông báo. Không ghi save mới trước bước này làm mất khoảng offline.
5. Tạo view theo state, restore cosmetic đã equip, Weather/status và tutorial, sau đó cho phép input.
6. Khi thoát/pause/định kỳ, ghi snapshot nhất quán; shutdown unsubscribe. Không chỉ dựa vào OnApplicationQuit để save.

Nếu save lỗi, hiển thị trạng thái thực và cho retry; không báo “đã lưu”. Chính sách gameplay trong lỗi tại Save_Data_Schema.

## PC và điện thoại

Domain và luật game dùng chung. Input adapter chuyển mouse/touch thành command, phân biệt tap/drag và chặn UI; không để ResourceSystem đọc Mouse.current. UI theo safe area/tỷ lệ màn hình; lifecycle pause/focus ngừng tick và chốt save/checkpoint. Xem Cross_Platform.md; T00 chỉ có camera chuột, mobile chưa nghiệm thu.

## Tách đồ họa Blender

TreeView có pivot cây, slot model stage và anchor `root`, `leaves`, `trunk`. Mỗi StageConfig reference prefab view. VFX đi theo anchor; logic dùng ID vùng, không tên mesh. Thay model/prefab phải giữ pivot, scale, anchor và collider click; camera lấy thông số stage hoặc bounds view. Không mã hóa trực tiếp đường dẫn file blend vào domain.

## Kiểm thử

EditMode: công thức cost, production giới hạn Water, cap/unlock, point/reset, save/offline và transaction. PlayMode/thủ công: input/raycast, scene/view/camera, tutorial, modal DEAD, preview và integration. Có RNG seed/clock giả hoặc debug hook chỉ trong Editor để tái hiện; không phụ thuộc chờ ngẫu nhiên vài phút để kiểm thử.

## Save T04 đã triển khai

SaveCodec đọc JSON/version/default; SaveValidator kiểm tra dữ liệu; SaveSerializer deep-copy snapshot/restore; SaveService chỉ file I/O/atomic replacement và kết quả lỗi. SaveSession điều phối load/recovery có xác nhận và checkpoint commit trước khởi tạo gameplay. Bootstrap nối session→domain→view→SaveController, không chứa luật phục hồi/công thức tài nguyên. SaveStatusView gửi retry/recover/reset và hiển thị dirty/load error; không sửa state.

SaveController dùng interval từ config, execution order1000 sau online tick, gộp pause/focus, không autosave trong nền; SaveService checkpoint không lùi khi clock giảm. StageChanged đã commit gọi save trước khi hết transition. T04 chưa có offline production; T05 phải nạp candidate/save checkpoint trước cho input, không reset checkpoint tải thường trước khi tính thưởng.


## Offline T05 đã triển khai

OfflineProduction.Evaluate thuần domain dùng ProductionSystem với effective duration theo cap/factor config, neutral multiplier1; không tick hệ thống khác. OfflineCoordinator deep-copy candidate, Save resource/checkpoint rồi mới cập nhật source và trả báo cáo. SaveSession điều phối load trước domain/view/input. GameBootstrapper chỉ nối service/gate/view và báo cáo, không chứa công thức offline.

SaveController điều phối nền/resume và retry; ResourceSystem.SimulationBlocked/Vitality predicate/Upgrade busy/Evolution guard/Input.CatchUpBlocked khóa simulation và command trước commit. Pending error không autosave/quit unrewarded state; tick bỏ frame đầu sau resume để tránh double elapsed. OfflineRewardView nghe report đã commit, UI chỉ dismiss; SaveStatusView gửi retry cho startup hoặc resume theo trạng thái. T05 không implement point bonus T06/Weather T07; các bonus permanent khi có phải đi qua production context chung và thêm kiểm tra online/offline.

## T06 đã triển khai: Rebirth transaction và point context

PermanentEffects thuần domain tạo ProductionModifiers theo stage/target IDs; ResourceSystem/OfflineProduction/UpgradeSystem dùng chung, Sức sống chỉ nhân production online. RebirthSystem đọc quote, guard expectedRunId/busy, clone snapshot, reset factory và +1 point cũ; SaveController.CommitCandidate ghi atomic candidate rồi domain mới adopt run/permanent, event sau commit. Tick/tap/upgrade/evolution bị chặn trong transaction/reentrant event. Vitality/settings giữ cùng live instance, permanent deep-copy giữ meta. Observer lỗi không biến commit đã ghi thành thất bại.

GameBootstrapper chỉ tạo/nối các hệ thống và view. RebirthView quản lý preview/modal/input/camera/HUD, không mutate tài nguyên/point; labels dùng runtime rules snapshot. Commit xóa popup offline run cũ, snap placeholder/camera Mầm, refresh save status sau Resource notification. DEAD tự mở forced modal/cấm Cancel; pending offline load/save blocker ưu tiên retry. Editor RebirthDebug chỉ đặt DEAD khi có isolated ValidationDirectory, chưa damage/Weather T07. Xem Validation/T06_20261003.md.

T09A/D47: UpgradeQuote.CostResourceId lấy từ PrototypeRules theo branch ID và level. GetQuote kiểm tra cả hai số dư hợp lệ, đủ đúng tài nguyên và độ chính xác phép trừ; TryUpgrade re-quote live state, trừ Water hoặc Growth rồi tăng level trước event. Các resource IDs và ngoại lệ bootstrap Leaves thuộc immutable config snapshot; không thêm currency vào save DTO.


D47/prototype6: ProductionSystem tích phân chuỗi Water→Quang hợp→Growth, chia đoạn tại điểm Water cạn và tích phân cung/cầu Quang hợp trong mỗi đoạn. ResourceSystem commit bốn ví và bank sau khi tính mọi đoạn thành công. UpgradeSystem re-quote đúng ví; spirit_absorptionL0 là giao dịch mở bằng cả ba ví/cấp1, các cấp sau trả Spirit. EvolutionSystem commit cả ba giá và stage trước event. Bootstrap vẫn chỉ nối service/view; UI chỉ gọi command. OfflineCoordinator persist bốn ví trong cùng snapshot/checkpoint.
