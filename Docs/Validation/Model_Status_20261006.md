# Kiểm tra tiến độ model và chuẩn bị prompt sửa thân — 06/10/2026

Scope: đọc asset đã lưu, render kiểm tra và soạn prompt. Không sửa .blend/.fbx, gameplay, prefab hoặc config; không triển khai chỉnh thân trong phiên này.

## Kết quả thực tế

- T00–T09 DONE theo metadata; T10 IN_PROGRESS, phụ thuộc T09 DONE, acceptance chưa tích. Không chạy lại nghiệm thu các task trước.
- Unity version trên đĩa: 6000.6.1f1. Không có Unity/Blender MCP tools trong phiên; không có process Unity tại lần kiểm tra. Blender process 24912 có title `(Unsaved) - Blender 5.2.2 LTS`; dữ liệu chưa lưu không được đọc/đồng bộ. Không xác nhận Console, scene live, Play Mode hoặc build mới.
- Đọc source bằng Blender 5.2.2 LTS background, factory-startup, disable-autoexec; không save input. Render scene kiểm tra trong bộ nhớ, ghi PNG ngoài Assets.
- Source .blend SHA256 trước/sau: `18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3`. FBX: `52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5`.
- .blend khác hash lịch sử C838…; đối chiếu với `ArtSource/Backup_20261005/Bonsai_Tree_pre_T10.blend` xác nhận cùng tên ba mesh, vertex/triangle count, vị trí đỉnh, topology, UV, corner normals, matrix_world, material slots và loại modifier. Chưa so mọi thuộc tính scene/file nên không quy kết nguyên nhân hash đổi hay nói toàn bộ file giống nhau.
- 51.332 vertices, 73.888 evaluated triangles: Roots 17.856, Trunk_Branches 37.728, Foliage 18.304. Blender bounds 8,209527 × 5,704260 × 4,306751 m. Metric/scale_length 1. Không material hoặc Empty anchors. Chưa đo hiệu năng/draw calls trong Unity.
- `PrototypeConfig.asset` vẫn trỏ ba prefab cũ; `large_tree.prefab` dùng built-in mesh. Asset Bonsai chưa tích hợp gameplay trên đĩa. Không có source/export ba stage mới trong thư mục làm việc Models được kiểm tra; bản Recovery chưa phải nguồn mặc định.

## Nhận xét và hướng prompt

Ảnh render mới xác nhận bố cục thân uốn, rễ xòe và tán nhiều tầng của baseline. Render clay còn mờ, không có vật liệu; chỉ dùng để đọc hình khối, không xem là hình ảnh game hoàn thiện. Phạm vi mới ưu tiên thân chính vững, thuôn tự nhiên và chuyển tiếp cổ cành; giữ điểm nối rễ, đầu cành và tán. Mesh `Trunk_Branches` chứa cả cành nên phải dùng mask, không coi toàn mesh là vùng sửa tự do.

Prompt: `Docs/Art/Trunk_Only_Agent_Prompts_20261006.md`, ba bước audit → candidate thân → kiểm chứng/export riêng. D25 ghi yêu cầu soạn prompt mới, không ghi nhận việc chỉnh thân đã diễn ra. T10 chưa đủ ba stage/tích hợp/Unity validation nên không DONE.

## Bằng chứng

- `Evidence/Model_Status_20261006/compare_saved_meshes.py` và `comparison.json`: đối chiếu fingerprint giữa source và backup; hai input không bị save.
- `Evidence/Model_Status_20261006/current/report.json`: audit source mới, `source_unchanged=true`.
- `current/front.png`, `back.png`, `right.png`, `perspective.png`: render mới bằng `Evidence/ModelReview_20261005/audit_blender_readonly.py`, không sửa script cũ.
- Lần render đầu dùng output relative bị Blender đổi cwd sau open_mainfile nên PNG nằm tại `C:/Evidence/Model_Status_20261006/current/`; đã chạy lại với absolute paths để bằng chứng chính nằm đúng project. Chưa xóa bộ ảnh phụ.

Không có kết quả nghiệm thu Unity trong phiên; không tích acceptance T10.
