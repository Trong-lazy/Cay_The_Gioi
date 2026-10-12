# Kiểm tra độc lập kết quả material P01–P05

Task: T10
Date: 2026-10-06
Result: PARTIAL — material Blender cơ bản đạt; chưa đạt đầy đủ chất lượng vỏ Cổ Thụ và bàn giao tái tạo material URP.

> **Cập nhật khắc phục R1, R2, R3 (06/10/2026 — Quyết định D41)**: Cả ba mục R1, R2, R3 nêu trong báo cáo review này đã được xử lý và kiểm chứng độc lập bằng dữ liệu + render thực tế tại [Material_Fixes_Antigravity_20261006.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Validation/Material_Fixes_Antigravity_20261006.md) và thư mục bằng chứng [Evidence/Material_Fixes_Antigravity_20261006/](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Material_Fixes_Antigravity_20261006). Giữ nguyên báo cáo dưới đây làm mốc lịch sử đối chiếu trước khi sửa.

## Phạm vi và phương pháp

Người dùng yêu cầu kiểm tra agent báo xong material. Chỉ đọc saved files, render và reimport độc lập bằng Blender 5.2.2 LTS background; không sửa/lưu/export lại bất cứ .blend, FBX hoặc texture nào. Không tác động phiên GUI đang mở hoặc dữ liệu chưa lưu. Unity/Blender MCP không có trong phiên kiểm tra; không xác nhận Console, Play Mode hoặc màu thực trong URP.

Đọc task, bộ prompt P01–P05, manifest, báo cáo và script triển khai; đối chiếu trực tiếp bốn nguồn, bốn FBX và sáu PNG. Script kiểm tra: `Evidence/Material_Independent_Review_20261006/audit_materials.py`; số liệu đầy đủ: `audit_report.json` cùng thư mục. SHA256 đầu/cuối của nguồn, FBX, PNG và hai asset Bonsai trong Assets đều giữ nguyên trong lượt review.

## Đã đạt qua kiểm tra độc lập

- SHA256 cả bốn blend/FBX hiện hành khớp báo cáo P05 của agent, không xem nhầm phiên bản cũ.
- Mầm 2.778 tris, Non 4.574, Lớn 26.682, Cổ Thụ 73.888; không thêm subdivision/displacement runtime. Tọa độ đỉnh, topology, corner normals và world matrix trùng baseline so sánh: Mầm/Non/Lớn dùng `.blend1` hiện có từ trước các bước cuối; Cổ Thụ dùng `Assets/_Project/Art/Bonsai_Tree.blend` có hash đã bảo toàn. `.blend1` chỉ là nguồn đối chiếu hiện có, không được tạo trong review; không coi nó chứng minh toàn bộ lịch sử trước khi backup được ghi.
- Leaf UV Lớn trùng backup và Cổ Thụ trùng baseline Assets; Mầm/Non leaf UV và Lớn bark UV có thay đổi hợp lệ được báo. Các file không mất hình cây đã duyệt.
- Có sáu PNG thực: lá 512×512, hai bộ vỏ 1024×1024; hình có nội dung, đường dẫn trong blend tương đối và resolve được. BaseColor dùng sRGB, Normal dùng Non-Color, normal node dùng Tangent. Độ dài vector giải mã normal khoảng 0.987–1.009 (hợp lý với lượng tử hóa 8-bit), không phát hiện map normal toàn phẳng hoặc dữ liệu hỏng.
- Cả bốn cây dùng chung hai file lá. Màu/gân Mầm và Non đọc rõ; Lớn/Cổ Thụ cùng họ xanh và vân lá, thân phân biệt nâu ấm/nâu xám. Đây là nhận xét hình ảnh từ render mới, không phải bảng màu đã được người dùng chốt.
- Normal vỏ thật có tác dụng: render cận bật/tắt liên kết Normal trong bộ nhớ cho thấy thay đổi bóng bề mặt. Render tắt chỉ là chẩn đoán, không lưu vào source.
- Reimport cả bốn FBX: vertices/tris, UV values (hash), tên material slots đều trùng nguồn; sai số world vertex lớn nhất `7.152557373046875e-7` m. Điều này chỉ chứng minh hình học/UV/slots, không chứng minh shader/màu đã tương đương.

## Phần chưa đạt hoặc cần chỉnh

### R1 — Material lá chưa được bàn giao tương đương sau export (ưu tiên cao)

Trong cả bốn nguồn, Leaf BaseColor đi qua `ShaderNodeMix`, RGBA/MULTIPLY, kết hợp texture và tint. Sau reimport cả bốn FBX, shader lá không còn texture BaseColor/Mix/tint; Base Color là màu mặc định `(0.8,0.8,0.8,1)`, chỉ Normal texture còn liên kết. Render Cây Lớn FBX cho thấy tán trắng/xám, khác nguồn xanh rõ rệt.

FBX hạn chế node shader là điều bình thường, không phải bằng chứng texture PNG bị mất. P05 đã cho phép remap riêng, nhưng yêu cầu ghi rõ không tương đương và bàn giao cách dựng đúng. Báo cáo hiện tại chỉ kiểm tra counts/UV layers/material slots rồi nhận PASSED; chưa kiểm tra màu/material sau reimport. Manifest hiện không đủ để tái tạo chính xác shader lá bằng URP Lit:

- Source Mix có **A=tint, B=texture**, nên BaseColor linear là `A × ((1-f) + f×B)`, với f=0.70/0.85/1.00/1.00 theo tuổi. URP Lit BaseMap×Tint thuần không biểu diễn cùng kết quả cho hai mốc f<1 nếu chỉ kéo PNG chung như hướng dẫn.
- Cần chọn và kiểm chứng giải pháp bàn giao: bake/chuẩn bị đầu ra lá phản ánh blend từng mức vào PNG, hoặc một phương án tint/map tương đương có hướng dẫn và preview đối chiếu. Không cần tạo thêm model, không tự thêm shader riêng. Review này không thực hiện giải pháp.
- Nhận định “FBX giữ material slots” là đúng; nhận định đã hoàn tất màu/material để dùng ngay chỉ bằng import thì chưa đúng. Chưa có kiểm tra Unity để biết bước remap cuối thực tế ra sao.

### R2 — Vân vỏ Cổ Thụ chưa đi theo thớ sinh trưởng (ưu tiên chất lượng hình ảnh)

Render cận mới ở `ancient/bark.png` và ảnh agent `final/ancient/closeup_bark.png` đều cho thấy nhiều dải ngang/chéo cuộn sóng, đổi hướng đột ngột và nối không liên tục giữa những phần thân sát nhau. Theo đánh giá hình ảnh của reviewer, nhiều vùng nhìn như vân đá hoặc gỗ đánh bóng hơn là khe/sống vỏ dọc tự nhiên như P04 mô tả. Tắt Normal vẫn thấy hướng vân trong BaseColor, nên không nên chỉ giảm normal strength để che vấn đề.

Texture có hướng dọc ở ảnh phẳng, nhưng hướng sau khi gán lên UV nguồn không nhất quán với hướng thân/cành. Cần chỉnh orientation/scale/UV mapping theo trục địa phương của thân/cành, giảm sự lặp của dải sóng và phân biệt rãnh dọc chính với khía ngắn phụ. Giữ geometry, không sculpt lại thân. Cây Lớn tương đối phù hợp hơn; vẫn nên kiểm tra seams/chạc khi chỉnh mapping, không tự tăng độ nứt ngang bằng Cổ Thụ.

### R3 — Manifest có thiếu và sai thông số (ưu tiên trước Unity)

- Shader bark cả Lớn/Cổ Thụ có Mapping scale `(2.5,2.5,1)`; manifest không ghi tiling `(2.5,2.5)` cho URP. Bản FBX Blender reimport có giữ mapping scale, nhưng không thể suy ra Unity material tự thiết lập đúng nếu bảng remap bỏ sót.
- Manifest ghi Leaf Common BaseColor nền `#C4D8A0`; PNG thật là trung tính xám kem, pixel góc `#D1D1C8`, RGB mean khoảng `(0.82465,0.82465,0.79000)` dưới chế độ đọc Non-Color. Cần mô tả đúng map tintable thay vì ghi xanh khác dữ liệu.
- Bảng P05 ghi Seedling “4 slots” nhưng liệt kê 5 tên material; actual có 5 material dùng trên 4 mesh, SeedShell có 2 slots. Không phải hỏng asset, nhưng cần sửa tài liệu.

### R4 — Ghi chú nhẹ, không chặn một mình

- Gân lá ở cây lớn rõ và cùng một pattern nhưng hơi đều/đậm khi cận; một số mép hiện đường xanh sáng. Có thể giảm contrast và xử lý sampling vùng mép nếu người dùng thích tự nhiên hơn, nhưng không thay hình lá đã đồng bộ.
- Foliage Lớn có UV suy biến ở 12.320/24.640 tam giác, do các mặt mép mỏng dùng UV cố định từ D34. Không phải hình học suy biến hoặc phát sinh mới trong material. Cần lưu ý tangents/normal sampling ở mép khi kiểm tra URP, không ép unwrap toàn bộ lá để xóa con số.
- Thư mục nguồn có 4 `.blend1` hiện có; không xóa trong review. Mầm/Non backup ghi sớm hơn các bước material; backup Lớn/Cổ Thụ ghi trong buổi làm art. Vì vậy không nên diễn giải “không tạo .blend1 ở lệnh save_version=0” thành “thư mục không có bản backup”.
- Ảnh lineup agent đang crop một phần Cổ Thụ; nên có lineup đủ cây, hoặc mỗi cây được frame riêng cho nghiệm thu. Không đánh giá màu cây nhỏ chỉ từ lineup này.

## Bằng chứng xem trực tiếp

`Evidence/Material_Independent_Review_20261006/` chứa render từ saved files thực, ánh sáng trung tính, AgX và cùng cấu hình theo tỷ lệ cây:

- `seedling/perspective.png`, `young/perspective.png`: màu và vân lá cây nhỏ.
- `large/perspective.png` và `large_fbx/perspective.png`: nguồn xanh so với FBX lá trắng/xám do BaseColor không được tái tạo.
- `large/bark.png`, `large/bark_normal_disabled.png`: chi tiết vỏ mới và kiểm tra Normal.
- `ancient/perspective.png`, `ancient/bark.png`, `ancient/bark_normal_disabled.png`: màu Cổ Thụ/hướng vân và kiểm tra Normal.
- Có render mặt sau cho bốn nguồn. Không phải ảnh AI minh họa; không lưu material chẩn đoán trở lại file gốc.

## Kết luận và giới hạn

Chấp nhận được phần **màu/vân lá cơ bản trong Blender, texture thật và bảo toàn geometry**. Chưa chấp nhận tuyên bố hoàn thành toàn bộ material: cần sửa vân vỏ Cổ Thụ và bổ sung/kiểm chứng đầu ra màu lá cùng manifest tái tạo URP. Sau đó mới nghiệm thu tích hợp Unity khi người dùng giao việc.

T10 vẫn `IN_PROGRESS`, acceptance gameplay chưa được tích. Đây là review, không sửa model/texture/manifest hoặc giao agent khắc phục. Các kết quả chưa thử trong Editor vẫn không PASS.

Nguồn kỹ thuật: [Blender — Node shader import/export limitations](https://docs.blender.org/manual/id/4.0/addons/import_export/node_shaders_info.html) giải thích format xuất thường chỉ hỗ trợ shader cơ bản; [Unity — URP Lit](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html) mô tả BaseMap color/texture, normal và tiling. Kết luận mất BaseColor/tint ở trên dựa vào **node data và render FBX thực**, không chỉ suy luận từ tài liệu.
