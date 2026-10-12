---
id: T08B
title: Shop và cosmetic
status: DONE
depends_on: ["T08A"]
validation: "Docs/Validation/T08B_20261003.md"
---

# T08B Shop và cosmetic

## Mục tiêu

Một cosmetic placeholder có preview/mua/equip, ownership persistent và không tác động gameplay.

## Thực hiện

- [x] Config glow_placeholder/price tạm 20 Coin, view riêng cosmetic và Burning.
- [x] Preview transient, buy Coin/ownership atomic, equip yêu cầu đã sở hữu.
- [x] Đóng/đổi panel, DEAD, Rebirth hoặc load khôi phục equip đã cam kết; preview không lưu.

## Acceptance

- [x] Preview không trừ Coin và không đổi saved equip; đóng trả view cũ.
- [x] Thiếu Coin hoặc chưa sở hữu không buy/equip; đủ mua đúng một lần, double click không trừ lại.
- [x] Mua không tự equip, equip mới là cam kết; ownership/equip giữ qua Rebirth/load.
- [x] DEAD/modal/transition ngắt preview đúng; mọi resource/rate/HP/Protection trước-sau cosmetic giữ đúng.

## Nghiệm thu chung

- [x] Compile/tests purchase/equip/ownership/preview cleanup đạt.
- [x] Play Mode world/UI/save và hồi quy T01–T08A, có log.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

- File thay đổi:
  - `Assets/_Project/Scripts/Domain/PrototypeRules.cs`: Thêm `CosmeticDefinition`, cosmetic mặc định `glow_placeholder` (giá 20 Coin), `FindCosmetic`, validation.
  - `Assets/_Project/Scripts/Domain/CosmeticSystem.cs`: Domain logic xử lý Buy (atomic, trừ coin, thêm ownership, không tự equip, rollback khi save fail), Equip (kiểm tra ownership, commit lưu save), Unequip, truy vấn trạng thái.
  - `Assets/_Project/Scripts/Presentation/CosmeticView.cs`: Visual renderer hiển thị hào quang ngọc bích phát sáng (`glow_placeholder`), hỗ trợ preview tạm thời, tự động hủy preview khi đóng panel, cây chết (`RunDied`), Rebirth hoặc chuyển stage.
  - `Assets/_Project/Scripts/Presentation/ShopView.cs`: Giao diện Shop trong SafeArea, hiển thị số dư Coin, danh sách vật phẩm, nút "Xem trước" (Preview), nút "Mua (20 Coin)", nút "Trang bị" / "Tháo", khóa/mở input khi mở/đóng modal.
  - `Assets/_Project/Scripts/Bootstrap/GameBootstrapper.cs`: Khởi tạo và wire `CosmeticSystem`, `CosmeticView`, `ShopView`.
  - `Assets/_Project/Editor/ShopDebug.cs`: Tiện ích Editor debug menu T08B (Log status, Add 50 coin QA, Open shop panel).
  - `Assets/_Project/Tests/PlayMode/SaveTestScope.cs`: Focus GameView trong `SaveTestScope.Begin()` để đảm bảo nhận input ảo ổn định xuyên suốt các bài test PlayMode.
  - `Assets/_Project/Tests/EditMode/CosmeticTests.cs`: 11 unit tests EditMode kiểm tra trọn vẹn toàn bộ acceptance criteria của T08B.
  - `Assets/_Project/Tests/PlayMode/CosmeticSceneTests.cs`: 4 PlayMode integration tests kiểm tra UI mở/đóng, tương tác preview không trừ coin/không đổi equip lưu, mua và trang bị persist qua reload, và cây chết ngắt preview.
  - `Docs/Validation/T08B_20261003.md`: Báo cáo nghiệm thu chính thức.
  - `Docs/Decisions.md`: Quyết định kỹ thuật D21.
  - `Evidence/T08B/test_results.txt`: Log bằng chứng kiểm thử tự động.
- Test: 176/176 EditMode PASS, 54/54 PlayMode PASS (Tổng: 230/230 PASS 100%, 0 fail, 0 skip).
- Console: 0 errors, 0 warnings.
- Blocker: không có.
- Tiếp theo: T08C (Tutorial, Settings, Audio).
