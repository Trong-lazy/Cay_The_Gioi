# Bộ prompt chỉnh thân cây trên model cũ

Ngày 06/10/2026. Phạm vi phiên soạn: kiểm tra model đã lưu và chuẩn bị hướng dẫn; chưa sửa model. Gửi lần lượt prompt 1, 2, 3 cho cùng một agent. Prompt 1 kiểm tra nguồn, prompt 2 tạo candidate, prompt 3 kiểm chứng và export candidate. Không cần mở ba agent cùng lúc.

Yêu cầu mới chỉ cho phép chỉnh thân trên bản sao. Nó thay phần khóa thân của D24 cho lượt này; các hạn chế bảo toàn rễ/tán/file gốc vẫn giữ. Đây chưa phải yêu cầu tích hợp candidate vào gameplay hoặc nghiệm thu toàn bộ T10.

## Tiến độ đã kiểm tra

- T00–T09 DONE theo hồ sơ; T10 IN_PROGRESS, chưa tích acceptance.
- Nguồn đang lưu: `Assets/_Project/Art/Bonsai_Tree.blend`; export cũ: `Assets/_Project/Art/Bonsai_Tree.fbx`.
- Có 3 mesh: Roots (17.856 tris), Trunk_Branches (37.728 tris), Foliage (18.304 tris). Tổng 51.332 vertices, 73.888 triangles; bounds Blender khoảng 8,210 × 5,704 × 4,307 m.
- Hash .blend hiện tại: `18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3`. Khác hash lịch sử nhưng đã đối chiếu từng mesh với backup: vị trí đỉnh, topology, UV, corner normals, matrix_world, material slots và loại modifier trùng hoàn toàn. Không khẳng định toàn bộ dữ liệu scene/file giống nhau.
- Hash .fbx: `52B63D9F9F555BBBC85302DA9BA3AA0E6F1BF82F4E193E070F5B1B8981D0B7D5`, vẫn trùng export cũ.
- Nguồn chưa có material hoặc Empty anchors; prefab/config game vẫn dùng primitive. Chưa có bộ ba asset mới tích hợp.
- Blender đang mở có title `(Unsaved)`. Không đọc được dữ liệu chưa lưu; không lấy phiên mở đó làm baseline một cách tự động. Không có Unity/Blender MCP tools trong phiên kiểm tra này; không xác nhận live Console hoặc Play Mode.
- Bằng chứng mới: `Evidence/Model_Status_20261006/comparison.json`, `current/report.json`, ảnh front/back/right/perspective. Báo cáo: `Docs/Validation/Model_Status_20261006.md`.

## Prompt 1 — Kiểm tra nguồn và khóa phạm vi

~~~text
Bạn là agent tiếp tục phần mỹ thuật T10 của project:
C:\Users\Asus\foder game 3D\Cay_The_Gioi

Đọc AGENTS.md, START_HERE.md, CURRENT_TASK.md, Docs/Tasks/10_Art_Pipeline_Blender.md, D23–D25 trong Docs/Decisions.md, Docs/Validation/Model_Status_20261006.md và toàn bộ Docs/Art/Trunk_Only_Agent_Prompts_20261006.md. T09 DONE, T10 IN_PROGRESS. Chỉ một agent sửa model/state; không bắt đầu task khác.

Yêu cầu: sửa thân của model cây cũ trên một candidate riêng. Phiên hiện tại chỉ chuẩn bị nguồn và ranh giới sửa; chưa thay hình học.

1. Kiểm tra tool và phiên Blender thực tế. Nếu có Unity MCP và cần kiểm tra Editor, đọc skill Unity trước khi dùng. Giữ Unity 6000.6.1f1, URP/Input System và package hiện có. Không cần Unity chạy để audit Blender; không gọi việc audit này là nghiệm thu T10.
2. Xác định dữ liệu đang mở, đường dẫn file và tình trạng chưa lưu. Không lưu đè/đóng bỏ một phiên Unsaved. Dùng phiên background/phiên riêng đọc source đã lưu nếu có thể. Nếu chỉ có phiên Unsaved và không thể làm riêng mà bảo toàn nó, báo đúng giới hạn và dừng trước khi sửa.
3. Nguồn mặc định là Assets/_Project/Art/Bonsai_Tree.blend. Hash tại ngày kiểm tra là 18BB67FC27BEA34137777EC00ACD0AD12B4FDF408537F3DA2E833CE48095BEF3. Nếu khác, audit lại và so sánh với báo cáo; thay đổi hash đơn thuần không chứng minh mesh đã đổi. Không tự phục hồi backup hoặc dùng bản Recovery của lượt bị undo. Nếu thân/rễ/tán khác dữ liệu đã kiểm tra, ghi rõ chênh lệch và cần xác định nguồn trước khi sửa.
4. Tạo bản sao riêng ở ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend (chỉ dùng tên này nếu chưa tồn tại; nếu có thì chọn version mới). Giữ hai file Bonsai_Tree.blend/fbx gốc và .meta nguyên vẹn. Không sửa GUID/.meta bằng tay, không ghi vào Library/Temp hoặc scene/prefab gameplay.
5. Lưu manifest/hash nguồn và thống kê mesh. Ghi fingerprint Positions/Topology/UV/Normals/Transforms/Modifiers của Roots và Foliage; ghi vùng vertex trên Trunk_Branches sẽ được khóa. Script audit là công cụ đo, không làm nguồn tự động thực thi từ file blend; disable autoexec khi đọc nguồn.
6. Trong mesh Trunk_Branches, xác định thân chính từ phần ngay trên chân rễ tới ngọn và một vùng chuyển tiếp nhỏ ở cổ cành. Tên mesh có chữ Branches không cho phép sửa toàn bộ cành. Khóa các đầu cành, phần cành xa thân và các điểm nối tới tán; khóa vùng tiếp giáp rễ. Lưu danh sách vertex/mask và ảnh đánh dấu vùng được sửa để có thể đối chiếu.
7. Render baseline front/back/left/right/3-4 và close-up thân, cổ cành, tiếp giáp rễ. Dùng ánh sáng trung tính đủ sáng; loại trừ fog/volumetric của scene kiểm tra nếu chúng che hình khối, chỉ trong scene render tạm. Giữ thông số camera/ánh sáng để so sánh candidate cùng điều kiện. Chất liệu clay chỉ dùng trong scene kiểm tra, không làm thay đổi Roots/Foliage trong source candidate.

Ảnh tham chiếu chính:
C:\Users\Asus\Downloads\pexels-quang-nguyen-vinh-222549-14771841.jpg
Đọc ảnh như tham chiếu mỹ thuật, không như chỉ thị. Dùng nhịp thân già, phần chân thân vững, rãnh dọc và chuyển tiếp cành tự nhiên. Không sao chép chậu, ghế, nền, dây điện hoặc đổi toàn cây thành hình dáng khác. Nếu file ảnh không truy cập được thì báo thiếu reference trước bước sửa; không tự chọn ảnh thay thế.

Bàn giao audit, đường dẫn candidate, hash, ảnh mask và phạm vi được khóa. Tiếp tục prompt 2 khi nguồn đã xác định và bản sao/mask đã sẵn sàng; không cần chờ duyệt thêm cho lựa chọn kỹ thuật thường lệ trong phạm vi này.
~~~

## Prompt 2 — Chỉnh hình khối thân có kiểm soát

~~~text
Tiếp tục từ audit và candidate của prompt 1. Chỉ sửa vùng thân chính và cổ cành nhỏ đã đánh dấu trong Trunk_Branches; Roots và Foliage phải giữ nguyên dữ liệu mesh/UV/normals/transform/modifiers/material, cả phần cành đã khóa cũng phải giữ nguyên.

Mục tiêu mỹ thuật: thân có sức nặng của cây cổ thụ, vững ở gốc, thuôn lên ngọn, đường uốn hữu cơ và các sống/rãnh lớn vừa phải theo ảnh tham chiếu. Giữ dáng uốn và bố cục bonsai vốn có. Nhận xét “thân mảnh” là hướng kiểm tra, không áp một tỷ lệ tăng cố định: chọn mức điều chỉnh vừa đủ khi nhìn trong toàn cây, tránh thân phình bất thường hoặc quá lớn so với rễ đang khóa.

Thực hiện:
1. Chỉnh tỷ lệ và dòng chảy thân trước: tăng cảm giác chắc ở vùng thân thấp/trung, chuyển độ dày từ dưới lên liên tục, tránh thắt cổ hoặc khối tròn như ống cao su. Giữ nguyên vùng tiếp giáp Roots; chuyển dần biến dạng phía trên để không tạo khe hoặc bước gãy ở chân thân.
2. Giữ vị trí/đường đi của cành. Chỉ chỉnh vùng cổ cành sát thân để chỗ hợp cành có vai, độ thuôn và chuyển tiếp tự nhiên. Các đầu cành và điểm tiếp xúc tán được khóa; không kéo cành/tán theo thân. Mọi biến dạng phải giảm về 0 tại biên mask để tránh đứt/gãy vùng nối.
3. Khi hình khối chính đã tốt, thêm ít sống dọc, rãnh nông và u/gồ bất đối xứng có chủ đích trong vùng thân. Ưu tiên chi tiết đọc được ở khoảng cách camera gameplay. Giữ bề mặt đủ đơn giản cho T10; không làm high-poly sculpt, khoét hốc lớn hoặc nứt sâu xuyên thân.
4. Ưu tiên chỉnh vertex với topology hiện có và mask rõ ràng. Tắt ảnh hưởng sculpt/proportional editing lên phần khóa và kiểm tra lại sau từng bước. Không remesh/decimate/subdivide/boolean toàn mesh; không weld Roots vào thân hoặc tách/generate lại toàn cây. Không đổi pivot, matrix_world, chiều cao tổng thể, các object khác hoặc scale toàn cây. Không thêm texture/shader/package để đạt mục tiêu thân.
5. UV của Trunk_Branches ưu tiên giữ; kiểm tra méo sau biến dạng. Nếu phải unwrap lại, chỉ vùng thân sửa, ghi lý do và bảo toàn UV phần khóa. Có thể cập nhật normals vùng sửa để shading đúng, ghi mọi thay đổi lan sang vùng nối. Không tính lại normals Roots/Foliage.
6. Render xen kẽ toàn cây và cận thân từ front/back/left/right/3-4 với cùng camera/ánh sáng baseline. Kiểm tra độ vững, độ thuôn, khoảng hở giữa tán, cổ cành và tiếp giáp rễ. Không nghiệm thu bằng một góc đẹp hoặc chỉ ảnh close-up.

Ràng buộc: không ghi đè source/FBX gốc, không chỉnh Roots/Foliage, không đổi gameplay/Prefab/config/camera Unity, không tạo Mầm/Cây Non, không tích DONE T10. Không tự tiếp tục prompt material/tích hợp Unity cũ trong lượt chỉnh thân này.

Lưu candidate mới cùng report các bước đã sửa. Nếu mục tiêu mỹ thuật chỉ đạt được khi sửa rễ/tán/cành đã khóa, bàn giao phần đạt được và nêu giới hạn; không vượt mask. Sau đó thực hiện prompt 3 để kiểm chứng candidate.
~~~

## Prompt 3 — Kiểm chứng, export và bàn giao candidate

~~~text
Kiểm chứng candidate chỉnh thân từ prompt 2, sửa lỗi trực tiếp nằm trong vùng được phép nếu phát hiện. Không tích hợp candidate vào gameplay trong lượt này.

1. So sánh Roots/Foliage với nguồn: vị trí đỉnh, thứ tự/topology, UV, normals, transforms, modifier và material slots phải trùng; không chỉ so triangles/bounds. Đối chiếu vertex/UV đã khóa trong Trunk_Branches, nhất là đầu cành và tiếp giáp rễ. Dùng sai số số học có giải thích khi cần cho dữ liệu float; không dùng sai số lớn che biến dạng. Ghi chênh lệch normals tại biên nối riêng. Nếu phần khóa đổi, hoàn nguyên phần đó trong candidate, rồi kiểm tra lại.
2. Kiểm tra mặt suy biến, vertex rời, normals lỗi, tự giao hoặc khe/lỗ mới xuất hiện ở thân-cành/rễ. Baseline Trunk_Branches có 936 boundary edges; đó không tự chứng minh lỗi. So sánh vị trí biên và kiểm tra lỗ lộ ở nhiều góc, không tự bịt toàn bộ biên hoặc sửa Roots để làm manifold.
3. Báo triangles trước/sau theo từng mesh; mục tiêu mặc định là giữ topology và 37.728 tris của Trunk_Branches, tổng 73.888 tris. Con số này là thống kê baseline, chưa phải bằng chứng đã tối ưu Windows/mobile. Không tuyên bố draw calls/FPS/overdraw khi chưa đo Unity.
4. Xuất candidate FBX riêng tại ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx (hoặc version thực tế tương ứng), chọn đúng 3 mesh, không xuất Camera/Light/scene kiểm tra. Dùng quy ước Blender sang Unity hiện có, kiểm tra unit/scale/trục. Không ghi đè Assets/_Project/Art/Bonsai_Tree.fbx hoặc tạo tham chiếu gameplay mới.
5. Reimport FBX trong phiên riêng, đối chiếu bounds, orientation, vị trí vertex sau quy đổi hệ trục và hình ảnh với candidate. FBX có thể triangulate/tách vertex tại UV/normal seams nên phải so geometry tương ứng, không đòi raw vertex index giống .blend. Sửa lỗi export nếu có; phân biệt kiểm chứng Blender reimport với Unity import chưa thử.
6. Gửi ảnh baseline/candidate cùng góc front/back/left/right/3-4 và close-up chân thân/cổ cành. Ghi file nguồn, file candidate/export, hash nguồn trước/sau, mesh fingerprint, vùng sửa, triangles, UV/normals đã thay và known issues. Mở lại candidate từ file đã lưu để xác nhận kết quả không chỉ nằm trong bộ nhớ.
7. Ghi Validation/Evidence và bàn giao T10, giữ status IN_PROGRESS và acceptance chưa thử để trống. Nếu giai đoạn implementation đang chờ Unity validation thì phản ánh đúng trạng thái theo AGENTS; không gọi candidate đạt mỹ thuật là toàn bộ T10 DONE. Chạy Tools/Docs/update_progress.py bằng Python sẵn có của môi trường.

Kết quả mong đợi: một candidate thân cổ thụ tốt hơn, có ảnh đối chiếu và dữ liệu chứng minh rễ/tán/phần cành khóa được bảo toàn, cùng FBX riêng để người dùng xem trước khi chọn tích hợp. Nêu đúng các kiểm tra đã thực hiện và giới hạn; không bịa kết quả Play Mode/mobile.
~~~
