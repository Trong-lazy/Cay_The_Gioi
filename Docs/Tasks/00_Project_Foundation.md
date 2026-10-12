---
id: T00
title: Nền tảng dự án
status: DONE
depends_on: []
validation: "Docs/Validation/T00_20261002.md"
---

# T00 Nền tảng dự án

## Mục tiêu

Tạo MainScene và nền tảng config/state/bootstrap để task tài nguyên có thể bắt đầu. Nền tảng đã nghiệm thu trong Unity ngày 02 tháng 10 năm 2026; xem bản ghi Validation.

## Đọc và phạm vi

Đọc GDD, Decisions và toàn bộ Technical. Kiểm tra Unity 6000.6.1f1, URP, Input System đang có. Dùng cấu trúc Assets/_Project được đề xuất trong START_HERE; tạo thư mục khi cần. Không làm đầy đủ Upgrade, Save, Weather hoặc Shop trong task này.

## Thực hiện

- [x] Ghi hiện trạng scene/script/Console; xác nhận phiên Unity và package tương thích.
- [x] Tạo MainScene duy nhất cho gameplay, đất/cây placeholder, camera orbit pivot, Canvas/EventSystem.
- [x] Tạo cấu trúc config/state/DTO và GameBootstrapper tối thiểu theo Architecture; có schema version/ID ổn định.
- [x] Khởi tạo run Mầm và VitalityState đúng Prototype_Config; validate config cơ bản.
- [x] Đặt TreeView slot và anchors root/leaves/trunk; model không chứa logic economy.

## Acceptance

- [x] Play Mode mở MainScene thấy cây và UI tạm; Exit/Enter Play không nhân đôi service/subscriber.
- [x] Config lỗi rõ được báo, không tiếp tục với state âm hoặc ID stage thiếu.
- [x] Kiến trúc có nơi cho resource/upgrade/stage/vitality/save; bootstrapper không ôm công thức gameplay.
- [x] Không phải có Blender hoặc asset chi tiết mới vào Play được.

## Nghiệm thu chung

- [x] Unity compile không lỗi; kiểm tra trực tiếp MainScene và ghi kết quả.
- [x] Kiểm tra hiện trạng/template còn dùng không bị phá; giữ scene mẫu nếu chưa cần xóa.
- [x] Có Validation PASS, Evidence phù hợp và metadata validation trỏ bản ghi.
- [x] Ghi bàn giao, đặt DONE rồi tổng hợp Progress/CURRENT_TASK.

## Bàn giao

Nghiệm thu ngày 02 tháng 10 năm 2026. Unity 6000.6.1f1, Windows64, URP PC, Input System; MainScene là build entry duy nhất. Đã tạo Scripts/Domain, Configuration, Bootstrap, Presentation, Infrastructure; asmdef Domain không phụ thuộc UnityEngine; PrototypeConfig.asset, ba prefab cây, vật liệu placeholder và UI khởi đầu. ProjectSetup tạo qua Unity Editor API, giữ asset/scene đã có khi chạy lại. Có config/state/factory, save DTO v1, ID ổn định, validation và TreeView anchors để thay model sau. Có .gitignore/.gitattributes/.editorconfig; không tạo commit/remote hoặc đổi repository cha.

Kiểm tra: 10/10 EditMode, 1/1 PlayMode (ba lần load MainScene), khởi tạo lại idempotent, orbit bằng Input System và khóa orbit; Console cuối 0 error/warning. Ảnh Game View cuối đã kiểm tra tại Evidence/T00/main-scene-playmode-1.png. SampleScene được giữ. Xem Docs/Validation/T00_20261002.md và Docs/Technical/Project_Setup.md để mở lại hoặc chạy setup.

T00 chưa có production, tap nhận thưởng, save file, offline hoặc Rebirth; HUD chỉ đọc state đầu run. Các hệ thống đó thuộc task sau. Unity đã thoát Play Mode, MainScene mở và không dirty. Tiếp theo: T01 Core Resources và Sức sống, hiện NOT_STARTED.
