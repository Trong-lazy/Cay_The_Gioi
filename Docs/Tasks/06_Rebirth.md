---
id: T06
title: Rebirth và point
status: DONE
depends_on: ["T05"]
validation: "Docs/Validation/T06_20261003.md"
---

# T06 Rebirth và point

## Mục tiêu

Rebirth tự nguyện và đường xử lý DEAD có giao dịch +1 point đúng stage, reset/giữ đúng và effect riêng. Dùng Editor hook đưa run DEAD trước khi T07 nối damage thực.

## Thực hiện

- [x] Preview đúng stage/point/effect/reset/giữ, confirm/cancel và unlock ở Cây Non.
- [x] Giao dịch runId guard, point theo stage cũ, reset run mới Mầm; persist trước event.
- [x] Permanent effect riêng áp đúng production/Protection, không tăng cap.
- [x] Giữ Coin/meta/settings/tutorial, bank và tapProgress Sức sống qua reset.
- [x] Modal DEAD chặn thao tác khác và Forced Rebirth; mở lại save DEAD vẫn đúng modal.

## Acceptance

- [x] Rebirth Cây Non chỉ +1 young_tree point, không +seedling/large_tree; hủy không thay state.
- [x] Reset resource/level/stage/HP/status/weather/run counter; giữ mọi dữ liệu vĩnh viễn và Sức sống.
- [x] Command runId cũ, double confirm, lỗi save/retry không nhận thêm point.
- [x] Point đúng effect, offline giữ permanent effect nhưng loại Sức sống; cap không đổi vì point.
- [x] DEAD không sản xuất/tap/upgrade/evolve/offline; xác nhận tạo run ALIVE mới và quỹ Sức sống tiếp tục.

## Nghiệm thu chung

- [x] Compile/tests reset/point/effect/transaction/DEAD đạt.
- [x] Play Mode confirm/cancel/save/load và hồi quy T01–T05, có log.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

Ngày03/10/2026: DONE theo Validation/T06_20261003.md; T00–T05 DONE trước sửa, T06 IN_PROGRESS trước triển khai. Không làm T07/Shop/Blender. GDD không sửa nên không export Word.

File mới: Domain/RebirthSystem.cs, PermanentEffects.cs; Presentation/RebirthView.cs; Editor/RebirthDebug.cs; Tests/EditMode/RebirthTests.cs; Tests/PlayMode/RebirthSceneTests.cs (dưới Assets/_Project). Unity tạo meta, không chỉnh GUID/meta thủ công.

File sửa: Scripts/Domain/PrototypeRules.cs, GameStateFactory.cs, ProductionSystem.cs, ResourceSystem.cs, OfflineProduction.cs, UpgradeSystem.cs, EvolutionSystem.cs; Infrastructure/SaveSerializer.cs, SaveCodec.cs; Bootstrap/SaveController.cs, GameBootstrapper.cs; Presentation/OfflineRewardView.cs. Tests/EditMode/SaveSafetyTests.cs, PlayMode/FoundationSceneTests.cs, SaveTestScope.cs. Configs/PrototypeConfig.asset được cập nhật qua Unity API, giữ scene/package/version/URP/InputSystem. EditorSettings khôi phục qua Unity API về reload domain+scene như T00.

Tuning: prototype-3, initialCalmSeconds120, pointEffects0.05/0.05/0.02 theo stage/target IDs. Water10/Growth0 theo factory config khi reset, không hard-code luật trong view. Schema1 vẫn giữ; saves prototype-1/2 được validate nâng stamp3, config lạ vẫn blocked. Transaction runId/busy→deep candidate→atomic persist→adopt/event; trước commit lỗi giữ state/file/point, retry+1; giữ toàn bộ meta/settings/quỹ/tap. DEAD forced modal/reopen, chặn gameplay/offline; debug DEAD chỉ Editor với isolated ValidationDirectory.

Test: 24 ca domain mới +7 scene mới. Tổng EditMode133/133, PlayMode41/41 mỗi viewport1920×1080 và540×960, hồi quy T00–T05. Direct MainScene Play/Stop/Play đã thử preview/cancel/IO fail/retry/double/stale/save-load/DEAD forced/reopen/effect/quỹ; số và ảnh thật Evidence/T06, Console cuối0 error/warning. Native Pointer cách ly chỉ trong test, restore enabled cuối. Lượt đầu fail/total0/sai viewport giữ log và không tính PASS, nguyên nhân/cách sửa trong Validation. Player main/backup SHA256 trước/sau giữ nguyên.

Tài liệu: D18/Technical config/architecture/data/save/cross-platform/setup, T00/T05 regression note, START_HERE, Validation và Evidence README cập nhật. Chạy Tools/Docs/update_progress.py sau DONE, xác nhận7/12 task đã nghiệm thu và CURRENT_TASK=T07 NOT_STARTED. Dừng trước T07.

Giới hạn: chưa phone thật/build độc lập, touch/safe area/native lifecycle chỉ mô phỏng Editor; D12 OS/hướng màn hình chưa chốt, file API mobile cần T09. Không blocker trong phạm vi T06. Cuối phiên đã bỏ override clock/thư mục thử, Mouse enabled, Play stopped, MainScene clean. Người dùng mở MainScene→Play→Upgrade Root/Leaves→Evolution Cây Non→Rebirth→Hủy/Xác nhận rồi Stop/Play để kiểm tra load.

Đính chính sau bàn giao: Console có3 log Error từ agent gọi SaveToSerializedFileAndForget với EditorSettings persistent sau snapshot console-final; không phải gameplay/compile/save game. Đã ghi console-followup.json và Validation, giữ log theo người dùng cho phiên sau; không gọi lại API sai. T06 DONE và T07 NOT_STARTED giữ nguyên.
