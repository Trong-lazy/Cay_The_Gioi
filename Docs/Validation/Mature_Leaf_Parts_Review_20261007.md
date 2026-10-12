# Kiểm tra lá và bộ phận Cây Trưởng Thành

Task: T10. Ngày: 07/10/2026. Kết quả: NEEDS_FIX cho asset mature; prompt sửa đã sẵn sàng, chưa có bản sửa để nghiệm thu.

## Phương pháp và bằng chứng

Đọc START_HERE, CURRENT_TASK, T10, GDD chính, Decisions, registry, review cũ và TreeView thực. T09 DONE; T10 đã IN_PROGRESS từ trước. Không sửa model/FBX/texture hoặc Unity scene.

Blender 5.2.2 LTS background mở 5 blend đã lưu, audit mesh/UV/material; render mới Cây Lớn và Trưởng Thành dưới cùng camera/ánh sáng ở góc trước 3/4 và sau. Import lại cả 3 FBX mature vào scene trắng, kiểm tra mesh/material/UV/counts. Script: `Evidence/Mature_Leaf_Parts_Review_20261007/audit_saved_models.py`. Dữ liệu: `audit_report.json`. Hash tất cả blend/FBX/PNG đầu vào được script kiểm tra trước/sau: `all_inputs_unchanged=true`. Không kiểm tra phiên Blender GUI chưa lưu.

Unity MCP xác nhận đúng project, 6000.6.1f1, MainScene, Play stopped, không compile. Console query error/warning trả 0 entries. Query đọc-only xác nhận scene clean (`dirty=false`), Ground=Cylinder, LeafLeft/LeafRight=Sphere, Stem=Cylinder. Chưa có kiểm chứng art mature trong URP/Play Mode; không suy ra PASS gameplay từ render Blender.

## Kết quả thực tế

| Model | Mesh vùng thực tế | Tris nguồn |
| --- | --- | ---: |
| Mầm | Roots, Trunk_Branches, Foliage, SeedShell | 2.778 |
| Cây Non | Roots, Trunk_Branches, Foliage | 4.574 |
| Cây Lớn | Roots, Trunk_Branches, Foliage | 26.682 |
| Cổ Thụ | Roots, Trunk_Branches, Foliage | 73.888 |
| Trưởng Thành LOD0 | TrunkAndRoots, Branches, Foliage | 16.626 |
| Trưởng Thành LOD1 | cùng cấu trúc, suffix LOD1 | 8.648 |
| Trưởng Thành LOD2 | cùng cấu trúc, suffix LOD2 | 3.514 |

Các FBX mature giữ count/name mesh theo source tương ứng; rễ vẫn gộp thân. Bốn nguồn cũ đã phân vùng, do đó chưa có căn cứ yêu cầu agent tách lại tất cả model.

**R1 — Lá chưa đồng bộ.** Render mới cho thấy mature sáng vàng hơn, phiến phồng dày và góc cạnh hơn chuẩn Cây Lớn. Audit xác nhận mature dùng 2 BaseColor WT riêng nhưng cùng Leaf_Common_Normal. Generator hiện có tạo phiến 6 đỉnh/8 tam giác theo công thức, không tái sử dụng cụm nguồn D34. Chuẩn gần nhất là Foliage của Tree_Large, đã đồng bộ hình học thật với Cổ Thụ theo D34. Sửa phải bao gồm hình học/cụm/UV/material, không chỉ palette.

**R2 — Gộp rễ và thân.** Cả 3 LOD cần tách vùng ở cổ rễ; cành thuộc Trunk. Material RootAccent không chứng minh rễ là mesh riêng. GDD mục 4 mô tả ba bộ phận chức năng, mục 7 dùng ID/anchor riêng khi sét trúng; quy ước mesh là cách triển khai presentation phù hợp yêu cầu người dùng, không phải luật GDD bắt buộc tên object cụ thể. Prompt trước của Codex đã yêu cầu TrunkAndRoots; đây là thiếu sót trong prompt, không quy kết agent vi phạm yêu cầu cũ.

**R3 — Bàn giao nguồn chưa cập nhật.** Registry/con trỏ còn ghi chưa có mature, trong khi đã có blend + 3 FBX. Ghi nhận nguồn chưa nghiệm thu; không gọi còn thiếu file và không gọi asset đã đạt.

**Thông tin khác:** LOD0 đo 16.626 tris, không khớp comment generator ~13.8k; mục tiêu 8k–15k từ prompt là tham khảo, chưa có benchmark mobile. Mầm/Non vẫn có 8/14 mặt diện tích 0, như review trước; không giao sửa các model này trong lượt đồng bộ mature. Các tồn đọng cũ về bark mapping/manifest/tích hợp tiếp tục giữ trong review 06/10, chưa được kiểm tra lại đầy đủ hoặc sửa hôm nay.

## Bàn giao và nghiệm thu tiếp theo

Prompt: `Docs/Art/Mature_Tree_Leaf_Parts_Fix_Prompt_20261007.md`. Khóa nguồn tham chiếu và Assets, sửa riêng mature, đo bề mặt gỗ trước/sau, isolate 3 vùng mọi LOD, so lá cùng điều kiện và reimport FBX. Khi có bản sửa đã lưu, Codex mở lại và kiểm tra độc lập các điều kiện cuối prompt.

Lượt này chỉ hoàn thành kiểm tra đầu vào và soạn prompt. Chưa thực thi sửa/dispatch agent ngoài, chưa nghiệm thu bản sửa, chưa tích acceptance T10 hoặc chạy gameplay test. T10 giữ IN_PROGRESS.
