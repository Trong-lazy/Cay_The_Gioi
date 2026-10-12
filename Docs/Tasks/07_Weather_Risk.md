---
id: T07
title: Weather Risk và Burning
status: DONE
depends_on: ["T06"]
validation: "Docs/Validation/T07_20261003.md"
---

# T07 Weather Risk và Burning

## Mục tiêu

Weather đổi môi trường placeholder, modifier/Risk đúng, sét → Burning → HP → DEAD nối Forced Rebirth.

## Thực hiện

- [x] Một Weather active, duration 180–600 cho Tốt/Xấu, unlock theo stage và cooldown/giới hạn chuỗi xấu.
- [x] Buff Water/Growth đúng phạm vi, một lần; Probability theo rate/elapsed giảm khi Protection tăng.
- [x] Seeded RNG/debug hook Editor, strike region/anchor, Burning và HP/status HUD.
- [x] Bình yên xóa Burning, neutral, không tự heal; event DEAD một lần và modal ưu tiên.
- [x] Lưu/khôi phục Weather/status/cooldown, không mô phỏng sự cố offline.

## Acceptance

- [x] Duration/unlock/single-active/anti-repeat đúng; Nắng/Mưa thay đúng resource, không tăng hai lần khi Sức sống có buff.
- [x] Giông có thể không trúng; early stages không trúng với exposure 0; protection tăng làm rate giảm.
- [x] RNG deterministic kiểm tra dt/FPS, debug strike bám đúng vùng; Burning Leaves giảm Growth.
- [x] HP về 0 dừng production và mở Forced Rebirth; subsequent ticks không phát chết/thưởng thêm.
- [x] Bình yên không damage/debuff/buff, không hồi HP đầy; status/save/offline hoạt động như spec.
- [x] Save DEAD reopen không offline resource; Rebirth xóa Burning và reset Weather.

## Nghiệm thu chung

- [x] Compile/tests risk/status/damage/DEAD/modifiers đạt (153 EditMode, 47 PlayMode PASS 100%).
- [x] Play Mode seeded/debug case, hồi quy T01–T06/save/offline và có log thực.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

Ngày 03/10/2026: T07 DONE theo Validation/T07_20261003.md; T00–T06 DONE trước sửa, T07 IN_PROGRESS trước triển khai. Dừng trước T08A.

File mới:
- `Assets/_Project/Scripts/Domain/WeatherSystem.cs`
- `Assets/_Project/Scripts/Domain/HealthStatusSystem.cs`
- `Assets/_Project/Scripts/Presentation/WeatherView.cs`
- `Assets/_Project/Editor/WeatherDebug.cs`
- `Assets/_Project/Tests/EditMode/WeatherTests.cs`
- `Assets/_Project/Tests/PlayMode/WeatherSceneTests.cs`

File sửa:
- `Assets/_Project/Scripts/Domain/PrototypeRules.cs` (thêm thông số weather/burning, configVersion prototype-4)
- `Assets/_Project/Scripts/Domain/ProductionSystem.cs` (thêm weather/burning multipliers)
- `Assets/_Project/Scripts/Domain/ResourceSystem.cs` (áp dụng weather/burning vào tick và rates)
- `Assets/_Project/Scripts/Presentation/OnlineTickDriver.cs` (điều phối tick thống nhất: weather -> health -> resource)
- `Assets/_Project/Scripts/Presentation/FoundationHudView.cs` (hiển thị thời tiết, burning status)
- `Assets/_Project/Scripts/Bootstrap/GameBootstrapper.cs` (khởi tạo, nối systems, T07 initialized log)
- `Assets/_Project/Scripts/Infrastructure/SaveValidator.cs` (cho phép các weatherId mới)
- `Assets/_Project/Scripts/Infrastructure/SaveCodec.cs` (tương thích ngược prototype-1..4)
- `Assets/_Project/Tests/PlayMode/FoundationSceneTests.cs` (cập nhật expectation log T07)
- `Assets/_Project/Tests/EditMode/RebirthTests.cs` (cách ly status trong rate test)
- `Assets/_Project/Configs/PrototypeConfig.asset` (lưu prototype-4 qua Unity API)
- Docs: Decisions D19, Evidence/T07/, Validation/T07_20261003.md, task/Progress/CURRENT_TASK.

Tất cả 153/153 EditMode và 47/47 PlayMode tests PASS 100%. Console 0 errors, 0 warnings.
Tiếp theo: T08A NOT_STARTED. Dừng phiên, không làm trước task kế tiếp.
