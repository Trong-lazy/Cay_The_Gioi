---
id: T02
title: Hệ thống Upgrade
status: DONE
depends_on: ["T01"]
validation: "Docs/Validation/T02_20261002.md"
---

# T02 Hệ thống Upgrade

## Mục tiêu

Ba nhánh Root/Leaves/Trunk có quote/cost/output, level/cap và thao tác nâng cấp một lần hợp lệ.

## Đọc và phạm vi

Đọc GDD mục 4–5, stage/economy config và Architecture. Trunk khóa ở Mầm. Kiểm tra unlock bằng stage giả qua debug, chưa thực hiện Evolution trong task này.

## Thực hiện

- [x] Quote và TryUpgrade kiểm tra alive/unlock/cap/cost thống nhất; cost tăng theo level.
- [x] Trừ Water và tăng một level cùng giao dịch; thất bại không đổi state/event.
- [x] UI Root/Leaves/Trunk hiển thị cost/output/level/cap/reason đúng.
- [x] Resource/rate HUD cập nhật sau nâng cấp; Sức sống áp một lần cho level mới.

## Acceptance

- [x] Đủ 5 Water ở Root L0 nâng thành L1, trừ đúng 5; thiếu không thay level hoặc số dư.
- [x] Cost theo config tăng đúng, không tính từ số K/M/B đã làm tròn UI.
- [x] Không nâng Trunk tại Mầm, không vượt cap, không nâng khi DEAD; command thất bại có lý do.
- [x] Nhiều click chỉ xử lý từng command hợp lệ, không cho Water âm hoặc double mutation trong một command.
- [x] Upgrade thay rate đúng; Sức sống không tăng Protection khi nâng Trunk.

## Nghiệm thu chung

- [x] Compile/EditMode tests cost/cap/unlock/transaction đạt.
- [x] Play Mode kiểm tra UI, hồi quy production/tap T01 và ghi kết quả.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

09/10/2026: PASS dưới đây là nghiệm thu economy cũ. Phân bổ Water chung bị thay bởi yêu cầu mới D47/T09A; kiểm chứng cho quy tắc mới nằm tại T09A, không suy ra từ checklist lịch sử này.

Ngày 02/10/2026: DONE trong Unity Editor; [Validation](../Validation/T02_20261002.md) và Evidence/T02 ghi đối chiếu acceptance, log thật, ảnh ngang/dọc/inset. EditMode46/46 và PlayMode cuối11/11 PASS, gồm toàn bộ hồi quy T00/T01; Console0error/warning. MainScene saved, không dirty, đã thoát Play.

File chính: thêm Domain/UpgradeSystem, Presentation/UpgradePanelView, Editor/UpgradeSetup và UpgradeTests/UpgradeSceneTests. Sửa PrototypeRules (trunkProtectionPerLevel0.05), ResourceSystem (notify), GameBootstrapper (wiring), SafeAreaHudLayout và CoreResourcesSetup (refresh debug). MainScene/config lưu qua Unity. Giữ economy prototype-1, base5/5/10,factor1.6,caps5/5/0→10/10/5→20/20/10. Sửa clipping Water consumption portrait, kiểm tra lại ảnh và test.

Cách thử: mở MainScene → Play → Upgrade → Root+1 rồi Leaves+1. Tap cây50 lần nhận20s/+5%. Trunk khóa Mầm; dùng menu Cây Thế Giới/T02/Debug/Young Tree State (no Evolution) để thử unlock/Protection; debug chỉ đổi runtime, chưa làm Evolution/model switch. Chưa có save; thoát Play reset.

Không còn blocker T02. Touch/UI mới kiểm tra bằng Input System mô phỏng trong Editor, chưa kiểm tra điện thoại/build PC/mobile; OS/hướng màn hình D12 chưa chốt, không chặn Editor T02. Tiếp theo T03 NOT_STARTED; dừng trước T03.
