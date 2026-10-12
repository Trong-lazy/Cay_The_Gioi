# Kiểm tra sơ bộ toàn project ngày 04/10/2026

Phạm vi: kiểm tra source, scene/prefab/config trên đĩa, log Editor và chạy lại build Windows hiện có. Đây là kiểm tra tiến độ, không triển khai T10 hoặc thay đổi trạng thái task.

## Kết luận

Khung gameplay Windows đã được tích hợp và chạy được. T00–T09 DONE trong bảng task; T10 NOT_STARTED phù hợp với asset hiện có. Chưa hoàn thiện đồ họa, playtest người thật hoặc nghiệm thu mobile.

## Kiểm tra project thực tế

- Unity 6000.6.1f1; URP 17.6.0 và Input System 1.20.0 trong manifest. Có phiên Editor mở đúng project; không có Unity MCP tool/resource khả dụng trong phiên kiểm tra này.
- Có 86 file C# trong Assets/_Project, gồm 17 file domain. Domain assembly đặt noEngineReferences=true; bootstrap nối các hệ thống production/vitality/upgrade/evolution/save/offline/rebirth/weather/health/achievement/cosmetic/tutorial/settings/audio.
- MainScene là scene duy nhất được bật trong build settings. SampleScene còn trong Assets/Scenes nhưng không được đưa vào build.
- MainScene có tham chiếu bootstrap/config/tree/camera/HUD. Quét 8 asset scene/prefab/config/material của Assets/_Project, đối chiếu GUID với Assets/Packages/PackageCache: không phát hiện GUID ngoài built-in bị thiếu. Kiểm tra này không thay thế AssetDatabase hoặc kiểm tra live Editor.
- Config prototype-4 nối 3 prefab seedling/young_tree/large_tree. Cả 11 mesh của 3 prefab là built-in placeholder. Không tìm thấy .blend/.fbx/.glb/.gltf trong Assets và ArtSource. ArtSource/Blender hiện chỉ có README.
- Input code có mouse/touch, chặn UI, phân biệt tap/drag; save có autosave, pause/focus và offline catch-up. Audio được sinh tạm bằng code.
- Build Windows có executable, UnityPlayer.dll, data và runtime/domain DLL; tổng thư mục 101.88 MB. Runtime DLL được ghi sau lần sửa source C# mới nhất. Không build lại trong phiên này.
- Logs/Editor.log không có các mẫu error CS, Compilation failed, NullReferenceException hoặc MissingReferenceException tại lúc đọc; có lỗi kết nối dịch vụ Unity cloud. Không đọc trực tiếp Console và không chạy lại EditMode/PlayMode. XML nghiệm thu cũ thực sự ghi 190/190 EditMode và 59/59 PlayMode Passed.

## Chạy lại Windows Standalone

Evidence chính: Evidence/ProjectAudit_20261004/focused_da9eb598/summary.json, 5 report JSON, 5 Player.log và 16 ảnh PNG. Chạy cửa sổ 1280×720, save QA riêng.

| Kịch bản | Kết quả mới |
| --- | --- |
| smoke_fresh | PASS, ExitCode 0; tutorial, vitality, Root/Leaves và lưu đĩa |
| smoke_offline | PASS, ExitCode 0; 3600s offline, +1350 Water/+1800 Growth |
| smoke_progression | PASS, ExitCode 0; 3 stage, claim, cosmetic và voluntary Rebirth |
| smoke_death_forced_rebirth | PASS, ExitCode 0; DEAD, modal bắt buộc, reset HP=100 và +1 point |
| ui_flow | PASS, ExitCode 0; Settings/Upgrade/Evolution/Achievement/Shop/Rebirth/Save |

Hash các file save thật trước/sau không đổi. QA runner kích hoạt Button bằng uGUI ExecuteEvents; tap/tài nguyên/kill có QA hook theo report. Không coi đây là thử chuột OS, kiểm tra raycast thủ công, natural pacing hoặc playtest người thật.

Lượt đầu chạy cửa sổ ẩn tại Evidence/ProjectAudit_20261004/run_f3f1bfaf/ thất bại vì mất focus, SaveController vào SUSPENDED; giữ nguyên report/log để truy vết. Chạy lại với cửa sổ hiện đạt 5/5. Không xác định lỗi gameplay từ điều kiện chạy ẩn này.

Đã xem ảnh ui_flow_05_large_tree: mô hình primitive và UI chức năng còn thô; tại 1280×720 thanh nút trên/toast che một phần đầu panel Upgrade, hướng dẫn che phần chú thích dưới. Đây là điểm cần rà soát layout, chưa sửa và chưa kết luận mọi tỷ lệ màn hình đạt.

## Còn lại

- T10: pipeline Blender, model low-poly 3 stage, anchor/collider/camera và hồi quy sau tích hợp.
- Rà soát chồng lớp UI, thay asset UI/VFX/audio tạm và tuning dựa trên người chơi thật.
- Android/iOS máy thật vẫn DEFERRED theo yêu cầu ngày 04/10/2026.
- Compile/Console/Play Mode live chưa được tái nghiệm thu trong phiên kiểm tra tiến độ này.
