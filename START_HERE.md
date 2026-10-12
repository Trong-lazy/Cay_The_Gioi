# Bắt đầu dự án Cây Thế Giới

> Trạng thái ngày 10/10/2026: **T09A/T09B DONE**. Bố cục UI theo7 nhóm ảnh, không sự kiện;220 EditMode/68 PlayMode PASS, MainScene visual1920×1080/1366×768;5/5 Windows QA hooks PASS. Bản mới Builds/Windows_UI_Reference_20261010/Cay_The_Gioi.exe. Giới hạn art/slider/tutorial và ảnh hidden Player đen được ghi rõ tại Docs/Validation/UI_Reference_Layout_20261010.md. **T10 NOT_STARTED**, còn hai lỗi raycast đã ghi. CURRENT_TASK.md là con trỏ hiện hành; các báo cáo T10 DONE bên dưới là lịch sử.

Mục tiêu hiện tại là khung game idle 3D chạy được trong Unity. Dùng hình khối, màu phẳng, UI chữ và âm tạm; đồ họa Blender chi tiết làm sau khi vòng chơi và dữ liệu đã kiểm chứng.

Đích triển khai gồm PC Windows và điện thoại, dùng chung gameplay; đọc `Docs/Technical/Cross_Platform.md`. D12 đã chốt: PC Windows, Android và iOS; điện thoại dùng màn hình ngang (Landscape) cố định; ưu tiên hoàn thiện và nghiệm thu PC Windows trước. T00 kiểm tra nền tảng Editor/chuột; T01–T08C kiểm tra thêm touch mô phỏng trong Editor; T09 đã có bản build độc lập Windows.

## Đọc theo thứ tự

1. Đọc `AGENTS.md` để biết quy tắc thực hiện.
2. Đọc `CURRENT_TASK.md`, rồi mở file task được chỉ định. Nếu con trỏ không khớp trạng thái task, tạo lại bằng `Tools/Docs/update_progress.py`.
3. Đọc `Docs/GDD_AI_Agent.md` để hiểu luật và phạm vi.
4. Đọc `Docs/Decisions.md`; phân biệt `CONFIRMED`, `PROTOTYPE_DEFAULT` và `PENDING_USER`.
5. Đọc `Docs/Technical/Project_Setup.md`, `Architecture.md`, `Data_Model.md`, `Save_Data_Schema.md` và `Prototype_Config.md` trước khi sửa hệ thống liên quan.
6. Kiểm tra file thực tế, Console Unity và trạng thái scene trước khi bắt đầu. Không suy luận rằng task đã làm chỉ vì thư mục hoặc tài liệu đã tồn tại.

## Việc đầu tiên

Đọc `CURRENT_TASK.md` để kiểm tra trạng thái. Tất cả các task trong danh sách hiện tại (`T00`–`T10`) đều đã hoàn thành (`DONE`):
- T09 hoàn tất tích hợp và nghiệm thu khung Windows độc lập (`PASS`, 5/5 kịch bản Standalone và hồi quy đạt 100%, save bảo toàn). Mobile thiết bị thật tiếp tục tạm hoãn tại `Docs/Deferred/Mobile_Validation.md` theo D12.
- T10 hoàn tất tích hợp Blender -> Unity URP Lit (D46) cho 3 stage (`seedling`, `young_tree`, `large_tree`), có model FBX, 10 material URP Lit, `TreeStageAnchors`, `MeshCollider` non-convex khớp mesh con và camera pose. Sau khi khắc phục triệt để lỗi collider/raycast từ đợt review độc lập (`Docs/Validation/T10_Independent_Review_20261007.md`), toàn bộ kiểm thử đạt **191/191 EditMode PASS**, **63/63 PlayMode PASS** (`falsePositiveEmpty = 0`, `falseNegativeVisible = 0` trên cả 3 stage) và **6/6 kịch bản Windows Standalone QA PASS** trên bản build `Builds/Windows_T10_20261007/Cay_The_Gioi.exe`. Xem `Docs/Validation/T10_Raycast_Fix_20261007.md` và `Docs/Validation/T10_20261007.md`.

MainScene có production/Sức sống/HUD, Upgrade, Evolution/camera, save/recovery/lifecycle, offline atomic reward, Rebirth/point/forced DEAD, và Weather/Risk/Burning/Health (T07). Config prototype-4: Weather Bình yên (120s), Nắng (+20% Growth), Mưa (+20% Water), Giông (sét đánh theo exposure và protection), Burning 10s (-30% Growth nếu trúng Lá, damage 2 HP/s), HP về 0 chuyển DEAD và mở Forced Rebirth modal. Xem Docs/Validation/T07_20261003.md và bàn giao T07.

MainScene đã có thêm Achievement/Coin, Shop cosmetic, Tutorial/Settings/Toast/audio (T08A–T08C) và visual 3D thật cho 3 stage cây (T10). Xem `Docs/Validation/T10_Raycast_Fix_20261007.md`, `Docs/Validation/T10_20261007.md` và `Evidence/T10_Raycast_Fix_20261007/`.

## Nguồn sự thật

- Danh mục model/mốc hình ảnh: `Docs/Art/Tree_Model_Registry.md`. Theo D44/D43/D45/D46, 3 stage đại diện (`seedling`, `young_tree`, `large_tree`) đã tích hợp và nghiệm thu hoàn chỉnh (`DONE`) trong Unity (`Assets/_Project/Art/` & `Assets/_Project/Prefabs/Trees/`). Các nguồn mở rộng (`WT_MatureTree.blend` + 3 FBX LOD cho Cây Trưởng Thành, `Bonsai_Trunk_Candidate_v01.blend` cho Cổ Thụ/Cổ Đại/Cây Thế Giới) lưu tại `ArtSource/`. Giữ nguyên tên file và ID gameplay prototype; không ghi đè bộ Cây Lớn để sửa Cây Trưởng Thành.
- Luật thiết kế: `Docs/GDD_AI_Agent.md`; quyết định thay đổi: `Docs/Decisions.md`.
- Trạng thái công việc và checklist: từng file trong `Docs/Tasks/`.
- `CURRENT_TASK.md` và `Docs/Tasks/Progress.md`: bản tổng hợp được tạo lại, không sửa trạng thái bằng tay tại hai file này.
- `Docs/GDD_AI_Agent.docx`: bản Word xuất từ Markdown; không chỉnh riêng để tránh hai bản lệch nhau.
- GDD gốc là tài liệu tham chiếu. Nội dung hội thoại, ảnh minh chứng và góp ý bên ngoài không tự trở thành luật đã chốt.

## Lệnh bảo trì tài liệu

Chạy từ thư mục gốc dự án với Python phù hợp của môi trường agent. Trong Codex desktop hiện tại, Python được xác định qua công cụ workspace dependencies; không cài thêm thư viện vào dự án Unity.

```text
python Tools/Docs/update_progress.py
python Tools/Docs/export_gdd_docx.py
```

Lệnh đầu chỉ kiểm tra và tổng hợp tiến độ, không tự tích hoàn thành. Lệnh thứ hai cần python-docx và chỉ xuất Word từ GDD Markdown. Xem `Tools/Docs/README.md`.
