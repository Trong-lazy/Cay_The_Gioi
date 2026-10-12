# Tiến độ sau undo model — 05/10/2026

Scope: kiểm tra đọc-only model đã lưu và project trên đĩa; viết lại prompt. Không sửa model, export, material, prefab, config hoặc gameplay.

## Undo được xác nhận

SHA256 .blend = C8383937B751CA3564EBF3E3E7F8714675FFBDDDF476D9F9872B554ED588BBF2.
SHA256 .fbx = 52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5.

Cả hai trùng bản đã review trước lượt sửa và ArtSource/Backup_20261005/pre_T10. Đọc lại .blend bằng Blender background, disable-autoexec, không save. Evidence/PostUndo_20261005/report.json: 3 mesh, 51.332 vertices, 73.888 triangles, bounds Blender 8,2095×5,7043×4,3068m; không material, không anchor Empty. Hash trước/sau không đổi. Không kiểm chứng state chưa lưu trong Blender đang mở.

## Tiến độ hiện tại

| Phần | Trạng thái thực |
| --- | --- |
| T00–T09 | DONE theo task và nghiệm thu đã lưu; không chạy lại trong phiên này |
| T10 | IN_PROGRESS; checklist chưa tích |
| Model Bonsai gốc | Đã trở về baseline trước sửa, mở file được |
| Material/texture cho model | Chưa có trong baseline |
| Mầm/Cây Non model mới | Chưa có source/export trong các thư mục làm việc kiểm tra |
| Unity import | Có .meta và bản ghi import trong Editor.log; chưa xác nhận live AssetDatabase |
| Prefab gameplay | Vẫn dùng mesh built-in; config stage giữ prefab cũ |
| Anchors/collider/camera cho Bonsai | Chưa tích hợp/kiểm chứng |
| Play Mode/build với model mới | Chưa thử |

Assets/_Project/Art/Models, Assets/_Project/Art/Textures và ArtSource/Models hiện không có file khi kiểm tra. Bản trạng thái trước undo vẫn được giữ trong ArtSource/Recovery_20261005/current_e53050a8, không dùng làm nguồn mặc định.

Unity 6000.6.1f1. Không có Unity/Blender MCP tools trong phiên này. Log không thấy các mẫu error CS/Compilation failed/NullReferenceException/MissingReferenceException tại lúc đọc, nhưng không tương đương xác nhận live Console sạch. Không thay đổi task DONE nào.

## Điều chỉnh bàn giao

Prompt trước undo có yêu cầu chỉnh rễ/tán và đã dẫn đến phạm vi người dùng không muốn giữ. Prompt mới loại bỏ toàn bộ sửa geometry baseline, ưu tiên material và tích hợp Cây Lớn hiện có; Mầm/Cây Non tách thành mốc sau. Giữ cảnh báo phong cách chân thực/anime là mục tiêu mỹ thuật, không phải lý do dựng lại model đã undo.

Prompt mới: Docs/Art/T10_Continue_After_Undo_20261005.md. File Model_Continuation_Prompts_20261005.md cũng được thay bằng nội dung mới; bản cũ lưu trong Docs/Art/Archive để truy vết, không dùng triển khai.
