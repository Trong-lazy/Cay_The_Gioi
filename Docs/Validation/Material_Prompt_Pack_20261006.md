# Kiểm tra bộ prompt material/vân lá/vỏ cây

Task: T10
Date: 2026-10-06
Result: DOCUMENT_REVIEW — chưa triển khai material và chưa nghiệm thu Unity.

## Phạm vi

Người dùng yêu cầu soạn prompt cho agent. Chỉ thêm bộ prompt `Docs/Art/Tree_Materials_Surface_Prompts_20261006.md` và ghi bàn giao/quyết định D35. Không thay .blend/.fbx/texture, không giao việc qua chat hoặc subagent.

## Kiểm tra đã làm

- Đọc START_HERE, AGENTS, con trỏ và phạm vi/checklist T10; T09 đã DONE, T10 IN_PROGRESS. Đọc handoff D34/Validation đồng bộ lá và hướng mỹ thuật bonsai.
- File system có đúng bốn nguồn trong ArtSource/Models và bốn FBX cùng tên trong ArtSource/Exports. Không dựa vào tên `Candidate` để tạo thêm model.
- ProjectVersion.txt ghi Unity 6000.6.1f1; manifest ghi URP 17.6.0. Không sửa phiên bản/package.
- Prompt phân biệt lá card Cổ Thụ và lá có độ dày Cây Lớn; giữ leaf UV, morphology/tán/geometry đã đồng bộ. Không bắt bake nguyên foliage với UV chồng hoặc chạy lại script sinh cây.
- Mỗi bước có scope, nguồn đích, cách kiểm tra, dừng ở bước được giao. Bake/maps/color spaces/UV0 và manifest/remap URP được nêu rõ; không giả rằng FBX mang shader Blender hoàn chỉnh vào game.
- Chi tiết lõm/lồi chỉ bằng normal map là đề xuất, không nhầm với geometry ở silhouette; màu HEX đề xuất là sRGB, không coi là đã chốt. Không yêu cầu texture 4K/8K, custom shader hoặc cài package.
- Bốn source được giữ; Assets/Bonsai_Tree và Unity gameplay không được chỉnh tự động. Không giảm acceptance hoặc đặt DONE sau material.
- Nguồn chính thức liên kết trong bộ prompt: Unity URP Lit, Blender Render Baking và Normal Map node. Một số URL Blender English không truy xuất được bằng trình đọc web; dùng kết quả tài liệu Blender chính thức bản ngôn ngữ khác/staging cho nguyên tắc bake/non-color, không coi tài liệu là nghiệm thu môi trường project.

## Chưa làm

Không chạy audit Blender mới, không mở/sửa model, không tạo ảnh material, bake/texture hoặc thay màu. Không có Console/Unity Play Mode/FBX reimport mới; kết quả D34 vẫn là lần kiểm chứng model trước. Các prompt sẽ cần agent có công cụ thực thi và kiểm tra output thật khi người dùng giao.

Giữ metadata validation T10 trỏ báo cáo asset D34; bổ sung báo cáo phiên soạn này qua bàn giao để không thay thế bằng chứng triển khai trước. Chạy Tools/Docs/update_progress.py để giữ đúng con trỏ.
