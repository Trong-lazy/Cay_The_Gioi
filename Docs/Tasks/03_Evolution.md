---
id: T03
title: Evolution và camera
status: DONE
depends_on: ["T02"]
validation: "Docs/Validation/T03_20261002.md"
---

# T03 Evolution và camera

## Mục tiêu

Ba stage có Evolution chủ động, mở cap/unlock đúng và đổi view/camera trong MainScene.

## Thực hiện

- [x] Condition/quote trong Upgrade panel: current/next, chi phí và reason.
- [x] TryEvolve trừ resource/đổi stage một lần, giữ level và tài nguyên còn lại, không trao point.
- [x] TreeView thay placeholder và camera blend theo stage, giữ anchors/collider.
- [x] Khóa command/input cần thiết trong transition; mở lại khi hoàn tất, không khóa vĩnh viễn nếu view lỗi.

## Acceptance

- [x] Có đủ resource vẫn ở stage cũ đến khi bấm; thiếu/DEAD/BUSY không chuyển.
- [x] Mầm → Cây Non → Cây Lớn đúng chi phí, cap và unlock; stage cuối không đi tiếp.
- [x] Level và resource còn lại giữ đúng, point không tăng; nâng tiếp dùng cap mới.
- [x] Cây lớn lên/camera đổi rõ; bấm nhanh không chạy hai transition, không cần scene khác.
- [x] Orbit có thể khóa theo setting và phục hồi sau Evolution; UI click không tap cây.
- [x] Orbit/lock/transition hoạt động với mouse và touch; kéo cảm ứng không nhận tap, thao tác bắt đầu trên UI không xoay cây.

## Nghiệm thu chung

- [x] Compile và tests stage/chi trả/unlock/guard đạt.
- [x] Play Mode view/camera/input đạt, hồi quy T01/T02 và có log.
- [x] Validation PASS/Evidence, metadata validation và bàn giao đủ.
- [x] DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

Ngày02/10/2026: DONE trong Unity Editor; [Validation](../Validation/T03_20261002.md) và Evidence/T03 ghi từng acceptance, log/ảnh thật. Config cuối prototype-2, MainScene. Compile sạch;56/56EditMode,16/16PlayMode đạt, gồm toàn bộ hồi quyT00–T02. Đã Play trực tiếp hai lần Evolution và lần đầu trên config cuối; view/camera/model/collider/UI ngang/dọc/inset đạt. Lỗi view có chủ đích trong fault test được lưu, phiên Play mới Console0error/warning; scene saved/notdirty,đã thoát Play.

File chính: thêm EvolutionSystem, EvolutionTransition, EvolutionPanelView, EvolutionSetup và EvolutionTests/EvolutionSceneTests. Sửa UpgradeSystem busy guard, config/version/tuning, TreeView anchors/collider, OrbitCameraRig pose/lock, UpgradePanelView layout, bootstrap wiring, editor debug refresh và FoundationSceneTests version log. MainScene/config lưu qua Unity; meta do Unity tạo. Cập nhật Technical/DecisionsD15/START_HERE/Validation/Evidence và tổng hợp.

Giữ economy50/25→200/100,cap5/5/0→10/10/5→20/20/10; không point reward, không Save/Weather/Rebirth. Config version2 ghi nhận tuning mới blend0.8s,targetheight0.65/1.1/1.9; giữ distance3.3/6.8/10.5,pitch25/25/28. Chỉ khóa commands/tap/orbit trong blend, không dừng production/Sức sống; setting orbitLocked giữ và có nút thử tối thiểu trong panel.

Cách thử: MainScene→Play→Upgrade→mua Root/Leaves, đủ chi phí thì Evolution. Menu Cây Thế Giới/T03/Debug/Fund Next Evolution giúp thử nhanh, vẫn phải tự bấm Evolution; lặp để lên Cây Lớn. Orbit: Đã khóa/Tự do điều khiển setting; chuột phải hoặc kéo một ngón để xoay, đóng panel để tap cây mới. Thoát Play reset vì chưa save.

Không blocker. Touch/input/safe area mới kiểm tra mô phỏng Editor, chưa điện thoại thật/buildPC/mobile/notch/suspendOS; D12 chưa chốtOS/hướngmàn hình. T04NOT_STARTED là con trỏ tiếp theo; dừng trướcT04.
