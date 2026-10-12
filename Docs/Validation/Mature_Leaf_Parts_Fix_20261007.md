# Báo cáo thực thi sửa Cây Trưởng Thành — Đồng bộ lá và tách vùng cây (`WT_MatureTree`)

- **Ngày thực hiện**: `2026-10-07`
- **Task**: `T10` (`IN_PROGRESS`)
- **Quyết định liên quan**: `D34`, `D41`, `D43`, `D44`, `D45`
- **Đặc tả thực thi**: [Mature_Tree_Leaf_Parts_Fix_Prompt_20261007.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Art/Mature_Tree_Leaf_Parts_Fix_Prompt_20261007.md)
- **Báo cáo review đầu vào**: [Mature_Leaf_Parts_Review_20261007.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Validation/Mature_Leaf_Parts_Review_20261007.md)
- **Thư mục bằng chứng & báo cáo JSON**: [Evidence/Mature_Leaf_Parts_Fix_20261007/](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Mature_Leaf_Parts_Fix_20261007/)
  - Baseline trước sửa: [baseline_before_fix.json](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Mature_Leaf_Parts_Fix_20261007/baseline_before_fix.json)
  - Tóm tắt thực thi sửa: [fix_execution_summary.json](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Mature_Leaf_Parts_Fix_20261007/fix_execution_summary.json)
  - Báo cáo kiểm định độc lập & FBX roundtrip: [verification_report.json](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Evidence/Mature_Leaf_Parts_Fix_20261007/verification_report.json)
- **Trạng thái bàn giao**: **`READY_FOR_REVIEW`** (Chờ người nghiệm thu độc lập Codex kiểm tra; chưa đánh dấu `T10` là `DONE` và chưa tự tuyên bố `PASS`).

---

## 1. Bảo toàn Baseline, Backup có ngày và Kiểm tra File Chỉ Đọc

1. **Đường dẫn phục hồi (Single Dated Backup)**:
   - Thư mục: `Evidence/Mature_Leaf_Parts_Fix_20261007/backup_20261007/`
   - Bao gồm đúng 4 file gốc trước sửa của Cây Trưởng Thành (`WT_MatureTree.blend`, `WT_MatureTree_LOD0.fbx`, `WT_MatureTree_LOD1.fbx`, `WT_MatureTree_LOD2.fbx`), khớp 100% SHA256 với baseline trong `Docs/Validation/Mature_Leaf_Parts_Review_20261007.md`.
2. **Đối chiếu SHA256 trước và sau sửa**:

| Đường dẫn file | Vai trò | SHA256 trước sửa (Baseline) | SHA256 sau sửa | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| `ArtSource/Models/WT_MatureTree.blend` | Nguồn Cây Trưởng Thành | `dc2ecbf840a528258233619da89b4bc10d82bbcd661797d8a8b5cabb52257e42` | `90164380016f8aad87c1489934ed07dae558e8efcbcabde39d17a81af3406f95` | **UPDATED (`READY_FOR_REVIEW`)** |
| `ArtSource/Exports/WT_MatureTree_LOD0.fbx` | Xuất FBX LOD0 | `36f7d02d7a9b18c3a2a874bd5628d3e5c1401e57c6ffa2153ec5e62b39b1e027` | `10a09b4ebbf5f54209678fe1e7dcfae57b233e19f7e7f107b01282cfb9b86c37` | **UPDATED (`READY_FOR_REVIEW`)** |
| `ArtSource/Exports/WT_MatureTree_LOD1.fbx` | Xuất FBX LOD1 | `7d7b5ea29ab56f1f51b35cc3d8efea6f7828b3915fb46d30e3a9f1fb34fc8017` | `956668c7e9f6df7b10e5d1935cefb4f101f077186235df03362f4b22b7177bb2` | **UPDATED (`READY_FOR_REVIEW`)** |
| `ArtSource/Exports/WT_MatureTree_LOD2.fbx` | Xuất FBX LOD2 | `003b5fe956ea25ab838ec2e8318eabad6b97085895f43683dbf7639d0e13d6d8` | `f0f9810e0ffadceb23f702b169d44c60bd3370e835881e3921d07a59da739ac6` | **UPDATED (`READY_FOR_REVIEW`)** |
| `ArtSource/Models/Tree_Seedling_Simple_v01.blend` | Nguồn Mầm (Read-only) | `8781507c59608f546229eb4bd0fa0da8b4b6218416fb5e9a0127e2d5c2918938` | `8781507c59608f546229eb4bd0fa0da8b4b6218416fb5e9a0127e2d5c2918938` | **UNCHANGED (100%)** |
| `ArtSource/Models/Tree_Young_Simple_v01.blend` | Nguồn Non (Read-only) | `a862d973f63adad5fbaa44713d1948928fefe45988864249dbd1b7bcd4f3e315` | `a862d973f63adad5fbaa44713d1948928fefe45988864249dbd1b7bcd4f3e315` | **UNCHANGED (100%)** |
| `ArtSource/Models/Tree_Large.blend` | Nguồn Lớn (Read-only) | `5cbb554d09a88becc38eaec33efec6acb56e7417bad5ad0ad804e8ce633ba70b` | `5cbb554d09a88becc38eaec33efec6acb56e7417bad5ad0ad804e8ce633ba70b` | **UNCHANGED (100%)** |
| `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` | Nguồn Cổ Thụ (Read-only) | `b1c1a1504e24fd397a9555afd0382d40032fdb44e850e9e6bacd1d0d57bdc95b` | `b1c1a1504e24fd397a9555afd0382d40032fdb44e850e9e6bacd1d0d57bdc95b` | **UNCHANGED (100%)** |
| `ArtSource/Exports/Tree_Seedling_Simple_v01.fbx` | FBX Mầm (Read-only) | `6fc4aa5a5a11d3a0dba9dc903891cd59d2f6845ff891520432d43e19cb6d60e6` | `6fc4aa5a5a11d3a0dba9dc903891cd59d2f6845ff891520432d43e19cb6d60e6` | **UNCHANGED (100%)** |
| `ArtSource/Exports/Tree_Young_Simple_v01.fbx` | FBX Non (Read-only) | `2f3bfb2c354c91c89ea5cdd96e862f90c60265fb06a304da60615e9f8ac71fa9` | `2f3bfb2c354c91c89ea5cdd96e862f90c60265fb06a304da60615e9f8ac71fa9` | **UNCHANGED (100%)** |
| `ArtSource/Exports/Tree_Large.fbx` | FBX Lớn (Read-only) | `1d0b4b383865d51f8f5c39ac0a72cc2eba2a1dd35919229ce7896a0dcf229154` | `1d0b4b383865d51f8f5c39ac0a72cc2eba2a1dd35919229ce7896a0dcf229154` | **UNCHANGED (100%)** |
| `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx` | FBX Cổ Thụ (Read-only) | `2efb2b489ccf2d08e3a4c9fa2f058f9145181fb7e45ab4cfa28c01f0099d1ba4` | `2efb2b489ccf2d08e3a4c9fa2f058f9145181fb7e45ab4cfa28c01f0099d1ba4` | **UNCHANGED (100%)** |
| `ArtSource/Textures/Tree_Common/*.png` (15 file) | Toàn bộ PNG trong `Tree_Common/` | *Khớp 100% 15/15 SHA256* | *Khớp 100% 15/15 SHA256* | **UNCHANGED (100%)** |
| `Assets/_Project/Art/Bonsai_Tree.blend` & `.fbx` | Asset trong `Assets/` | `b99a966a...` / `80261072...` | `b99a966a...` / `80261072...` | **UNCHANGED (100%)** |

---

## 2. Đồng bộ Lá từ Hình học Cụm Nguồn Thật & Material `M_Foliage` (Mục 3)

### 2.1. Trích xuất 8 mẫu cụm lá 4 phiến thật từ nguồn chuẩn D34
- Đọc trực tiếp object `Foliage` trong `ArtSource/Models/Tree_Large.blend` (đối chiếu chéo với object `Foliage` trong `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend`):
  - Trích xuất 8 mẫu cụm 4 phiến (`templates[0..7]`) ứng với 32 đảo phiến lá đầu tiên (`blade_islands 0..31`, dải đỉnh `0..319` trong `Tree_Large.blend::Foliage` và `0..159` trong `Bonsai_Trunk_Candidate_v01.blend::Foliage`).
  - Sai số đối chiếu chéo mặt trung hòa chuẩn hóa giữa `Tree_Large.blend::Foliage` và `Bonsai_Trunk_Candidate_v01.blend::Foliage`: **`5.03245e-07 m`** (`< 1e-6 m`).
  - Mỗi phiến lá nguồn có tỷ lệ Rộng : Dài chuẩn $\approx 1 : 2$ ($0.236\text{ m} : 0.472\text{ m}$), nếp gấp sống lá chữ V $18.69^\circ$, uốn cong rủ tự nhiên về ngọn, tọa độ UV chuẩn: cuống `(0.5, 0.0)`, mép trái `(0.0, 0.5)`, mép phải `(1.0, 0.5)`, ngọn `(0.5, 1.0)`.
  - Mỗi phiến lá được đóng kín 2 mặt (`10` đỉnh, `12` mặt gồm `8` tam giác mặt trên/dưới + `4` tứ giác mép mỏng $1.5\text{ mm} \times \text{scale}$, tương đương `16` tam giác/phiến, `64` tam giác/cụm 4 phiến). Các tứ giác mép viền được gán UV vi mô không suy biến quanh tâm sống lá `[[0.495, 0.495], [0.505, 0.495], [0.505, 0.505], [0.495, 0.505]]`, đảm bảo đồng thời **`non_manifold_edges = 0`** và **`degenerate_uv_triangles = 0`**.

| Template ID | Nguồn `Tree_Large.blend` | Blade Islands | Dải Vertex (`Tree_Large`) | Dải Vertex (`Bonsai_Trunk_Candidate_v01`) | Bán kính cụm cục bộ ($R_{\text{local}}$) |
| :---: | :---: | :---: | :---: | :---: | :---: |
| `0` | `Foliage` | `0, 1, 2, 3` | `0 .. 39` | `0 .. 19` | `0.428260 m` |
| `1` | `Foliage` | `4, 5, 6, 7` | `40 .. 79` | `20 .. 39` | `0.413141 m` |
| `2` | `Foliage` | `8, 9, 10, 11` | `80 .. 119` | `40 .. 59` | `0.414575 m` |
| `3` | `Foliage` | `12, 13, 14, 15` | `120 .. 159` | `60 .. 79` | `0.390554 m` |
| `4` | `Foliage` | `16, 17, 18, 19` | `160 .. 199` | `80 .. 99` | `0.459765 m` |
| `5` | `Foliage` | `20, 21, 22, 23` | `200 .. 239` | `100 .. 119` | `0.490671 m` |
| `6` | `Foliage` | `24, 25, 26, 27` | `240 .. 279` | `120 .. 139` | `0.396901 m` |
| `7` | `Foliage` | `28, 29, 30, 31` | `280 .. 319` | `140 .. 159` | `0.401263 m` |

### 2.2. Phép biến đổi đặt cụm lá lên 33 vùng tán của Cây Trưởng Thành
- Giữ nguyên tâm và vùng không gian 3D của toàn bộ **33 tầng/vùng tán** (`4` chồi thân dưới + `29` vòm tán chính trên các nhánh cấp 1–3) của `WT_MatureTree`.
- Mỗi cụm 4 phiến được đặt bằng phép biến đổi cứng gồm **dịch chuyển** $\mathbf{t}$, **quay 3D trực giao** $\mathbf{R} \in SO(3)$ và **uniform scale** $s > 0$:
  $$\mathbf{p}_{\text{world}} = \mathbf{t} + s \, \mathbf{R} \, \mathbf{p}_{\text{local}}$$
- Mẫu 5 phép biến đổi đầu tiên trên `WT_MatureTree_Foliage` (`LOD0`, chi tiết đầy đủ lưu trong `fix_execution_summary.json`):
  - `sprig_index = 0` (`template 0`, vertices `0..39`): $\mathbf{t} = (1.41250, -0.32648, 1.84557)\text{ m}$, uniform scale $s = 1.31789$.
  - `sprig_index = 1` (`template 1`, vertices `40..79`): $\mathbf{t} = (1.41010, -0.27457, 1.83991)\text{ m}$, uniform scale $s = 1.46893$.
  - `sprig_index = 2` (`template 2`, vertices `80..119`): $\mathbf{t} = (1.48057, -0.26802, 1.75053)\text{ m}$, uniform scale $s = 1.41268$.
  - `sprig_index = 3` (`template 3`, vertices `120..159`): $\mathbf{t} = (1.31141, -0.31601, 1.80330)\text{ m}$, uniform scale $s = 1.49091$.
  - `sprig_index = 4` (`template 4`, vertices `160..199`): $\mathbf{t} = (1.28871, -0.21600, 1.77835)\text{ m}$, uniform scale $s = 1.21268$.
- **Sai số khôi phục mặt trung hòa chuẩn hóa** (`normalized_mid_surface_max_error_m`) trên toàn bộ các phiến lá sau khi đặt:
  - `LOD0` (`484` cụm / `1,936` phiến): **`2.9714e-07 m`**
  - `LOD1` (`273` cụm / `1,092` phiến): **`2.1905e-07 m`**
  - `LOD2` (`116` cụm / `464` phiến): **`1.5997e-07 m`**

### 2.3. Đồng bộ Material lá `M_Foliage`
- Append trực tiếp material `M_Foliage` từ `ArtSource/Models/Tree_Large.blend` và gán duy nhất cho `WT_MatureTree_Foliage`, `WT_MatureTree_Foliage_LOD1`, `WT_MatureTree_Foliage_LOD2`:
  - `Leaf_Large_BaseColor.png` (`sRGB`, đường dẫn tương đối `//..\Textures\Tree_Common\Leaf_Large_BaseColor.png`) nối trực tiếp vào `Principled BSDF.Base Color` với tint trắng `(1.0, 1.0, 1.0, 1.0)`.
  - `Leaf_Common_Normal.png` (`Non-Color`, đường dẫn tương đối `//..\Textures\Tree_Common\Leaf_Common_Normal.png`) nối qua `Normal Map` (`Tangent`, `uv_map = "Float2"`, `Strength = 0.20`) vào `Principled BSDF.Normal`.
  - `Roughness = 0.72`, `Metallic = 0.0`, `Emission Strength = 0.0`.
- Đã gỡ bỏ hoàn toàn `M_WT_Leaves_Light` và `M_WT_Leaves_Dark` khỏi `WT_MatureTree.blend` và cả 3 file FBX; giữ nguyên 2 file PNG cũ trên đĩa ở trạng thái không còn tham chiếu.

---

## 3. Phân tách 3 Vùng Cây (`Roots`, `Trunk_Branches`, `Foliage`) trên Cả 3 LOD (Mục 4)

### 3.1. Phương pháp tách tại cổ rễ và bảo toàn bề mặt gỗ
- Trên mỗi LOD (`LOD0`, `LOD1`, `LOD2`), tách `WT_MatureTree_TrunkAndRoots*` và hợp nhất `WT_MatureTree_Branches*` thành 2 mesh gỗ độc lập:
  - **`WT_MatureTree_Roots`** (`_LOD1`, `_LOD2`): Toàn bộ hệ rễ chính xòe dưới mặt phẳng cổ rễ ngang $z = 0.475\text{ m}$ cùng toàn bộ các nhánh rễ phụ sát đất (`z in [-0.0412 m, 0.4750 m]`).
  - **`WT_MatureTree_Trunk_Branches`** (`_LOD1`, `_LOD2`): Toàn bộ thân chính xoắn (`Island 0`, bắt đầu từ $z = 0.4803\text{ m}$), phần sống thân trên cổ rễ ($z \ge 0.475\text{ m}$), chạc phân nhánh chính và toàn bộ cành cấp 1–3 (`z in [0.4750 m, 8.5905 m]`).
- Các đa giác băng qua mặt phẳng cổ rễ $z = 0.475\text{ m}$ được cắt chính xác theo thuật toán Sutherland-Hodgman trên từng `loop_triangle` gốc, dùng chung đỉnh cắt trên mỗi cạnh lưới và nội suy tuyến tính chính xác tọa độ UV cùng vector pháp tuyến góc (`corner_normals`).
- **Sai số bề mặt thế giới tối đa so với gỗ trước sửa**:
  - `LOD0`: `max_new_vertex_to_baseline_surface_m = 7.0e-08 m`, `max_baseline_vertex_to_new_surface_m = 0.0 m`.
  - `LOD1`: `max_new_vertex_to_baseline_surface_m = 7.0e-08 m`, `max_baseline_vertex_to_new_surface_m = 0.0 m`.
  - `LOD2`: `max_new_vertex_to_baseline_surface_m = 1.4e-07 m`, `max_baseline_vertex_to_new_surface_m = 0.0 m`.

### 3.2. Bảng thống kê số liệu Hình học Trước và Sau sửa (Đo trực tiếp từ `.blend` và `.fbx` Reimport)

| LOD | Mesh Object | Trước sửa (Verts / Tris) | Sau sửa (Verts / Polys / Tris) | Materials | Bounds Min (X, Y, Z) m | Bounds Max (X, Y, Z) m | Kiểm tra hình học & UV |
| :---: | :--- | :---: | :---: | :--- | :---: | :---: | :--- |
| **LOD0** | `WT_MatureTree_Roots` | *(Gộp trong TrunkAndRoots: 1,416 v / 2,670 t)* | `1,050` v / `1,092` p / **`1,864`** t | `M_WT_Bark`, `M_WT_RootAccent` | `(-3.490, -2.993, -0.041)` | `(3.259, 3.030, 0.475)` | `zero_area=0`, `deg_uv=0`, `loose=0` |
| **LOD0** | `WT_MatureTree_Trunk_Branches` | *(Branches riêng: 1,531 v / 3,036 t)* | `2,251` v / `2,469` p / **`4,178`** t | `M_WT_Bark` | `(-3.455, -2.383, 0.475)` | `(3.056, 2.580, 8.590)` | `zero_area=0`, `deg_uv=0`, `loose=0` |
| **LOD0** | `WT_MatureTree_Foliage` | `8,190` v / `10,920` t | `19,360` v / `23,232` p / **`30,976`** t | `M_Foliage` | `(-4.863, -3.819, 1.436)` | `(4.453, 4.136, 9.153)` | `zero_area=0`, `deg_uv=0`, `non_manifold=0` |
| **LOD0** | **Tổng cộng LOD0** | **`11,137` v / `16,626` t** | **`22,661` v / `26,793` p / `37,018` t** | 3 materials | `(-4.863, -3.819, -0.041)` | `(4.453, 4.136, 9.153)` | Scale `(1,1,1)`, Rot `(0,0,0)` |
| **LOD1** | `WT_MatureTree_Roots_LOD1` | *(Gộp trong TrunkAndRoots_LOD1: 682 v / 1,246 t)* | `467` v / `507` p / **`771`** t | `M_WT_Bark`, `M_WT_RootAccent` | `(-3.489, -2.992, -0.041)` | `(3.258, 3.029, 0.475)` | `zero_area=0`, `deg_uv=0`, `loose=0` |
| **LOD1** | `WT_MatureTree_Trunk_Branches_LOD1` | *(Branches_LOD1 riêng: 785 v / 1,282 t)* | `1,119` v / `1,249` p / **`1,995`** t | `M_WT_Bark` | `(-3.451, -2.384, 0.475)` | `(3.052, 2.580, 8.590)` | `zero_area=0`, `deg_uv=0`, `loose=0` |
| **LOD1** | `WT_MatureTree_Foliage_LOD1` | `4,590` v / `6,120` t | `10,920` v / `13,104` p / **`17,472`** t | `M_Foliage` | `(-4.990, -4.011, 1.408)` | `(4.520, 4.185, 9.368)` | `zero_area=0`, `deg_uv=0`, `non_manifold=0` |
| **LOD1** | **Tổng cộng LOD1** | **`6,057` v / `8,648` t** | **`12,506` v / `14,860` p / `20,238` t** | 3 materials | `(-4.990, -4.011, -0.041)` | `(4.520, 4.185, 9.368)` | Scale `(1,1,1)`, Rot `(0,0,0)` |
| **LOD2** | `WT_MatureTree_Roots_LOD2` | *(Gộp trong TrunkAndRoots_LOD2: 204 v / 348 t)* | `126` v / `144` p / **`181`** t | `M_WT_Bark` | `(-3.488, -2.930, -0.041)` | `(2.697, 2.468, 0.475)` | `zero_area=0`, `deg_uv=0`, `loose=0` |
| **LOD2** | `WT_MatureTree_Trunk_Branches_LOD2` | *(Branches_LOD2 riêng: 374 v / 614 t)* | `511` v / `575` p / **`899`** t | `M_WT_Bark` | `(-3.452, -2.383, 0.475)` | `(3.054, 2.576, 8.167)` | `zero_area=0`, `deg_uv=0`, `loose=0` |
| **LOD2** | `WT_MatureTree_Foliage_LOD2` | `1,914` v / `2,552` t | `4,640` v / `5,568` p / **`7,424`** t | `M_Foliage` | `(-4.667, -4.254, 2.986)` | `(4.981, 4.256, 9.130)` | `zero_area=0`, `deg_uv=0`, `non_manifold=0` |
| **LOD2** | **Tổng cộng LOD2** | **`2,492` v / `3,514` t** | **`5,277` v / `6,287` p / `8,504` t** | 2 materials | `(-4.667, -4.254, -0.041)` | `(4.981, 4.256, 9.130)` | Scale `(1,1,1)`, Rot `(0,0,0)` |

- **Ghi chú tradeoff số tam giác**:
  - Mỗi cụm lá nguồn chuẩn D34 gồm 4 phiến lá khép kín 2 mặt (`64` tris/cụm thay vì `8` tris/phiến đơn ở bản cũ). Để phủ đầy 33 tầng tán của cây trưởng thành cao `9.15 m` và rộng `9.32 m` ở 360° không hở mặt lưng hay dưới tán, `LOD0` sử dụng `484` cụm (`30,976` tris lá, tổng `37,018` tris — nằm đúng giữa Cây Lớn `26,682` tris và Cổ Thụ `73,888` tris). `LOD1` giảm xuống `20,238` tris (`273` cụm) và `LOD2` giảm xuống `8,504` tris (`116` cụm) trong khi vẫn giữ nguyên loài lá, UV và material `M_Foliage`.

---

## 4. Điểm Neo Chức Năng Khuyến Nghị (Mục 4 / R3)

| Tên điểm neo | Vùng gắn | Vai trò chức năng | Tọa độ Blender Local (Z-up, m) | Tọa độ Unity Imported (Y-up, m) |
| :--- | :---: | :--- | :---: | :---: |
| **`RootAnchor`** | `WT_MatureTree_Roots` | Điểm neo vùng rễ / cổ rễ lộ trên mặt đất (có kèm phương án rễ tiền cảnh `(0.12, -0.95, 0.22)` Blender / `(0.12, 0.22, 0.95)` Unity) | `(0.00, 0.00, 0.28)` | `(0.00, 0.28, 0.00)` |
| **`TrunkAnchor`** (`Anchor_TrunkAura`) | `WT_MatureTree_Trunk_Branches` | Điểm neo giữa thân chính xoắn dưới chạc phân nhánh lớn (gắn hào quang cosmetic) | `(0.08, 0.00, 2.40)` | `(0.08, 2.40, 0.00)` |
| **`LeavesAnchor`** (`Anchor_LeavesCenter`) | `WT_MatureTree_Foliage` | Tâm thể tích của hệ vòm tán nhiều tầng (gắn hạt tăng trưởng / lá rụng; tách biệt rõ với điểm sét đánh) | `(0.00, 0.00, 5.45)` | `(0.00, 5.45, 0.00)` |
| **`Anchor_LightningTarget`** | `WT_MatureTree_Foliage` (Upper Crown) | Điểm sét đánh tại vòm tán đỉnh (cao hơn `LeavesAnchor` $3.0\text{ m}$ và nằm trong khối tán đỉnh dưới mốc $9.15\text{ m}$) | `(0.00, 0.00, 8.45)` | `(0.00, 8.45, 0.00)` |

---

## 5. Kiểm tra FBX Roundtrip Độc Lập và Danh mục Bằng chứng Hình ảnh (Mục 5)

1. **Kiểm chứng Reimport FBX trong Scene Blender Trắng (`read_homefile(use_empty=True)`)**:
   - Cả 3 file `ArtSource/Exports/WT_MatureTree_LOD0.fbx`, `WT_MatureTree_LOD1.fbx`, `WT_MatureTree_LOD2.fbx` khi nạp vào scene trắng đều chỉ chứa đúng **3 mesh objects** tương ứng (`WT_MatureTree_Roots*`, `WT_MatureTree_Trunk_Branches*`, `WT_MatureTree_Foliage*`), không lẫn đèn, camera hay mặt phẳng nền.
   - Số đỉnh, đa giác, tam giác, `uv_hashes` (`Float2`) và danh sách material slot khớp 100% với `.blend` đã lưu; sai số đỉnh thế giới tối đa $\le 1.91 \times 10^{-6}\text{ m}$ (chỉ do chuyển trục float32 Z-up $\leftrightarrow$ Y-up của định dạng FBX); `zero_area_faces = 0`, `degenerate_uv_triangles = 0`.
2. **Danh mục ảnh bằng chứng đã kết xuất và kiểm tra trực tiếp tại `Evidence/Mature_Leaf_Parts_Fix_20261007/`**:
   - **Toàn cây `LOD0` (8 góc × Material Preview + Solid)**: `full_tree_views/material/` và `full_tree_views/solid/` (`front.png`, `back.png`, `left.png`, `right.png`, `3quarter_front.png`, `3quarter_back.png`, `top.png`, `low_underside.png`).
   - **Tách riêng từng phần (`Roots`, `Trunk_Branches`, `Foliage`, `Wood Assembled`, `Full Assembled`) & Cận cảnh cổ rễ (`Collar Closeup`) cho cả 3 LOD**: `isolated_parts/LOD0/`, `isolated_parts/LOD1/`, `isolated_parts/LOD2/`.
   - **So sánh trực tiếp cụm lá mẫu (`Tree_Large` vs `WT_MatureTree`)**: `leaf_sprig_comparison/material/` và `leaf_sprig_comparison/solid/` (`sprig_compare_top_front.png`, `sprig_compare_side.png`, `sprig_compare_underside.png`).
   - **Lineup đúng tỷ lệ (`Tree_Large` — `WT_MatureTree` — `Bonsai_Trunk_Candidate_v01`) & Crop cận tán**: `progression_lineup/` (`lineup_true_scale_front.png`, `lineup_true_scale_perspective.png`, `crop_large_canopy.png`, `crop_mature_canopy.png`, `crop_ancient_canopy.png`).
   - **Render từ FBX Reimport trong Scene Trắng (`LOD0`, `LOD1`, `LOD2`)**: `fbx_roundtrip_renders/LOD0/`, `LOD1/`, `LOD2/` (`front.png`, `3quarter_front.png`, `3quarter_back.png`, `low_underside.png`).
