# Nghiệm thu độc lập nguồn Cây Trưởng Thành sau sửa

Task: T10
Date: 2026-10-07
Result: PARTIAL

Đạt đồng bộ hình học/material lá, tách vùng và export. Chưa chốt toàn bộ asset: cần cải thiện thể tích/độ phủ của một số tầng tán. Có material trong Blender/FBX; chưa có material URP hoặc model mature trong Assets Unity. T10 giữ IN_PROGRESS, không tích acceptance gameplay.

## Phương pháp

Mở file đã lưu bằng Blender 5.2.2 LTS background, không chạy script sửa của agent. Script kiểm tra mới `Evidence/Mature_Independent_Acceptance_20261007/review.py` chỉ nạp định nghĩa audit/render đã dùng ở review trước. Mở Tree_Large làm chuẩn, backup mature đã khớp hash review trước, source mature cuối và ba FBX trong scene trắng. Render mới 8 góc LOD0, isolate rễ/thân/gỗ và cận cổ rễ, các góc trước/sau/dưới của LOD1/2 và FBX. Bật collection LOD chỉ trong bộ nhớ để render được nguồn LOD1/2 đang ẩn. Không lưu blend, export, sửa texture hoặc Assets.

Báo cáo đo mới: `Evidence/Mature_Independent_Acceptance_20261007/report.json`. Lượt đo/render đầu giữ hash toàn bộ art đầu vào (`initial_verified_report.json`). Trong lượt render bổ sung bật collection LOD, source mature được lưu lại từ bên ngoài: SHA256 từ `90164380...` sang `2ec327fb...`; script phát hiện thay đổi và không ghi report bổ sung. Đã mở lại bản lưu cuối, chạy audit-only đầy đủ, xác nhận ổn định hash qua lần kiểm tra cuối. Cả 9 mesh giữ positions/topology/corner normals/UV/world_matrix, từng material giữ graph/thông số/ảnh so bản đã render; chỉ thứ tự liệt kê material trong JSON khác. Vì vậy render vẫn đại diện geometry/material bản cuối và ba FBX vẫn tương ứng. Không xác định người/process lưu nguồn, không quy kết. Các nguồn/texture tham chiếu được theo dõi khớp hash review trước sửa. Backup mature khớp hash gốc review trước.

## Kết quả đạt

- Cả source và mỗi FBX có ba mesh đúng vùng Roots, Trunk_Branches, Foliage cho từng LOD. Isolate Roots chỉ gồm hệ rễ sát đất (Z từ khoảng -0.041 đến 0.475 m); Trunk gồm cổ thân phía trên và cành. Bề mặt gỗ ghép lại không phát hiện khe mới do cắt tại cổ rễ ở các góc đã xem.
- Đối chiếu toàn bộ 484/273/116 cụm lá với 8 mẫu 4 phiến lấy trực tiếp từ Tree_Large, bằng phép fit quay/dịch/uniform scale trên mặt trung hòa. Sai số chuẩn hóa lớn nhất lần lượt 3.37e-7 / 2.81e-7 / 2.20e-7 m; không có cụm vượt 1e-5 m. Đây là đo độc lập, không sử dụng các phép biến đổi tự báo của agent làm kết quả kiểm tra.
- Bề mặt gỗ mới so backup bằng BVH: sai số đỉnh mới tới mặt gốc lớn nhất 1.37e-7 m; chiều ngược lại 0 m. Giữ dáng/pivot, scale 1 và rotation 0 trên các mesh.
- Source và FBX khớp số vertices/tris, UV hashes và material slots trên cả 9 mesh. FBX mỗi file chỉ có 3 MESH, không có camera/đèn. Sai số vị trí world-space lớn nhất 1.55e-6 m.
- Không có polygon diện tích 0 hoặc UV triangle suy biến theo ngưỡng audit trên các mesh mature. Số tris xác nhận: LOD0 37.018; LOD1 20.238; LOD2 8.504. Không coi số tris là benchmark hiệu năng mobile.

## Material thực sự hiện có

| Material | Gán cho | BaseColor | Normal | Roughness / Normal Strength |
| --- | --- | --- | --- | --- |
| M_Foliage | Lá cả 3 LOD | Leaf_Large_BaseColor.png | Leaf_Common_Normal.png | 0.72 / 0.20 |
| M_WT_Bark | Thân/cành và rễ | WT_Bark_BaseColor.png | WT_Bark_Normal.png | 0.80 / 0.55 |
| M_WT_RootAccent | Một số mặt rễ LOD0/1 | WT_RootAccent_BaseColor.png | WT_Bark_Normal.png | 0.84 / 0.55 |

Cả ba dùng Principled BSDF, Metallic 0, Emission Strength 0; BaseColor texture nối trực tiếp, tint trắng. Lá có map 512×512 sRGB và normal 512×512 Non-Color/Tangent, đường dẫn tương đối resolve được; tên datablock normal có suffix `.001` nhưng file nguồn vẫn đúng Leaf_Common_Normal.png. Render FBX không mất màu lá thành trắng/xám. LOD2 không sử dụng RootAccent, không tự coi đó là thiếu material.

## Điểm chưa đạt để chốt toàn bộ model

Tán giữ bố cục tầng nhưng có vùng quá mỏng khi nhìn ngang/chính diện/phía sau; đầu cành ở tán trên lộ vượt khỏi các lớp lá, mặt dưới các tầng khá phẳng và khoảng trống lớn hơn bản trước. Xem `source_lod0/front.png`, `back.png`, `underside.png`, và `source_lod1/perspective.png`, `source_lod2/perspective.png`. Đây là đánh giá hình ảnh có bằng chứng, không phải thiếu mesh hoặc texture và không phải FBX làm mất lá.

Prompt mục 3/7 yêu cầu tán vẫn dày, phủ đa góc và giữ thể tích mature. Cần bố trí thêm/định hướng lại lớp lá dưới và quanh đầu cành bằng cùng cụm nguồn đã đạt; giữ material, gỗ và vùng tán. LOD thấp được giảm chi tiết nhưng cần kiểm tra silhouette/chuyển LOD khi tích hợp, vì độ thưa đang thay đổi rõ. Chưa đo lỗi chuyển LOD trong game và chưa kết luận phải dùng cùng mật độ ở mọi LOD.

Các mối nối ống gỗ nhìn cận còn có cạnh/tiếp giáp sẵn có; bảo toàn bề mặt được xác nhận, không quy kết chúng là lỗi mới do tách rễ. Không yêu cầu dựng lại thân/rễ trong lượt review này.

## Unity và giới hạn nghiệm thu

Unity MCP đúng MainScene/6000.6.1f1, Play stopped, không compile; Console query 5 error/warning trả 0 entries; scene dirty=false. AssetDatabase tìm WT_MatureTree trả rỗng; material project chỉ có placeholder Bark.mat, Leaf.mat, Soil.mat. Chưa nhập mature hoặc tạo .mat URP cho nó. Không chạy Play Mode, tap, camera, Burning, Evolution, save hoặc test hồi quy vì đây là review asset, không yêu cầu tích hợp.

Không có Empty anchor trong source/FBX; agent bàn giao tọa độ đề xuất qua manifest/report, phù hợp phương án export MESH-only. Chưa có bằng chứng prefab/gameplay đã dùng các neo.

Kết luận: có material đầy đủ cho nguồn Blender/FBX và hai yêu cầu sửa chính đã đạt; nghiệm thu toàn bộ art còn PARTIAL do độ phủ tán, tích hợp Unity chưa thực hiện. Chưa sửa model trong phiên kiểm tra này.
