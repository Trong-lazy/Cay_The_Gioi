# Mô hình dữ liệu

## Phân biệt ba loại dữ liệu

- Config: định nghĩa tuning, ID và luật cố định; không thay trực tiếp khi chơi.
- Runtime state: dữ liệu mutable của phiên; state chuẩn để service xử lý.
- Save DTO: bản copy thuần dữ liệu với version, validate/migrate trước khi đưa vào runtime. Không serialize live service.

## Config

- EconomyConfig: resource ID, tốc độ/chi phí theo nhánh, hệ số tăng, Water consumption, offline cap/rate.
- StageConfig: stable stageId, nextStageId, level cap mỗi nhánh, unlock flags, chi phí Evolution, exposure, camera và view prefab.
- VitalityConfig: tapsPerReward, secondsPerReward, maxBankSeconds, output multiplier 1.05; tác dụng/tiêu theo D11.
- WeatherConfig: weatherId, nhóm, duration range, rate risk, buff/debuff, cooldown/repeat rules.
- StatusConfig: Burning duration/damage, production multiplier theo vùng.
- PointEffectConfig: effect riêng từng stage, target và magnitude; không sửa cap.
- AchievementConfig/CosmeticConfig/TutorialConfig: stable ID, yêu cầu, Coin/price, view hoặc bước/target.

## Runtime state

GameState chứa RunState, PermanentState, SettingsState và VitalityState. PresentationState không lưu gồm panel mở, toast queue, preview, transition lock và tutorial spotlight hiện tại.

RunState: runId; life ALIVE/DEAD; stageId; Water/Growth; root/leaves/trunk level; HP; WeatherState; StatusState; counter/milestone chỉ thuộc run. cap/unlock/exposure lấy từ StageConfig, không lưu hai bản dễ lệch.

PermanentState: point list theo stageId; Coin; achievement progress và claimed; ownedCosmeticIds; equippedCosmeticId; tutorialCompletedStepIds; rebirthCount; highestStageEverId và milestone/counter vĩnh viễn.

SettingsState: musicEnabled hoặc musicVolume; orbitLocked; các setting đã thực hiện. Zoom chưa làm không có field giả cho tính năng hoàn chỉnh.

VitalityState: tapProgress và bankSeconds. Đây là quỹ thời gian tối đa 3600 giây, không phải resource Water/Growth hoặc HP. Giữ qua Rebirth và đóng băng offline; lưu độc lập khỏi RunState. Buff +5% đầu ra upgrade chỉ online ALIVE khi quỹ >0. Giữ tapProgress qua Rebirth là mặc định tạm D11. Không lưu multiplier buff trong state vì được suy ra từ quỹ và context.

WeatherState: weatherId, remainingOnlineSeconds, badStreak, recentWeatherIds/cooldown cần để khôi phục chống lặp. StatusState: statusId hoặc none, remainingOnlineSeconds, impactRegionId. VFX không được lưu.

## Số và tính hợp lệ

Resource/rate dùng double hữu hạn, không âm; kiểm tra NaN/Infinity và overflow khi cost tăng. Coin/point/level/counter dùng số nguyên không âm có kiểm tra tràn; không dùng số rút gọn HUD cho chi trả. Time dùng giây có đơn vị rõ; timestamp UTC dùng Unix seconds.

UI chỉ làm tròn khi hiển thị. Dùng một hàm so sánh/chi trả thống nhất để tránh UI báo đủ nhưng lệnh thất bại do làm tròn; không dùng epsilon lớn cho phép chi vượt tài nguyên thật.

IDs ổn định: `seedling`, `young_tree`, `large_tree`; branches `root`, `leaves`, `trunk`; resources `water`, `growth`; region `root`, `leaves`, `trunk`. ID cosmetic hoặc achievement có namespace/tiền tố dễ phân biệt. Không dùng index enum đang thay đổi làm giá trị save.

## Reset và giữ

- Evolution: đổi stage và trừ chi phí, giữ level/resource còn lại; cap phải hợp lệ, giữ permanent/settings/vitality.
- Rebirth: tạo runId mới, reset run theo config Mầm; +1 point stage cũ; giữ permanent/settings/tutorial, Vitality bank và tapProgress theo D11.
- Load: validate state và ID, reconcile derived caps/unlocks từ config version; migration rõ khi config đổi, không clamp tùy tiện để che lỗi.
- Tutorial: ghi completedStepIds; current step suy ra từ prerequisites và state, không tin con trỏ cũ đang bị khóa sau Rebirth.
- Shop: ownership/equip persistent; preview transient, luôn phục hồi equippedCosmeticId.

## Tính sản xuất

Root tạo Water; Leaves dùng Water tạo Growth. Evaluate nhận số dư ban đầu, level, elapsed và context modifiers. Tính cung Water trong khoảng, số thời gian Leaves được cấp nước và lượng Growth; trả delta/rate cho caller commit. Không cấp Growth vượt lượng nước và không trừ Water âm.

Với config prototype tuyến tính không cap storage, tính tích phân giải tích cho khoảng dài để offline 24h không cần chạy hàng triệu frame. Online và offline phải cho kết quả tương đương khi context giống nhau, kể cả trạng thái ban đầu thiếu Water. Quy tắc hệ số offline cho cung Water, nhu cầu Leaves và Growth tại Prototype_Config.

## T06 runtime và reset

RebirthQuote mang runId/stageId/effect/forced/reason; không lưu modal/preview/busy vào DTO. Rebirth +1 stagePoints của stageId trước reset, rebirthCount+1; không trao point stage thấp và không tăng cap. Run mới factory đổi runId, seedling/ALIVE, initial resources/level0/HP100/calm timer/statusnone, history/cooldown/runCounters mới. Permanent gồm Coin/achievements/cosmetics/tutorial/highest/counters được giữ; settings, vitality.bankSeconds/tapProgress giữ qua transaction. PermanentEffects đọc các entry theo stable ID thay thứ tự list. DEAD file giữ life/HP và mở forced modal sau load, offline/tick/tap/upgrade/evolve không chạy. Xem D18/T06 Validation.


## D47 runtime prototype-6

RunState có bốn double riêng `water/photosynthesis/growth/spirit`, bool `spiritAbsorptionUnlocked`, LevelState thêm int `spiritAbsorption`. Stage ID `mature_tree`, branch ID `spirit_absorption`; cap từ config. Linh khí không thể có số dư/level khi chưa mở. Snapshot/clone deep-copy mọi field; giao dịch chỉ trong domain. Rễ→Water→Lá→Quang hợp→Thân→Growth; Thân còn Protection. Rebirth reset Linh khí/khóa và cấp lại vốn mặc định theo config. Xem D47/Prototype_Config cho tuning và Save_Data_Schema cho migration.
