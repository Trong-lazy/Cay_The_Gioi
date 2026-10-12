> HISTORICAL / KHÔNG DÙNG TRIỂN KHAI. Đã thay bằng prompt sau undo tại Docs/Art/T10_Continue_After_Undo_20261005.md.

# Prompt bàn giao hoàn thiện model Cây Thế Giới

Ngày 05/10/2026. Dành cho AI có công cụ Blender/Unity. Đây là yêu cầu sửa/dựng asset thực, không chỉ sinh ảnh concept. Phiên review hiện tại không sửa model.

## Prompt 1 — Blender và export asset

~~~text
Bạn tiếp tục hoàn thiện model 3D cho project:
C:\Users\Asus\foder game 3D\Cay_The_Gioi

Đọc AGENTS.md, START_HERE.md, CURRENT_TASK.md, Docs/Tasks/10_Art_Pipeline_Blender.md, D23 trong Docs/Decisions.md và Docs/Validation/Model_Independent_Review_20261005.md trước khi sửa. T09 DONE; T10 metadata NOT_STARTED. Đồng bộ task sang IN_PROGRESS trước khi triển khai; một task hoạt động.

Asset: Assets/_Project/Art/Bonsai_Tree.blend và Bonsai_Tree.fbx.
Ảnh dáng cây: ArtSource/References/world_tree_bonsai_user_20261004.png.
Concept brief: Docs/Art/Bonsai_Reference_Prompts.md.
Report đo: Evidence/ModelReview_20261005/blend/report.json và fbx/report.json.

Hiện có một cây gồm Roots, Trunk_Branches, Foliage, 73.888 triangles, 51.332 vertices; bounds khoảng 8,210×5,704×4,307m trong Blender, scale=1, rotation=0. Có UV nhưng không material/texture; chưa có anchors/root hierarchy hoặc ba stage mới. Khung 3D dùng được, không dựng lại từ đầu.

Thực hiện theo thứ tự:
1. Giữ dáng bonsai cổ thụ: thân già uốn, cành ngang, tán rộng nhiều tầng, khoảng hở thấy thân/cành. Gốc ở mặt đất. Theo ảnh tham chiếu, không thêm chậu triển lãm, công trình, mặt cây, floating island hoặc stage mới.
2. Chỉnh rễ quá dài/nhọn và tỏa như nan: taper tự nhiên, tiếp đất, phân nhánh/đan xen vừa phải. Kiểm tra nối rễ/thân ở góc thấp, giữ phần tốt đang có.
3. Hoàn thiện foliage thành cụm lá nhỏ có tán ngang rõ. Tránh silhouette vụn như gai/giấy xé. Nếu dùng cards, kiểm tra normals, hướng hai mặt, UV và material. Không tăng mật độ vô hạn hoặc che kín khoảng trống tầng. Cards có biên mở là bình thường, không hàn toàn bộ thành khối kín.
4. Gán material thật: thân/rễ nâu xám, lá xanh tự nhiên. Tạo vỏ sần bằng chi tiết có chọn lọc, normal/texture nếu cần. Bảo đảm có phương án dùng trong URP, không chỉ shader node Blender hoặc màu viewport. Chân thực hoặc anime bán chân thực phải nhất quán với concept đã chọn; không chibi. Ưu tiên bản đơn giản chạy được trước shader phức tạp. Kiểm tra UV bằng checker và đóng gói texture; UV nằm trong [0,1] chưa chứng minh unwrap hoàn chỉnh.
5. Kiểm tra biên hở Roots/Trunk_Branches, mặt đảo, điểm nối, floating parts và tự giao. Sửa lỗ lộ gây lỗi render, tránh boolean/remesh toàn cây làm mất dáng. Không yêu cầu mọi mesh manifold kín.
6. Làm ba stage cùng loài: seedling thân ngắn/hai lá mầm; young_tree thân/cành/rễ trẻ/tán tầng nhỏ; large_tree dùng cây bonsai hiện có. Không làm Cây Non bằng cách thu nhỏ nguyên cây già. Root pivot chung, apply scale/rotation, mesh/material dễ quản lý.
7. Tổ chức nguồn ở ArtSource/Models, export ở Assets/_Project/Art/Models theo T10. Backup trước sửa. Asset đã import phải di chuyển qua Unity và giữ .meta/GUID; không chỉnh GUID bằng tay. Tránh game runtime phụ thuộc .blend/Blender.
8. Tạo hoặc bàn giao vị trí Anchor_LightningTarget, Anchor_LeavesCenter, Anchor_TrunkAura và root anchor cho Burning. Đây là Transform chức năng, không mesh/chữ trang trí. Unity phải bind thật, không chỉ đặt tên trong Blender.

Bàn giao .blend, FBX phù hợp importer, material/texture, units/axis/scale và render front/back/left/right/top/three-quarter/cận thân-rễ-lá. Ghi triangles thực sau triangulation, material/submesh count, UV, bounds, transforms và known issues. Không suy draw calls hoặc “100% tối ưu mobile” từ số mesh; chưa đo thì ghi chưa đo.

Không đổi gameplay, save schema, resource, stage ID, Coin, Weather/Rebirth. Không tự nâng Unity hoặc thêm package. Giữ Unity 6000.6.1f1, URP/Input System. Nếu chỉ làm Blender, bàn giao sang Unity; không đánh dấu T10 DONE.
~~~

## Prompt 2 — Unity integration và nghiệm thu

~~~text
Tiếp tục T10 tại C:\Users\Asus\foder game 3D\Cay_The_Gioi sau bàn giao Blender. Đọc AGENTS.md, START_HERE.md, CURRENT_TASK.md, T10, D23, Docs/Validation/Model_Independent_Review_20261005.md và bàn giao asset. Đọc skill Unity trước khi dùng Unity MCP. Không coi file hoặc báo cáo PASS là bằng chứng model đã đạt.

Kiểm tra Editor Unity 6000.6.1f1, MainScene, Console và asset thực. Đồng bộ task theo quy tắc một task hoạt động. Giữ package URP/Input System và save người chơi; dùng save test riêng.

Hiện config vẫn dùng prefab primitive. Thực hiện:
1. Import export chuẩn hóa; kiểm tra scale=1, axis, pivot mặt đất, material/texture URP và không cần Blender runtime. Lá/rễ không biến mất ở góc orbit/góc thấp. Dùng shader có sẵn phù hợp, chưa cần shader phức tạp.
2. Gắn model mới vào seedling/young_tree/large_tree prefab, giữ stage ID và logic. Giữ .meta/GUID của prefab. Không đưa Camera/Light Blender vào gameplay, không chỉnh Library/Temp/file C# sinh tự động.
3. Bind anchors từng stage. TreeView.ShowStage đang ghi đè Root/Trunk/Leaves từ bounds; phải nối điểm đã chọn để WeatherView/CosmeticView dùng thật. Đặt tên anchor trong model chưa đủ. Giữ fallback hợp lý, giới hạn sửa presentation phục vụ T10.
4. Cấu hình collider/raycast: kiểm tra SphereCollider hiện phủ bounds, không để vùng trống lớn quanh tán nhận tap sai hoặc click xuyên. Không dùng foliage dày làm collider chi tiết. Giữ tap/drag, chặn UI, mouse/touch dùng chung.
5. Chỉnh cameraDistance/pitch/targetHeight trong PrototypeConfig.visuals cho tán rộng và rễ. Kiểm tra Evolution blend/đổi stage/Rebirth không nhảy pivot, không cắt tán. Chỉ visual config, không đổi economy.
6. Kiểm tra sét, Burning Root/Leaves/Trunk, glow_placeholder đúng thân ở ba stage. Cosmetic không tạo buff/damage.
7. Thử MainScene Play Mode: tap/drag/UI, Upgrade, ba stage, Evolution, voluntary/forced Rebirth, cosmetic preview/equip, save/load/offline và hồi quy bị ảnh hưởng. Chạy EditMode/PlayMode hiện có, kiểm tra Console; build Windows mới theo acceptance và chạy save QA riêng. Lưu ảnh/log thật.
8. Đo triangles, material/submesh, rendering cost/frame time trong Unity với điều kiện rõ. Không suy draw calls từ mesh count; mobile máy thật vẫn DEFERRED.

Ghi Validation/Evidence, file thay đổi, config/build, kết quả/known issues trong task. Chỉ DONE khi đủ checklist T10/AGENTS. Chưa thử Unity trực tiếp thì tối đa WAITING_VALIDATION; bị chặn thì ghi lý do. Chạy Tools/Docs/update_progress.py cuối phiên, không tự mở task sau khi còn thiếu nghiệm thu.
~~~
