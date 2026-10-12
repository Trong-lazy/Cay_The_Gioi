---
id: T05
title: Sản xuất offline
status: DONE
depends_on: ["T04"]
validation: "Docs/Validation/T05_20261003.md"
---

# T05 Sản xuất offline

## Mục tiêu

Nhận production offline tối đa 24 giờ với tốc độ cơ sở 50%, neutral Weather/status/Sức sống, ghi thưởng/checkpoint một lần.

## Thực hiện

- [x] Clock UTC và clamp elapsed 0–86400; không lùi checkpoint khi clock âm.
- [x] Evaluate economy với effective duration bằng capped elapsed nhân 0.5, giữ permanent effects; không dùng rate HUD có buff.
- [x] Bank và counter Sức sống giữ nguyên; Weather/status không chạy offline.
- [x] Candidate reward + checkpoint lưu trước thông báo/input, lỗi có retry/rollback, không double credit.
- [x] Popup thông báo duration xét, resource thực nhận, giới hạn 24h/50%; số hiển thị khớp state.

## Acceptance

- [x] Root=1/Leaves=0/Water=0, 1 giờ neutral offline nhận 1800 Water; 24h nhận 43200, 30h cũng 43200.
- [x] Có Sức sống không làm số trên thành 1890; bank/counter trước-sau offline giữ nguyên.
- [x] Leaves thiếu Water được tính đúng nhu cầu, không Growth không nguồn nước; offline consumption/output không bị giảm hệ số hai lần.
- [x] 0 giây/clock âm/DEAD nhận 0; không click, Evolution, Rebirth Point, random strike hoặc damage offline.
- [x] Reopen ngay nhận 0; I/O fail/retry/crash simulation không cộng resource lần hai.
- [x] Weather/status online remaining giữ nguyên; lúc trở lại online Sức sống +5% hoạt động lại.

## Nghiệm thu chung

- [x] Compile và tests clock/cap/50%/water/dead/idempotency đạt.
- [x] Play Mode save/thoát/mở bằng clock thử, hồi quy T01–T04 và có log thực.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

Ngày03/10/2026: T05 DONE sau kiểm tra Unity, dừng trước T06. Config prototype-2 giữ nguyên offlineMaxSeconds86400/offlineRateFactor0.5/autosave30; giữ Unity6000.6.1f1/URP/Input System/package/StandaloneWindows64/MainScene.

File gameplay mới: Scripts/Domain/OfflineProduction.cs, Scripts/Infrastructure/OfflineCoordinator.cs, Scripts/Presentation/OfflineRewardView.cs (dưới Assets/_Project/). File sửa: Domain/ResourceSystem.cs, VitalitySystem.cs, EvolutionSystem.cs; Infrastructure/SaveService.cs, SaveSession.cs; Bootstrap/GameBootstrapper.cs, SaveController.cs; Presentation/TreeInputAdapter.cs, OnlineTickDriver.cs, SaveStatusView.cs. Bootstrap chỉ nối, evaluator dùng ProductionSystem chung; atomic reward/checkpoint commit trước source/event/input. Resume I/O fail giữ baseline/khóa gameplay, retry không double credit; backup recovery không replay thưởng cũ. Offline popup dùng cap/rate config, online tiếp tục sau commit.

Tests mới EditMode/OfflineTests.cs (16 ca), PlayMode/OfflineSceneTests.cs (6 ca). Sửa SaveTestScope clock cố định/temp riêng; FoundationSceneTests marker T05; SaveSceneTests cập nhật resume zero-time checkpoint commit. Hồi quy T00–T04 đầy đủ:109/109 EditMode;34/34 PlayMode ngang1920×1080 và34/34 dọc540×960,0 fail/skip. Ca portrait total0 không tính PASS, giữ evidence rồi chạy lại đủ34. Compile và smoke Console0 error/warning, MainScene clean/1 Bootstrapper/1 EventSystem.

Validation: Docs/Validation/T05_20261003.md; Evidence/T05 gồm kết quả runner, direct seed/open/Stop→Play/reopen0, fault/retry log, ảnh popup ngang/dọc/inset và modal I/O, final state/Console và SHA256 save người chơi trước/sau bằng nhau. Chưa kiểm chứng điện thoại thật/build độc lập/native pause-focus/notch/mobile file API; chỉ mouse/touch/lifecycle/inset mô phỏng Editor. D12 còn chờ OS/hướng màn hình, không chặn T05 Editor.

Tài liệu cập nhật START_HERE, Decisions D17, Technical Architecture/Save_Data_Schema/Cross_Platform/Project_Setup, Validation/Evidence. Không sửa GDD nên không xuất lại Word. Editor ValidationDirectory/ValidationClock/ValidationUtcSeconds đã null, MainScene mở và Play dừng.

Vào Play: mở MainScene, load save hiện có; Upgrade tạo Root/Leaves, Stop vài giây rồi Play lại xem popup offline, bấm Tiếp tục. Test clock giả chính xác chạy trong Test Runner, thư mục save temp riêng. Không triển khai T06; khi T06 làm bonus point cần nối context permanent vào cả online/offline chung và giữ hồi quy T05. Phần permanent hiện được giữ nguyên, chưa có công thức bonus Rebirth trước T06. Chạy update_progress.py để con trỏ T06 NOT_STARTED rồi dừng.
