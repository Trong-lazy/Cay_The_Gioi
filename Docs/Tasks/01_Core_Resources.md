---
id: T01
title: Tài nguyên và Sức sống
status: DONE
depends_on: ["T00"]
validation: "Docs/Validation/T01_20261002.md"
---

# T01 Tài nguyên và Sức sống

## Mục tiêu

Root/Leaves sản xuất Water/Growth theo thời gian, HUD đúng và tap tích quỹ Sức sống +5%. Chưa có UI Upgrade đầy đủ; dùng config/debug Editor để đặt level kiểm thử.

## Đọc và phạm vi

Đọc D02/D03/D11, Data_Model và Prototype_Config. Tap thay click yield Water. Input phải dùng Input System và chặn UI. Chưa làm Evolution, Shop hoặc Weather.

## Thực hiện

- [x] Production Evaluate thuần dữ liệu, Water consumption và Growth theo lượng nước có thực.
- [x] Tick online nhất quán; HUD resource/rate, tap counter và thời gian Sức sống.
- [x] Tap đúng collider cây một lần mỗi thao tác; chỉ ALIVE/input được phép.
- [x] Input mouse/touch dùng chung command; tách tap/kéo camera, chặn UI và touch/mouse mô phỏng trùng theo Cross_Platform.md. HUD không ép tỷ lệ PC.
- [x] 50 tap → 20 giây tạm, reset tapProgress; bank cap 3600, thời gian dư không vượt cap.
- [x] Sức sống +5% đầu ra Water/Growth, không stack; tiêu theo giây online ALIVE và chia tick khi quỹ hết.

## Acceptance

- [x] Với Root=1, Leaves=0, 10 giây neutral từ Water=0 nhận 10 Water; không khác đáng kể theo chia dt.
- [x] Với Root=0, Leaves=1, Water=1, 10 giây neutral nhận 4 Growth và Water=0; không âm hoặc sản xuất quá nước.
- [x] Tap 49 lần chưa thưởng; lần 50 bank +20 và counter=0; lần 100 thêm 20, buff vẫn +5%.
- [x] Ở bank=3590, thưởng 20 chỉ tới 3600; giữ nút/UI không tự nhận tap.
- [x] Click PC và chạm điện thoại nhận đúng một tap; giữ/kéo/đa chạm/hủy pointer không thưởng sai. Kiểm tra mouse và touch mô phỏng trong Editor, ghi rõ chưa là kiểm tra thiết bị thật.
- [x] Root=1/Leaves=0, bank=5, tick 10s nhận 10.25 Water: 5s buff và 5s thường; bank còn 0.
- [x] Buff không tăng Water consumption/Protection/HP/Coin/point; DEAD không sản xuất/tap hoặc tiêu bank.

## Nghiệm thu chung

- [x] Unity compile và EditMode tests cho production/tap/cap/buff/time boundary đạt.
- [x] Play Mode kiểm tra collider/UI/HUD, hồi quy T00 và lưu log thật.
- [x] Có Validation PASS/Evidence và metadata validation; bàn giao rõ.
- [x] Đặt DONE và tổng hợp Progress/CURRENT_TASK.

## Bàn giao

Hoàn tất ngày 02 tháng 10 năm 2026 trên Unity6000.6.1f1, MainScene/config prototype-1. Thêm ProductionSystem/ResourceSystem/VitalitySystem; input adapter mouse/touch dùng chung command, tick online, HUD/rate/counter/quỹ, safe area; bootstrap chỉ nối hệ thống. Tuning input nằm trong PrototypeConfig/Input. Có menu Editor T01/Debug đặt level/state để thử trước T02; run mới vẫn level0/Water10.

Sửa nền tảng trực tiếp: CapsuleCollider của đất Cylinder dẹt che raycast cây; thay riêng bằng BoxCollider khớp mesh và sửa ProjectSetup chống tái tạo lỗi. Không đổi model, package, Unity hoặc URP. File và config thay đổi liệt kê đầy đủ trong Docs/Validation/T01_20261002.md.

Kiểm tra đạt:29/29 EditMode (gồm10 T00),6/6 PlayMode (gồm1 T00 reload ba lần), direct Play acceptance đúng10 Water/4 Growth/10.25 Water/quỹ0-20-40/cap3600; tick frame thực28.9875832s nhận28.9875846 Water. Input mouse/touch mô phỏng, UI/hold/drag/multitouch/cancel/duplicate, orbit lock, pause, HUD ngang/dọc và inset đều kiểm tra. Console cuối0 error/warning; MainScene đã lưu, không dirty, thoát Play. Evidence/T01 giữ cả lỗi lần đầu, diagnostic và kết quả cuối PASS.

Chưa kiểm chứng: điện thoại thật, player build PC/mobile, suspend/resume trên thiết bị thật; chưa triển khai save/offline hoặc T02. Không còn blocker T01. Con trỏ tổng hợp sang T02 rồi dừng.
