# Bộ prompt material, vân lá và chi tiết vỏ cây

Ghi chú D42 ngày 06/10/2026: model nguồn `Tree_Large` trong bộ prompt này nay đại diện **Cây Trưởng Thành**. Nhãn mỹ thuật “Lớn” của nguồn đó là lịch sử; giữ nguyên tên source/FBX/texture và không đổi stage gameplay `large_tree`. Nguồn Cây Lớn mới còn thiếu, không ghi đè nguồn Trưởng Thành để tạo nó. Xem `Docs/Art/Tree_Model_Registry.md`; ghi chú này không giao chạy lại các prompt.

Ngày 06/10/2026. Đây là bộ lệnh để người dùng giao cho agent thực hiện sau; phiên soạn tài liệu không sửa model, material, texture hoặc Unity. Chạy tuần tự bằng cùng một agent, không giao nhiều agent sửa chung file. T10 vẫn IN_PROGRESS.

## Hướng mỹ thuật đề xuất

Cây tự nhiên bán chân thực, màu dịu, cùng một loài qua các mốc. Giữ hình khối đã duyệt. Mầm/Non xanh non hơn; Lớn xanh trưởng thành, gỗ nâu ấm; Cổ Thụ xanh trầm, gỗ nâu xám. Không bóng nhựa, neon, lá kim loại hoặc phát sáng. Đây là đề xuất, không phải bảng màu người dùng đã chốt; nếu có ảnh phối màu đã duyệt, ưu tiên ảnh đó, không tự đổi hình cây theo ảnh.

Chi tiết lồi lõm được đề xuất là **lồi lõm thị giác bằng tangent-space normal map**, không đục/rút silhouette hoặc thêm hàng vạn đỉnh. Normal map không tạo khối nhô thật ở đường viền. Nếu cần hốc/u lớn thật làm đổi dáng, phải xin yêu cầu riêng; không tự thực hiện trong bộ này.

| Mốc | Lá nền | Lá sáng | Lá trầm | Thân nền |
| --- | --- | --- | --- | --- |
| Mầm | #9BCB55 | #BDD979 | #6F9F46 | #8BB75A, thân còn xanh |
| Non | #75AA43 | #A2C963 | #426F36 | #A18153, nâu sáng |
| Lớn | #5F963E | #96BB55 | #355F32 | #8A6848, nâu ấm |
| Cổ Thụ | #507C38 | #87A84D | #2F5231 | #70634F, nâu xám |

HEX là **sRGB**, không gán thẳng byte/255 vào socket cần linear. Các giá trị roughness/normal strength trong prompt là điểm khởi đầu nghệ thuật, cần kiểm tra ánh sáng thực, không phải luật gameplay hoặc đơn vị độ sâu vật lý.

## Cách dùng

Dán khối “Quy tắc chung” một lần rồi gửi lần lượt P01 → P05. Mỗi prompt có nhiệm vụ và tiêu chí riêng. Nếu dùng chat agent mới cho một bước, dán lại quy tắc chung, prompt cần chạy và báo cáo bước trước. Agent có Blender/file access mới thực hiện được; chat chỉ tạo ảnh không sửa được .blend.

P01 làm màu cơ bản; P02 làm lá; P03 và P04 làm vỏ hai tuổi; P05 bake/export và kiểm tra. Không chạy lại các script dựng cây hoặc script đồng bộ lá D34 để tạo lại geometry. Texture cần các file ảnh dùng chung; điều này không phải tạo thêm model .blend mới.

## Quy tắc chung — dán trước mọi bước

```text
Bạn là agent Blender phụ trách material và chi tiết bề mặt cho project:
C:/Users/Asus/foder game 3D/Cay_The_Gioi

Đọc AGENTS.md, START_HERE.md, CURRENT_TASK.md, task T10, Docs/Decisions.md (D26, D34 và phạm vi bộ prompt này), Docs/Validation/Leaf_Sync_Direct_20261006.md và Docs/Art/Tree_Materials_Surface_Prompts_20261006.md. Giữ Unity 6000.6.1f1, URP 17.6.0 và Input System hiện có. Chỉ làm bước được giao, không tự chạy bước kế tiếp hoặc tích hợp gameplay.

Nguồn phải sửa trực tiếp, không Save As sang model mới:
- Mầm: ArtSource/Models/Tree_Seedling_Simple_v01.blend
- Non: ArtSource/Models/Tree_Young_Simple_v01.blend
- Lớn: ArtSource/Models/Tree_Large.blend
- Cổ Thụ: ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend

Tên Candidate của Cổ Thụ là tên nguồn hiện có, không phải yêu cầu tạo candidate mới. Cho phép sửa material của nguồn Cổ Thụ khi đến bước tương ứng; không đổi hình học. Assets/_Project/Art/Bonsai_Tree.blend và các asset gameplay trong Assets giữ nguyên trong lượt chuẩn bị art này. Không tự đồng bộ bản nguồn sang Assets hoặc dựng thêm stage Cổ Thụ trong gameplay.

Khóa vertex positions, topology, corner normals, silhouette, mật độ/vị trí lá, số cụm tán, thân/cành/rễ, pivot, unit và transforms. Không remesh, subdivision, decimate, sculpt hoặc displacement geometry; không tạo thêm đất/chậu/cỏ/rêu/hoa/quả, gió, rig, VFX hoặc shader riêng. Chỉ đổi material, texture và các UV/material assignments thực sự cần cho bề mặt.

Audit dữ liệu đang lưu và Unsaved trước khi làm. Không ghi đè hoặc đóng bỏ thay đổi chưa lưu của người dùng. Nếu GUI không thể kiểm tra, nói rõ chỉ kiểm tra saved file, dùng background riêng và so hash lại trước khi lưu để tránh đè file vừa đổi. Lưu fingerprint geometry trước/sau; hash cả file sẽ đổi hợp lệ khi sửa material.

Giữ UV lá đã đồng bộ giữa Lớn/Cổ Thụ; không tạo lại lá. Nếu UV thân chưa dùng được, được unwrap thân/cành trên đúng mesh hiện có, không đổi vertices/normals; bảo toàn UV cũ ở layer lưu và ghi rõ layer dùng để xuất. Unity URP Lit dùng UV0 cho base/normal: bảo đảm UV đúng nằm ở kênh này trong export, không chỉ node Blender trỏ tới UV phụ. Nếu leaf UV thật không đạt giả định, audit và báo sửa UV nhỏ cần thiết, không tự dựng lại mesh.

Sửa/tái sử dụng material hiện có theo vai trò, tránh .001/.002 dư. Mục tiêu 1 material lá và 1 material gỗ mỗi cây, cộng material vỏ hạt nếu mesh đó đã tồn tại. Không 1 material/chiếc lá. Không sửa material placeholder/shared gameplay. Texture lưu ổn định trong ArtSource/Textures/Tree_Common/, tên rõ và dùng lại; không tải bộ texture không rõ quyền sử dụng hoặc cài package.

Metallic=0, emission=0; lá/thân matte. Procedural nodes được phép để tác giả tạo chi tiết nhưng đầu ra game phải có texture image. Không khẳng định FBX tự chuyển toàn bộ shader Blender sang URP. Không dùng camera-facing mapping, toon-only shader, shader SSS/translucency tùy biến hoặc displacement cần engine riêng.

Chỉ lưu vào đúng file nguồn khi đã kiểm tra preview. Không tạo bản .blend1/candidate/v02 dư; nếu cần cơ chế khôi phục riêng, hỏi trước, không xóa backup hiện có. Cuối bước mở lại file, kiểm tra texture paths, geometry, lưu Evidence/Validation và bàn giao T10. Không tích nghiệm thu Unity chưa thử, không đánh dấu T10 DONE. Chạy Tools/Docs/update_progress.py. Nếu công cụ không làm được phần nào, báo đúng phần đó.
```

## P01 — Audit và tạo màu cơ bản cho bốn mốc

```text
Thực hiện P01 của bộ Tree_Materials_Surface_Prompts_20261006, áp dụng quy tắc chung. Chỉ audit và tạo material màu cơ bản trên bốn nguồn Mầm, Non, Lớn, Cổ Thụ; chưa thêm vân lá/rãnh thân hoặc xuất đè FBX.

1. Audit đủ objects, material slots/nodes, UV layers và UV coverage, hai mặt lá, geometry fingerprints, mesh/evaluated tris, units/pivot/transforms. Nguồn Lớn sau D34 dự kiến 26.682 tris, Cổ Thụ 73.888, Mầm 2.778, Non 4.574; đây là mốc đối chiếu, không được ép số bằng cách chạy script dựng lại. Nếu khác, kiểm tra nguồn mới nhất trước.
2. Dùng palette đề xuất trong tài liệu, hoặc ảnh màu đã được người dùng chốt nếu có. Giữ cùng họ xanh qua các mốc, không đổi loài lá hoặc thêm màu mùa thu. Mầm thân xanh mềm, Non nâu sáng, Lớn nâu ấm, Cổ Thụ nâu xám già. Rễ chuyển màu tự nhiên theo thân; Mầm có thể dùng kem đất #C9AE82 và vỏ hạt #765038, Non rễ nâu nhạt #A98B62. Không giả vết nứt già trên cây non.
3. Tạo/update Principled BSDF material. Lá roughness ban đầu 0.65–0.80; Mầm có thể 0.60–0.70; thân gỗ 0.80–0.92; rễ/vỏ hạt 0.85–0.95; Metallic 0, emission 0. Màu HEX sRGB phải được chuyển đúng sang linear khi gán bằng script. Chưa random màu từng leaf bằng Object Info vì không coi đó là đầu ra game đã bake.
4. Không thay vị trí đỉnh, topology, corner normals, transforms hoặc UV ở bước này. Có thể đổi material slots/face assignments để các bộ phận nhận đúng màu nhưng không tăng draw slots vô lý.
5. Render cả bốn cây với cùng world, exposure và ánh sáng trung tính, cùng khoảng cách quan sát theo kích thước mỗi cây; không đổi đèn để che màu quá bão hòa. Cây phải đọc rõ khi thu nhỏ về khoảng 300 px chiều cao, không bóng như nhựa. Xem front, back, left, right và 3/4; ghi bảng màu/roughness cuối cùng và fingerprint bảo toàn geometry.

Lưu đúng bốn nguồn; báo cáo material thực có trong file đã mở lại. Gửi preview và known issues. Dừng sau P01, chưa tự làm rãnh, bake hoặc Unity.
```

## P02 — Vân lá dùng chung, không phá kết quả đồng bộ lá

```text
Thực hiện P02 sau P01 đã kiểm tra, áp dụng quy tắc chung. Tạo vân lá cho các model hiện có, lấy hình lá/cụm Cổ Thụ và kết quả D34 làm chuẩn. Không tạo, thay hoặc di chuyển leaf geometry; không đổi silhouette/tán.

1. Tạo một họ texture lá dùng chung. Ưu tiên bộ Leaf_Common_BaseColor.png và Leaf_Common_Normal.png 512×512; màu nền của map nên trung tính/tintable để material của từng mốc tạo được palette P01, không nhân hai lớp xanh khiến lá tối. Roughness dùng scalar ban đầu, không cần thêm map chỉ để tăng số file.
2. Một gân giữa thon chạy từ cuống đến đầu lá, 3–5 cặp gân phụ xiên về mép, nhánh gân mềm, hơi bất đối xứng; gân mảnh, không biến thành xương cá trắng hoặc đường nứt. Vùng gần gân có chuyển sắc nhẹ, bề mặt hơi sần mịn. Không đổi viền lá bằng alpha, không thêm gai/lỗ/thùy mới.
3. Kiểm tra UV thực: Lớn/Cổ Thụ dự kiến cuống (0.5,0), chóp (0.5,1), mép trái/phải (0,0.5)/(1,0.5). Gân phải thẳng theo chiều cuống–chóp, không quay ngang. Các bản lá có UV chồng để reuse: tạo texture từ một mẫu lá chuẩn, không bake cả hàng nghìn lá chồng UV khiến chúng ghi đè nhau. Normal map là tangent-space; texture màu sRGB, normal Non-Color.
4. Lá Lớn/Cổ Thụ dùng cùng texture/gân và cùng quy tắc shader; khác tint nhẹ theo tuổi, không một bên trơn và một bên có loài lá khác. Mầm/Non dùng gân yếu hơn: mầm chỉ gân giữa và gợi vài gân phụ; giữ đúng hình hai lá mầm hiện có. Dùng UV/mask hoặc mức normal phù hợp, không dựng lá trưởng thành thay lá mầm.
5. Normal intensity bắt đầu thấp, khoảng 0.10–0.25 theo thang shader; chỉ tăng nếu preview thật chưa đọc được. Không dùng trị số này như độ sâu mm. Không thêm vein geometry. Mặt dưới/mép cần hiển thị đúng, không bị gân nhòe kéo dài hoặc viền sáng: xử lý UV/material sampling nếu cần, không chia mỗi lá thành material riêng. Cổ Thụ nguồn có lá card một mặt: giữ geometry và ghi cần material Render Face Both khi thử URP; không tự thêm độ dày cho toàn bộ nguồn.
6. Roughness lá theo P01, không emission, không giả ánh sáng mặt trời cố định trong BaseColor. Không để gân làm toàn bộ tán trắng hoặc lốm đốm khi camera xa. Xem 3/4, mặt sau, top, low và cận cảnh một cụm ở cùng điều kiện sáng.

Lưu bộ texture dùng chung và liên kết vào đúng bốn nguồn bằng đường dẫn ổn định. Kiểm tra bằng file đã lưu, không chỉ hình concept. Bàn giao preview Lớn cạnh Cổ Thụ, cận lá và báo cáo bảo toàn hình học; dừng sau P02.
```

## P03 — Vỏ thân Cây Trưởng Thành (`Tree_Large`): chưa cổ thụ

```text
Thực hiện P03 sau P02, áp dụng quy tắc chung. Chỉ chi tiết vỏ trên ArtSource/Models/Tree_Large.blend; giữ nguyên bộ lá mới, mọi hình học thân/cành/rễ và các nguồn khác.

1. Tạo/update material vỏ nâu ấm P01. Nền #8A6848, vùng sáng vật liệu #AD8860, khe nhẹ #594A39. Đây là biến thiên albedo tự nhiên, không vẽ bóng nắng cố định hoặc đường viền đen. Bề mặt matte, Metallic 0, roughness khoảng 0.80–0.90.
2. Tạo thớ dọc chạy theo trục sinh trưởng địa phương của từng thân/cành, uốn theo đường cong đã có, nhỏ dần ở cành con. Không đơn giản kéo noise theo world Z lên mọi cành ngang; tránh sọc ngang/đồng tâm như ống.
3. Rãnh nông, đứt đoạn tự nhiên, sống vỏ mềm, vài vết khía nhỏ. Vỏ Cây Lớn còn khá liền, không nhiều u bướu/hốc rỗng, không nứt sâu như Cổ Thụ. Không làm toàn cây nhăn như da hoặc dây thừng xoắn.
4. Dùng height/bump trong Blender làm bước tạo chi tiết, rồi bake/xuất tangent normal map cho runtime. Không displacement/remesh/subdivision/sculpt low-poly và không tăng triangles. Các sống/rãnh geometry hiện có chỉ được bổ sung cảm giác bề mặt, không nhân độ sâu gây lõm quá tay.
5. Audit UV thân/cành. Nếu phải unwrap, đặt seams ở mặt khó thấy, mật độ texel tương đối đều, chuyển tiếp ở chạc cành không có đường gạch/UV kéo dài; giữ geometry/corner normals. Dùng đúng UV0 trong bản export và giữ leaf UV không đổi. Rễ được giữ hình dạng và tint gỗ; không tự thêm khe già vào rễ hay dựng lại root flare.
6. Đầu ra đề xuất Large_Bark_BaseColor.png và Large_Bark_Normal.png, 1024×1024; roughness scalar. Không 4K/8K, không thêm bộ layer shader phức tạp. Lưu trong ArtSource/Textures/Tree_Common/, dùng tên ổn định không v01/v02 dư. Không bake direct/indirect light vào BaseColor.

Render thân từ front/back/left/right và 3/4, một cận chạc cành, kiểm tra ánh sáng xiên để thấy rãnh và nhìn xa để kiểm tra noise. File mở lại không mất ảnh; vị trí đỉnh/topology/normals/tri count giữ nguyên, UV/material thay đổi phải có log. Dừng sau P03, chưa sửa Cổ Thụ hoặc Unity.
```

## P04 — Vỏ thân Cổ Thụ: già hơn nhưng cùng loài

```text
Thực hiện P04 sau P03, áp dụng quy tắc chung. Sửa material/bề mặt trực tiếp ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend, giữ nguyên thân đã duyệt, rễ, cành, lá và bố cục tán. Không sửa Assets/_Project/Art/Bonsai_Tree.blend hoặc file Lớn.

1. Giữ ngôn ngữ vỏ của Cây Lớn nhưng tuổi già hơn: nền nâu xám #70634F, sống vỏ nhạt #9B896C, khe #463F34. Không xám trắng chết cây, không đen cháy, không chuyển thành đá. Metallic 0, roughness khoảng 0.85–0.95, emission 0.
2. Thớ gỗ dọc uốn quanh thân/cành theo hướng sinh trưởng; sống vỏ rộng và đứt đoạn hơn Lớn, khe nứt phân nhánh vừa phải, vài vùng chai vỏ quanh cổ cành hiện có. Chênh độ sâu thị giác khoảng 1.5–2 lần Lớn làm điểm khởi đầu, phải xem preview; không tự nhân shader strength một cách máy móc.
3. Dùng normal/roughness để thể hiện vỏ sần, rãnh lõm và mép vỏ gồ nhẹ. Không tạo hốc lớn, u mới, hang, mặt người, dây leo/rêu/nấm hoặc làm dày thân. Giữ khoảng trống nhìn thân và hình rễ; không sửa hình học để đạt vẻ già.
4. UV thân/cành theo cùng tiêu chí P03; giữ UV lá nguồn. Không bake hàng nghìn lá vào atlas vỏ. Lá tiếp tục dùng chung bộ vân P02, tint trầm hơn chứ không đổi loài hay làm mọi lá khô. Không gán normal vỏ cho Foliage.
5. Đầu ra Ancient_Bark_BaseColor.png và Ancient_Bark_Normal.png 1024×1024, roughness scalar ban đầu. Texture nguyên gốc từ procedural/painting do bạn tạo, không tải ảnh bản quyền không rõ nguồn. Chi tiết phải nhìn được ở nhiều góc, không chỉ mặt camera.

Render cùng ánh sáng Cây Lớn và so cặp: Lớn nâu ấm/rãnh nông, Cổ Thụ nâu xám/rãnh rõ; vẫn cùng một loài. Xem 4 phía, 3/4, cận thân/chạc và góc rễ. Mở lại đúng nguồn để kiểm tra. Bàn giao số liệu geometry bảo toàn, các UV/material thay đổi và known issues; dừng sau P04.
```

## P05 — Hoàn thiện texture, xuất file và kiểm tra

```text
Thực hiện P05 sau P01–P04 đã có kết quả, áp dụng quy tắc chung. Chỉ đóng gói art, bake/export và kiểm tra độc lập; không tự sửa scene/prefab/gameplay Unity.

1. Loại phụ thuộc procedural chỉ chạy trong Blender ở bản material game: BaseColor + tangent Normal thật. Nếu chỉ có Bump/Noise mà chưa bake/xuất thì chưa đạt. Image textures phải lưu ra disk, có dimensions/content thực và tương đối từ .blend; mở lại file kiểm tra không missing textures. Packing vào .blend được phép nếu có bản PNG ngoài để Unity dùng.
2. Không coi BaseColor render có đèn/bóng là albedo. Normal theo chuẩn Blender/OpenGL (+Y), dùng Non-Color; ghi quy ước, không tùy tiện đảo green để hình trông đẹp. Nếu về sau Unity cho dấu lõm/lồi đảo thì kiểm tra tangent basis/normal import trước. Tangent map cần UV0 phù hợp và giữ UV leaf dùng chung.
3. Roughness scalar là đủ ban đầu. Nếu có roughness texture thì cần chuyển sang smoothness = 1 - roughness cho URP và ghi kênh dùng; không gán raw Roughness vào Metallic. Không dùng MaskMap HDRP cho URP Lit. Không thêm ORM/height/AO riêng nếu không có mục đích đã kiểm tra.
4. Xuất đè đúng các FBX đang có, không tạo model mới:
   ArtSource/Exports/Tree_Seedling_Simple_v01.fbx
   ArtSource/Exports/Tree_Young_Simple_v01.fbx
   ArtSource/Exports/Tree_Large.fbx
   ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx
   Chỉ mesh cây, không camera/light/review scene. Blender mét/Z-up, FBX tương thích Unity Y-up/-Z-forward, scale chuẩn. Reimport vào phiên/scene sạch, kiểm tra world vertex/bounds/triangles, UV0, material slots và texture paths. Nếu FBX không khôi phục shader như Blender, ghi rõ không tương đương; dùng bảng map→URP để bàn giao, không giả material đã được Unity nghiệm thu.
5. Viết material manifest: model, object/submesh, material, tint, file BaseColor/Normal, colorspace, normal convention, UV channel, roughness và smoothness đề xuất. Khi dùng baked albedo có đủ màu thì tint trắng; chỉ dùng tint tuổi lên map được thiết kế trung tính, tránh nhân màu hai lần. Tối đa hai material chính mỗi cây, thêm shell nếu đã có; không gán material khác cho từng chiếc lá.
6. Leaf Cổ Thụ là card một mặt, ghi URP Lit Opaque/Render Face Both cần thử; không Transparent blending, không alpha clip nếu silhouette đã là mesh. Cây Lớn lá khép kín có thể dùng Front nhưng cần xem backside thật. Không giảm geometry tự động để khớp source cũ.
7. Render 4 model cùng lighting/exposure, đủ front/back/left/right/top/3/4/low, cận vân lá và cận vỏ Lớn/Cổ Thụ. So với ảnh geometry sau D34: dáng/tán/rễ không đổi, màu cùng họ, gân không gây trắng tán, vỏ không nhìn như nhựa/đá. Preview cuối phải dùng Image Texture/Normal map thật, không chỉ procedural đẹp chưa bake.
8. Lưu report trước/sau về geometry fingerprints, UV/material changes, tris, bounds, texture resolutions, SHA256 file đã lưu và FBX reimport. Rễ/thân có cạnh hở từ trước: ghi đúng, không nhận là lỗi mới do material và không tuyên bố toàn cây watertight. Không tuyên bố FPS/mobile optimized khi chưa đo.

Bàn giao đúng file gốc, bốn FBX, texture chung, manifest và Evidence/Validation. Cập nhật task và chạy update_progress.py; T10 không DONE vì chưa có nghiệm thu Unity. Nếu thiếu công cụ bake/export, giữ nguồn và báo phần chưa đạt, không bịa file/ảnh/log. Việc import Assets, tạo URP .mat, prefab, Play Mode và hồi quy phải được người dùng giao ở bước riêng; lượt này chỉ chuẩn bị đầu ra và hướng dẫn remap.
```

## Nguồn kỹ thuật đã đối chiếu

- Normal map thể hiện bumps/grooves và URP Lit có Smoothness, không phải socket Roughness: [Unity 6 — URP Lit material](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html). Agent triển khai phải đối chiếu thêm package URP 17.6.0 đang cài, không nâng package để giống ví dụ tài liệu.
- Bake cần UV và image đích; tangent normal độc lập biến đổi object: [Blender — Render Baking](https://docs.blender.org/manual/id/5.0/render/cycles/baking.html).
- Normal texture cần Non-Color và đúng UV, Blender mặc định OpenGL +Y: [Blender 5.2 — Normal Map node](https://docs.staging.blender.org/manual/en/latest/render/shader_nodes/displacement/normal_map.html).

Độ phân giải/palette/độ sần ở trên là lựa chọn đề xuất cho project, không phải thông số bắt buộc từ tài liệu hãng hoặc cam kết hiệu năng.
