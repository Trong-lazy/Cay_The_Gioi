# Prompt tiếp tục T10 sau khi người dùng undo model

> Cập nhật 06/10/2026: yêu cầu mới là soạn prompt chỉnh thân trên candidate riêng theo D25. Dùng `Docs/Art/Trunk_Only_Agent_Prompts_20261006.md` cho lượt đó. Prompt dưới giữ làm hướng tích hợp baseline; không chạy tự động trong lượt chỉnh thân mới, và không dùng hash lịch sử thay việc kiểm tra nguồn hiện tại.

Ngày 05/10/2026. **Thay thế toàn bộ prompt Model_Continuation_Prompts_20261005 trước undo.** Các nhận xét thẩm mỹ trong review cũ không còn là chỉ thị chỉnh sửa hình học.

## Tiến độ đã xác minh

- T00–T09 DONE, T10 IN_PROGRESS; mọi checklist T10 vẫn chưa nghiệm thu.
- Bonsai_Tree.blend và Bonsai_Tree.fbx đã trở về đúng baseline trước lần sửa: hash trùng hoàn toàn với review độc lập và backup pre_T10.
- Model: Roots, Trunk_Branches, Foliage; 51.332 vertices, 73.888 triangles. Không material/texture hoặc anchor chức năng trong file baseline.
- SourceModels và export Tree_Large/Tree_Young từ lượt sửa trước không còn file trong các thư mục làm việc đã kiểm tra. Có bản sao trạng thái trước undo tại ArtSource/Recovery_20261005/current_e53050a8; không lấy bản đó làm nguồn mặc định.
- Unity .meta cho Bonsai còn tồn tại; log có import. Prefab/config gameplay vẫn dùng primitive. Có .meta hoặc import không đồng nghĩa tích hợp/Play Mode PASS.
- Không đọc được phiên Blender đang mở/unsaved qua MCP; kết luận trên là file đã lưu. Chưa thử trực tiếp Unity trong lượt này.

## Prompt để gửi AI tiếp tục

~~~text
Tiếp tục T10 của project:
C:\Users\Asus\foder game 3D\Cay_The_Gioi

Đọc AGENTS.md, START_HERE.md, CURRENT_TASK.md, Docs/Tasks/10_Art_Pipeline_Blender.md, D23/D24 trong Docs/Decisions.md và Docs/Validation/Post_Undo_Status_20261005.md. T09 DONE; T10 IN_PROGRESS. Chỉ một task hoạt động, giữ Unity 6000.6.1f1/URP/Input System.

Người dùng đã undo thay đổi model. Nguồn gốc cần giữ:
Assets/_Project/Art/Bonsai_Tree.blend
SHA256 C8383937B751CA3564EBF3E3E7F8714675FFBDDDF476D9F9872B554ED588BBF2
Assets/_Project/Art/Bonsai_Tree.fbx
SHA256 52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5

QUY TẮC CHO LƯỢT NÀY:
- Giữ nguyên hình học cây đã undo: thân/rễ/cành/tán, vị trí vertex, topology, UV, normals và tỷ lệ.
- Không rút ngắn rễ, không gom lại/tạo lại tán, không làm thẳng thân, remesh, decimate, sculpt, boolean, weld, generate model lại hoặc “tối ưu” geometry.
- Không chạy các mục chỉnh rễ/tán của prompt trước undo. Nhận xét trong review cũ là đánh giá, không phải yêu cầu sửa mới.
- Không ghi đè hai file baseline hoặc dùng Tree_Large/Tree_Young trong backup của lượt sửa bị undo.
- Ảnh tham chiếu chỉ hướng màu/chất liệu ở bước này; không dùng ảnh để thay silhouette cây đã khôi phục.

MỤC TIÊU LƯỢT NÀY:
Đưa đúng model Cây Lớn hiện có vào Unity với material cơ bản, anchors, camera/collider và kiểm tra gameplay; không redesign model và không cố hoàn tất mọi phần T10 trong một lần.

THỰC HIỆN:
1. Đọc skill Unity nếu dùng Unity MCP; xác định Editor đúng project/MainScene, Console và trạng thái Blender nếu kết nối được. Nếu Blender có thay đổi chưa lưu, không lưu đè baseline. Tạo backup có manifest, hash và ảnh baseline trước làm.
2. Làm trên bản sao/prefab mới. Nếu cần nguồn làm việc ở ArtSource/Models thì copy baseline, chưa di chuyển hoặc ghi đè bản gốc. Không sửa .meta/GUID bằng tay; thao tác asset đã import qua Unity khi di chuyển.
3. Dùng FBX hiện có nếu import tốt, không export lại từ Blender chỉ để đổi phong cách. Gắn vào prefab large_tree hiện có hoặc prefab candidate rồi cập nhật visual bằng Unity, giữ stage ID, prefab GUID và logic. Không nhập Camera/Light Blender vào gameplay.
4. Gán material qua renderer/prefab: Root và Trunk_Branches dùng màu nâu xám tự nhiên, Foliage xanh tự nhiên. Ưu tiên shader URP có sẵn, màu/roughness cơ bản trước. Nếu dùng Bark/Leaf có sẵn thì tạo bản material riêng, không sửa material shared khiến placeholder đổi theo. Chưa thêm texture/shader phức tạp, chưa thay UV/normals để đạt hình ảnh giống hệt ảnh tham chiếu. Foliage không bị mất mặt khi orbit; xử lý bằng cài đặt material nếu cần.
5. Thêm Transform anchor trong prefab, không sửa mesh: Anchor_LightningTarget, Anchor_LeavesCenter, Anchor_TrunkAura và Root anchor. TreeView hiện tính lại Root/Leaves/Trunk từ bounds, nên phải bind thật để WeatherView/CosmeticView nhận đúng anchor; không chỉ tạo object có tên rồi tuyên bố xong. Giới hạn sửa script ở view/binding trực tiếp cần cho T10, có fallback cho hai stage placeholder.
6. Chỉnh visual camera cho tán rộng khoảng 8,21m, sâu 5,70m, cao 4,31m theo Blender baseline. Xác minh Unity dimensions/axis thực tế trước tuning. Nếu cần đổi hướng/pivot, xử lý importer hoặc parent Transform, không viết lại vertex. Không non-uniform scale hoặc tạo lại mesh. Giữ silhouette; chỉ dùng uniform scale ở parent nếu có lý do ghi rõ.
7. Cấu hình collider đơn giản phù hợp gameplay. Kiểm tra SphereCollider hiện bao bounds để tránh khoảng trống lớn nhận tap sai. Không dùng foliage dày làm collider chi tiết, không chỉnh cây để vừa collider. Kiểm tra tap/drag, chặn UI, orbit và mouse/touch.
8. Thử Play Mode model Cây Lớn: các góc camera, Evolution vào Cây Lớn, Rebirth về Mầm, sét/Burning đúng vùng, glow_placeholder, Upgrade, save/load. Giữ hai stage trước bằng placeholder trong lượt này. Dùng save test riêng. Chạy tests liên quan và toàn bộ EditMode/PlayMode theo acceptance T10 khi có điều kiện. Không dùng QA hook làm bằng chứng đã thử raycast chuột thật.
9. Lưu ảnh cùng góc trước/sau và báo cáo thay đổi. Kiểm tra hash hai file baseline vẫn nguyên; nếu làm derivative/export mới thì đối chiếu vertex positions/topology sau quy đổi trục, không chỉ đếm triangles/bounds. Đo hiệu năng/draw calls trong Unity nếu báo chúng, không suy từ mesh count.

GIỚI HẠN:
- Không thay luật gameplay, economy, save schema, stage ID, Coin, Rebirth hoặc package.
- Không xóa bản backup/undo, không sửa Library/Temp/file C# sinh tự động.
- Nếu phát hiện vấn đề chỉ giải quyết được bằng thay geometry, ghi known issue và để nguyên geometry; không tự sửa.
- Không ghi “đã hoàn thiện chân thực/anime” chỉ vì đổi màu material.
- Không tự bắt đầu dựng lại Mầm/Cây Non trong lượt này. Đó là mốc tiếp theo sau khi baseline Cây Lớn đã tích hợp và kiểm chứng ổn.
- Mobile thật tiếp tục DEFERRED.

BÀN GIAO:
Ghi file thay đổi, material, prefab, config visual, anchors/collider, kết quả trực tiếp và known issues ở task T10/Validation/Evidence. T10 còn thiếu ba stage và acceptance thì vẫn chưa DONE. Chưa thử Unity trực tiếp thì không tích acceptance; ghi trạng thái phù hợp AGENTS. Chạy Tools/Docs/update_progress.py cuối phiên.
~~~

## Các mốc còn lại sau lượt trên

1. Kiểm chứng bản Cây Lớn giữ nguyên geometry chạy được với material/anchors/camera.
2. Tạo Mầm và Cây Non riêng, dùng phong cách của cây hiện có, không ghi đè/biến đổi baseline Cây Lớn. Đây là công việc của lượt tiếp theo, không ghép vào prompt triển khai hiện tại.
3. Nghiệm thu đủ ba stage, Evolution, raycast, Weather/Burning/cosmetic, save và hồi quy Windows theo T10. Không mở task sau trước DONE.
