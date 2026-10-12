---
id: T04
title: Save và load cơ bản
status: DONE
depends_on: ["T03"]
validation: "Docs/Validation/T04_20261002.md"
---

# T04 Save và load cơ bản

## Mục tiêu

Lưu/khôi phục run và Sức sống an toàn, schema sẵn cho permanent/meta/weather; không tính thưởng offline trong task này.

## Thực hiện

- [x] DTO/schema/version/ID theo Save_Data_Schema, lưu trong persistentDataPath.
- [x] Snapshot/restore deep-copy, temp/flush/verify/backup và atomic replacement không xóa main trước.
- [x] New/existing/corrupt/backup/unsupported/config lạ có hành vi rõ; retry/recovery/reset có xác nhận và giữ file cũ.
- [x] Autosave theo config30s, pause/focus/quit và stage Evolution đã commit; checkpoint online không lùi.
- [x] Gộp pause/focus vào nền một lần; autosave/tick/Sức sống dừng nền, resume không thưởng trong T04.
- [x] Khôi phục stage/level/resource/HP/DEAD/settings/tapProgress/bank/counter/meta; default field tương lai có test.

## Acceptance

- [x] Stop/Play khôi phục đúng state và view/camera; không reset bank/counter hoặc auto Evolution: Play tests và direct-save/reopen.
- [x] JSON lỗi dùng backup hợp lệ sau xác nhận; commit checkpoint mới trước gameplay, không thưởng: direct-recovery và Play tests.
- [x] Version mới không bị ghi đè; lỗi I/O hiện RetrySave và giữ main/revision: SaveSafety/SaveScene/direct-io-failure.
- [x] Lỗi AfterFlush/BeforeReplace/AfterReplace giữ JSON hoàn chỉnh/revision theo commit: 38 ca T04 EditMode.
- [x] DTO không có preview/panel/Coroutine/GameObject; thiếu field thực sự default và lõi thiếu bị chặn: SaveSafety/SaveSystem.

## Nghiệm thu chung

- [x] Compile sạch; 93/93 EditMode (T04 38 ca), 28/28 PlayMode (T04 12 ca).
- [x] Thử trực tiếp Unity Play/Stop/Play MainScene/PrototypeConfig; hồi quy T00–T03, ngang1920×1080/dọc540×960.
- [x] Validation PASS và Evidence/log/ảnh thật, lỗi các lượt trước được giữ.
- [x] DONE, chạy update_progress.py và kiểm tra con trỏ T05 NOT_STARTED, dừng trước T05.

## Bàn giao

Ngày02/10/2026: đã hoàn tất T04 theo Validation/T04_20261002.md. Sửa các lỗi nền từ bàn giao cũ: Delete→Move làm mất atomicity, overwrite version/corrupt qua fresh fallback, pause/focus lưu đôi, không có UI lỗi/recovery, snapshot/list alias và tests xóa save người chơi.

File mới/sửa trong T04:
- Infrastructure: SaveCodec.cs, SaveSession.cs, SaveSerializer.cs, SaveValidator.cs, SaveService.cs.
- Bootstrap: SaveController.cs, GameBootstrapper.cs (chỉ khởi tạo/nối).
- Presentation/Editor: SaveStatusView.cs, SaveSetup.cs, MainScene.unity (component SaveStatusView thêm qua Unity, giữ scene khác).
- Domain/config: PrototypeRules.cs (autosaveIntervalSeconds30), SaveData.cs (comment), PrototypeConfig.asset (field mới được Unity serialize; configVersion prototype-2 giữ nguyên).
- Tests: SaveSystemTests.cs từ bàn giao; SaveSafetyTests.cs/SaveSceneTests.cs/SaveTestScope.cs; FoundationSceneTests/CoreResourcesSceneTests/UpgradeSceneTests/EvolutionSceneTests đổi cleanup sang temp directory và log đúng.
- Docs: Decisions D16, Architecture, Prototype_Config, Save_Data_Schema, Cross_Platform, Validation/T04_20261002.md, Evidence/T04, task/Progress/CURRENT_TASK.

Config: autosave30s; economy, camera, input, Unity/URP/Input System và build target giữ nguyên. Save mặc định persistentDataPath; Editor test/manual dùng temporaryCachePath và đã trả override về null. MainScene clean, đã Stop Play, Console0error/0warning ở smoke cuối. Hash save người chơi trước thử trực tiếp/cuối phiên giống nhau.

Kiểm tra: EditMode93/93; PlayMode28/28 ngang và28/28 dọc, gồm 38 ca dữ liệu T04 và12 ca Play T04. Thử trực tiếp roundtrip/HUD/camera, dirty I/O/retry, backup checkpoint1000→now không thưởng và reset hai bước/archive2. Xem Evidence/T04/README.md.

Giới hạn: touch/pause/focus/safe area chỉ mô phỏng Editor; chưa build/thiết bị thật hoặc File.Replace Android/iOS (D12/T09). Không có blocker T04 còn lại. T05 chưa triển khai. T05 phải xử lý candidate offline/commit từ checkpoint trước khởi tạo input; không tự save làm mất elapsed. Weather/catalog/meta gameplay thuộc task sau.

Cách thử: MainScene→Play, tap cây/Upgrade/Orbit. Chờ30s hoặc menu Cây Thế Giới/T04 Save/Debug/Force Save Now; Stop→Play để tải lại. Khi file lỗi, UI cho Thử lại/Phục hồi backup/Bắt đầu lại có xác nhận, không cần bàn phím.

Tiếp theo: T05 NOT_STARTED. Dừng phiên, chưa triển khai task kế tiếp.
