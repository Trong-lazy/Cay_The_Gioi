---
id: T08A
title: Achievement và Coin
status: DONE
depends_on: ["T07"]
validation: "Docs/Validation/T08A_20261003.md"
---

# T08A Achievement và Coin

## Mục tiêu

Sáu Achievement tạm có progress/claim và Coin vĩnh viễn đúng một lần.

## Thực hiện

- [x] Config stable ID, điều kiện theo Prototype_Config, progress từ sự kiện/counter đã có.
- [x] UI danh sách, chưa đạt/đạt/chưa claim/claimed rõ; claim atomic Coin + flag và save.
- [x] Restore/giữ qua Rebirth/load, không thưởng lại qua subscriber hoặc kiểm tra điều kiện khi load.

## Acceptance

- [x] Đạt từng milestone thay progress đúng; Sức sống lần đầu tính lúc nhận thời gian, không chỉ tap một lần.
- [x] Claim đủ điều kiện +10 Coin tạm; claim lại hoặc click nhanh không thưởng lặp.
- [x] Coin/claimed/progress giữ qua Rebirth và load; lỗi save không nhân đôi khi retry.
- [x] Coin không dùng cho Upgrade/Evolution hoặc cứu cây, Sức sống không buff reward Coin.

## Nghiệm thu chung

- [x] Compile/tests progress/claim/idempotency/save đạt.
- [x] Play Mode UI và hồi quy T01–T07, có log.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

- File thay đổi:
  - `Assets/_Project/Scripts/Domain/PrototypeRules.cs`: Thêm `AchievementDefinition`, 6 achievement mặc định (`first_root`, `first_leaves`, `evolve_young_tree`, `evolve_large_tree`, `first_rebirth`, `first_vitality`), helper `FindAchievement`, validation.
  - `Assets/_Project/Scripts/Domain/GameStateFactory.cs`: Giữ nguyên sạch sẽ, để AchievementSystem quản lý khởi tạo entry tự động tương thích ngược.
  - `Assets/_Project/Scripts/Domain/AchievementSystem.cs`: Domain logic theo dõi tiến độ, claim Coin, đảm bảo tính chất idempotent, chống duplicate claim, rollback khi save lỗi.
  - `Assets/_Project/Scripts/Presentation/AchievementView.cs`: UI danh sách 6 achievement, nút mở panel góc trên bên phải, hiển thị tiến độ và nút nhận 10 Coin (vàng/xám), khóa/mở input camera và tap gameplay.
  - `Assets/_Project/Scripts/Presentation/FoundationHudView.cs`: Hiển thị Coin trên HUD và nhận cập nhật từ AchievementSystem.
  - `Assets/_Project/Scripts/Bootstrap/GameBootstrapper.cs`: Wire `AchievementSystem` và `AchievementView`, đăng ký lắng nghe events từ Upgrade, Evolution, Rebirth, Vitality.
  - `Assets/_Project/Scripts/Presentation/WeatherView.cs`: Thay thế `FindFirstObjectByType` bằng `FindAnyObjectByType` để làm sạch console 0 warnings.
  - `Assets/_Project/Editor/AchievementDebug.cs`: Editor debug menu T08A (Log status, Grant progress, Claim all, Open panel).
  - `Assets/_Project/Tests/EditMode/AchievementTests.cs`: 12 unit tests EditMode kiểm tra trọn vẹn toàn bộ acceptance criteria của T08A.
  - `Assets/_Project/Tests/PlayMode/AchievementSceneTests.cs`: 3 PlayMode integration tests kiểm tra UI mở/đóng, tương tác claim, save/load và Rebirth trong MainScene thật.
  - `Assets/_Project/Tests/PlayMode/SaveSceneTests.cs`: Tìm achievement theo ID ổn định thay vì hard-code index [0] theo đúng nguyên tắc AGENTS.md.
  - `Docs/Validation/T08A_20261003.md`: Báo cáo nghiệm thu chính thức.
  - `Docs/Decisions.md`: Quyết định kỹ thuật D20.
  - `Evidence/T08A/test_results.txt`: Log bằng chứng kiểm thử tự động.
- Test: 165/165 EditMode PASS, 50/50 PlayMode PASS (Tổng: 215/215 PASS 100%, 0 fail, 0 skip).
- Console: 0 errors, 0 warnings.
- Blocker: không có.
- Tiếp theo: T08B (Shop và Cosmetic).
