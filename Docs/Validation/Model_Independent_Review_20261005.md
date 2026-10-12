# Kiểm tra độc lập model Bonsai ngày 05/10/2026

Kết luận: **model có nền tảng hình dáng phù hợp, nhưng chưa đủ để nghiệm thu asset hoặc T10 trong project**. Không sửa hoặc lưu lại model, FBX, prefab, scene, config hay gameplay trong phiên này.

## Phương pháp và bằng chứng

- Ảnh đính kèm của người dùng là cửa sổ mở file, che phần lớn cây; không dùng ảnh đó để kết luận chất lượng model.
- Đọc .blend thực bằng Blender 5.2.2 LTS background, tắt tự chạy script; import FBX trong một phiên Blender riêng để đối chiếu.
- Render front/back/right/three-quarter bằng mesh và trạng thái material đang lưu. Camera/world/lights kiểm tra chỉ tạo trong bộ nhớ, không ghi vào .blend.
- Script: Evidence/ModelReview_20261005/audit_blender_readonly.py. Report: blend/report.json, fbx/report.json. Log: blend_audit.log, blend_stats.log, fbx_audit.log. Ảnh: blend/front.png, back.png, right.png, perspective.png.
- Hash .blend trước/sau: C8383937B751CA3564EBF3E3E7F8714675FFBDDDF476D9F9872B554ED588BBF2.
- Hash .fbx trước/sau: 52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5.
- Unity MCP không khả dụng trong bộ tool phiên này; không thử live Console, import Unity, Play Mode, raycast, frame time hoặc build mới.

## Dữ liệu đo từ asset hiện tại

| Mesh | Vertices | Faces | Triangles thực | Biên mở |
| --- | ---: | ---: | ---: | ---: |
| Roots | 9.120 | 8.928 | 17.856 | 384 |
| Trunk_Branches | 19.332 | 18.864 | 37.728 | 936 |
| Foliage | 22.880 | 18.304 | 18.304 | 18.304 |
| Tổng | 51.332 | 46.096 | 73.888 | — |

- Blender units METRIC, scale_length=1.0. Ba mesh có location=(0,0,0), rotation=(0,0,0), scale=(1,1,1), không modifier chờ áp dụng. Không loose vertices hoặc mặt có diện tích dưới ngưỡng 1e-10 trong phép kiểm tra này.
- Bounds khoảng 8,210 × 5,704 × 4,307 m theo tọa độ Blender; đáy z≈−0,0193 m. Chưa xác nhận kích thước/orientation sau Unity import.
- FBX reimport có cùng mesh count, triangles và bounds gần trùng; chỉ chứa ba mesh, không camera/light. File Blender có thêm Camera/Light.
- Có UVMap nằm trong [0,1]. Chưa kiểm tra distortion, islands, texel density, padding hoặc atlas.
- **Không có material datablock, không slot material trên ba mesh, không image texture dùng cho asset**, ở cả .blend và .fbx. Màu random trong ảnh viewport cũ không phải bằng chứng material game.
- Không empty/root hierarchy hoặc anchor chức năng trong file. Anchor có thể tạo trong prefab Unity, nhưng model-prefab tích hợp đó hiện chưa có.

## Đánh giá mỹ thuật

- Có thân uốn, rễ tỏa rộng, khoảng trống giữa tầng và chiều sâu đa góc: có thể tiếp tục từ model hiện có.
- Rễ khá dài, đầu nhọn, tỏa như nan; cần chỉnh cho cảm giác bám đất/đan xen như ảnh tham chiếu.
- Thân có biến dạng lớn nhưng render hiện còn mềm, trắng do chưa có vật liệu. Chưa công nhận vỏ sần nâu xám chân thực/anime hoàn thiện.
- Tán có silhouette vụn/răng nhọn từ các mặt tam giác nhỏ. Cần material và cách gom cụm để ra tán ngang nhiều tầng với lá nhỏ tự nhiên; không chỉ tăng mật độ lá.
- Biên mở của Roots/Trunk cần kiểm tra ở điểm nối và góc thấp. Biên mở không tự chứng minh lỗi: giao trong thân có thể chấp nhận; lỗ lộ cần xử lý. Foliage cards không cần manifold kín. Chưa đo tự giao/normals chuyên biệt.

## Mức độ phù hợp với project

| Tiêu chí | Hiện trạng |
| --- | --- |
| Hướng bonsai theo D23 | Nền tảng phù hợp; cần hoàn thiện bề mặt/tán/rễ |
| Material chân thực hoặc anime dùng trong URP | Thiếu |
| Scale/pivot đã apply trong Blender | Nền tảng tốt; chưa xác minh Unity |
| Ba stage cùng loài | Chỉ có một asset Bonsai; chưa có bộ ba model mới |
| Prefab/anchors/collider/camera | Chưa tích hợp model mới |
| Save/gameplay hồi quy với model mới | Chưa thử |
| Hiệu năng Windows/mobile | Chưa đo |

PrototypeConfig vẫn trỏ seedling.prefab, young_tree.prefab, large_tree.prefab hiện có với mesh built-in placeholder. Không có tham chiếu Bonsai trong MainScene/config/prefab. Chưa có .meta cho .blend/.fbx lúc kiểm tra trên đĩa; không thay thế đọc AssetDatabase live.

TreeView.ShowStage đang suy anchor Root/Trunk/Leaves từ bounds, chưa bind anchor mới theo prefab. SphereCollider phủ bounds cần thử lại với tán rộng/khoảng hở. WeatherView/CosmeticView dùng anchor view hiện có; chỉ đặt tên anchor trong Blender chưa đủ.

Các bản ghi Model_Audit_360, Model_Detail_Surface, Model_Foliage_Refined nói PASS cho từng bước trước. Không dùng câu “tối ưu 100% PC/Mobile”, “3 draw calls” hoặc “~90k tris” làm kết quả đo hiện tại. Kiểm tra mới đo 73.888 tris; shader passes, shadow/overdraw, draw calls và hiệu năng phải đo trong Unity. Không sửa các bản ghi cũ trong phiên review này.

## Việc tiếp tục

Prompt bàn giao: Docs/Art/Model_Continuation_Prompts_20261005.md.

1. AI Blender hoàn thiện material, foliage/rễ và kiểm tra geometry; giữ phần tốt, làm ba stage nhất quán.
2. AI Unity import/bind prefab anchors/collider/camera, nghiệm thu T10. Mobile máy thật vẫn DEFERRED.

T10 vẫn NOT_STARTED theo metadata; review không tự cập nhật trạng thái triển khai của công việc bên ngoài. AI tiếp tục đồng bộ task trước khi sửa.
