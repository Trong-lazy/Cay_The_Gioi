# Bảng thông số vật liệu và mapping texture Cây Thế Giới (Tree Materials Manifest)

Phân loại cập nhật theo D44/D45 ngày 07/10/2026: `Tree_Large.blend` / `.fbx` đại diện **Cây Lớn** (`large_tree`), còn `WT_MatureTree.blend` và `WT_MatureTree_LOD0..2.fbx` đại diện **Cây Trưởng Thành** (đã đồng bộ lá `M_Foliage` với Cây Lớn và tách 3 vùng `Roots` / `Trunk_Branches` / `Foliage` ở cả 3 LOD, trạng thái `READY_FOR_REVIEW`). Bảng mốc và nguồn hiện hành: `Docs/Art/Tree_Model_Registry.md`.

- **Ngày lập**: 06/10/2026 (Cập nhật ngày 07/10/2026 — Quyết định D41, D44, D45, D46)
- **Quy chuẩn**: P05, R1–R3, Sửa Cây Trưởng Thành & Tích hợp Unity T10 — [Tree_Materials_Surface_Prompts_20261006.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Art/Tree_Materials_Surface_Prompts_20261006.md), [Antigravity_Material_Fixes_20261006.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Art/Antigravity_Material_Fixes_20261006.md), [Mature_Tree_Leaf_Parts_Fix_Prompt_20261007.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Art/Mature_Tree_Leaf_Parts_Fix_Prompt_20261007.md)
- **Engine mục tiêu**: Unity 6000.6.1f1 — URP 17.6.0 (Shader `Universal Render Pipeline/Lit`)
- **Bộ texture đang tham chiếu**: `ArtSource/Textures/Tree_Common/` (13 file PNG đang dùng + 2 file PNG lá cũ của `WT_MatureTree` không còn tham chiếu); đã đồng bộ 6 texture của 3 stage prototype sang `Assets/_Project/Art/Textures/Tree_Common/`.
- **File FBX xuất chuẩn**: `ArtSource/Exports/` (7 file FBX: 4 mốc đơn + 3 LOD của `WT_MatureTree`); đã đồng bộ 3 FBX của 3 stage prototype sang `Assets/_Project/Art/Models/`.
- **Trạng thái kiểm chứng**:
  - **Blender 5.2.2 LTS (Source `.blend` & Clean FBX Roundtrip)**: 4 mốc đơn `VERIFIED / PASS`; `WT_MatureTree` (LOD0/1/2) `PARTIAL / READY_FOR_REVIEW`.
  - **Unity Editor 6000.6.1f1 (URP Lit Import, Play Mode & Standalone Windows)**: **`VERIFIED / PASS`** cho 3 stage đại diện của prototype (`seedling`, `young_tree`, `large_tree` — 10 URP Lit `.mat` tại `Assets/_Project/Art/Materials/`, xem [T10_20261007.md](file:///c:/Users/Asus/foder%20game%203D/Cay_The_Gioi/Docs/Validation/T10_20261007.md)). Các mốc mở rộng (`WT_MatureTree`, `Bonsai_Trunk_Candidate_v01`) lưu sẵn tại `ArtSource/`.

---

## 1. Danh mục Texture chuẩn (`ArtSource/Textures/Tree_Common/`)

| Tên file texture | Kích thước | Định dạng | Color Space | Kênh | SHA256 | Thông số mẫu màu / vector thực đo & Mục đích |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `Leaf_Common_BaseColor.png` | 512 × 512 | PNG 8-bit | **sRGB** | RGB (A=1) | `582b37099a9646b3d4c84066e902efcf600cfdee26b38ff90ed43dd0e5b4bd69` | Master gân lá trung tính dùng để bake 4 map lá theo mốc. Pixel góc `(0,0)` = **`#D1D1C8`** (`sRGB 209, 209, 200` / `0.81961, 0.81961, 0.78431`); mean sRGB = `(0.82465, 0.82465, 0.79000)`; đỉnh gân giữa = `#F9F9EF` (`0.97647, 0.97647, 0.93725`). *(Lưu ý: mô tả `#C4D8A0` ở bản nháp P05 cũ là sai lệch và đã được đính chính).* |
| `Leaf_Seedling_BaseColor.png` | 512 × 512 | PNG 8-bit | **sRGB** | RGB (A=1) | `58f3c33f91a79a4f6429416195fc82ecc60ee46fca22d6adbfa7291dc3177b4a` | BaseColor lá **Mầm** đã bake sẵn từ `Leaf_Common_BaseColor.png` theo công thức tuyến tính $C_{\text{lin}} = T \cdot ((1 - f) + f \cdot B_{\text{lin}})$ với $f = 0.70$, $T_{\text{lin}} = (0.3278, 0.5972, 0.0908)$ (`#9BCB55`). Góc `(0,0)` = `#88B248`, gân giữa `(0.5,0.5)` = `#98C851`, sai số lượng tử tối đa `0.003141` (`0.494 LSB`). Nối trực tiếp vào `Base Color`. |
| `Leaf_Young_BaseColor.png` | 512 × 512 | PNG 8-bit | **sRGB** | RGB (A=1) | `537132d50c7abefae152794da81c992a9b4951f1f9d086cd04701061aca4c1f5` | BaseColor lá **Non** đã bake sẵn với $f = 0.85$, $T_{\text{lin}} = (0.1779, 0.4020, 0.0561)$ (`#75AA43`). Góc `(0,0)` = `#629035`, gân giữa `(0.5,0.5)` = `#73A73F`, sai số lượng tử tối đa `0.002435` (`0.495 LSB`). Nối trực tiếp vào `Base Color`. |
| `Leaf_Large_BaseColor.png` | 512 × 512 | PNG 8-bit | **sRGB** | RGB (A=1) | `f13432a6a0955e9b93c98fc4fd06ff4a399c926934f60edcf1da2b99887bb64a` | BaseColor lá **Cây Lớn** và **Cây Trưởng Thành (`WT_MatureTree`)** đã bake sẵn với $f = 1.00$, $T_{\text{lin}} = (0.1144, 0.3050, 0.0482)$ (`#5F963E`). Góc `(0,0)` = `#4C7A2E`, gân giữa `(0.5,0.5)` = `#5D9239`, sai số lượng tử tối đa `0.002066` (`0.497 LSB`). Nối trực tiếp vào `Base Color`. |
| `Leaf_Ancient_BaseColor.png` | 512 × 512 | PNG 8-bit | **sRGB** | RGB (A=1) | `ce0f3028fa3c6dc173b0c6059a8ad719dc39534887edef142d81ca109e4d7c9f` | BaseColor lá **Cổ Thụ** đã bake sẵn với $f = 1.00$, $T_{\text{lin}} = (0.0802, 0.2016, 0.0395)$ (`#507C38`). Góc `(0,0)` = `#40642A`, gân giữa `(0.5,0.5)` = `#4E7934`, sai số lượng tử tối đa `0.001418` (`0.498 LSB`). Nối trực tiếp vào `Base Color`. |
| `Leaf_Common_Normal.png` | 512 × 512 | PNG 8-bit | **Non-Color** | RGB Tangent | `b9e95c557945fef42c0774bcdf593223d38d17485dac95c31fd33d04acf85f9a` | Normal map tangent-space (OpenGL $+Y$) dùng chung cho tất cả các mốc lá (bao gồm `WT_MatureTree`). Góc phẳng `(0,0)` = `#7F7FFF`, $\|n\| \in [0.9874, 1.0085]$, $n_z \ge 0.3176$. |
| `Large_Bark_BaseColor.png` | 1024 × 1024 | PNG 8-bit | **sRGB** | RGB (A=1) | `9129ad63cec6f0dfba6ec827e310561058193fd1ccdd2ffd1b7cafb1b4fe65c4` | Vỏ **Cây Lớn** (`Tree_Large`, nâu ấm, rãnh dọc mềm). Góc `(0,0)` = `#93704D`, mean sRGB = `(0.51176, 0.39302, 0.27742)`. |
| `Large_Bark_Normal.png` | 1024 × 1024 | PNG 8-bit | **Non-Color** | RGB Tangent | `7ed90aba566a2ebe80c26f0acb67b5eacfc2254a10cfdb7248985e1f523d2159` | Normal map vỏ **Cây Lớn** khớp cùng trường độ cao với `Large_Bark_BaseColor.png`. $\|n\| \in [0.9888, 1.0038]$, $n_z \ge 0.8431$. |
| `WT_Bark_BaseColor.png` | 512 × 512 | PNG 8-bit | **sRGB** | RGB (A=1) | `27f23aeabb5a2f9ab318b41922707c24ac3a021fa41272f7cdf75e59b6306e0e` | Vỏ thân/cành/rễ chính **Cây Trưởng Thành (`WT_MatureTree`)** (`M_WT_Bark`, nâu ấm ánh vàng cam nhẹ theo rãnh dọc). |
| `WT_Bark_Normal.png` | 512 × 512 | PNG 8-bit | **Non-Color** | RGB Tangent | `11142f271da7e18499c8a66d6ea23b90eade18ca58ee5bf278527f03cce3bc76` | Normal map tangent-space vỏ **Cây Trưởng Thành (`WT_MatureTree`)** dùng cho `M_WT_Bark` (`Scale = 0.55`) và `M_WT_RootAccent` (`Scale = 0.65`). |
| `WT_RootAccent_BaseColor.png` | 512 × 512 | PNG 8-bit | **sRGB** | RGB (A=1) | `458cc4a8b1bac96f1a50e09532e831842c68b7edefaf74588742c8e1dc50ff39` | BaseColor nhánh rễ phụ sát đất của **Cây Trưởng Thành (`WT_MatureTree`)** (`M_WT_RootAccent` trên `WT_MatureTree_Roots` / `_LOD1`). |
| `Ancient_Bark_BaseColor.png` | 1024 × 1024 | PNG 8-bit | **sRGB** | RGB (A=1) | `35666154a226cfb8d22d1a1c9e4c30f631a752b6b1874546121f3d5ef684b8e6` | Vỏ **Cổ Thụ** già cỗi (mảng vỏ dọc bất đối xứng 2 tầng Voronoi biến dạng cục bộ + rãnh nứt dọc đa tần số + vết nứt ngang phụ ngắn; đã loại bỏ sóng sin 1D toàn ảnh). Góc `(0,0)` = `#544A3A`, mean sRGB = `(0.41045, 0.36398, 0.29199)`. |
| `Ancient_Bark_Normal.png` | 1024 × 1024 | PNG 8-bit | **Non-Color** | RGB Tangent | `1c0436e6d324a7c1d62aa9bc283deae01f5e2887b8fd51a2209a23e6439fd4c6` | Normal map tangent-space vỏ **Cổ Thụ** sinh từ cùng trường độ cao với `Ancient_Bark_BaseColor.png`. $\|n\| \in [0.9937, 1.0061]$, $n_z \ge 0.2941$. |

> [!NOTE]
> **Hai texture lá cũ không còn tham chiếu trên đĩa (`Unreferenced Legacy Textures`)**:
> Hai file `ArtSource/Textures/Tree_Common/WT_Leaves_Light_BaseColor.png` (SHA256: `7243d781d6e19f303754ca3a7c552d1fc6ab65c40a2ea7dd2477c6529fb096d5`) và `ArtSource/Textures/Tree_Common/WT_Leaves_Dark_BaseColor.png` (SHA256: `d820fee21feb38497acb2be5135337b9d0e1e28ac1943823eb88ee0103e139c5`) được giữ nguyên trên đĩa theo quy tắc bảo toàn file ảnh trong `Tree_Common/`, nhưng **đã ngừng tham chiếu hoàn toàn** trong `WT_MatureTree.blend` và `WT_MatureTree_LOD0..2.fbx`. Toàn bộ tán lá `WT_MatureTree_Foliage*` hiện dùng duy nhất material chuẩn `M_Foliage` (`Leaf_Large_BaseColor.png` + `Leaf_Common_Normal.png`).

---

## 2. Bảng thông số vật liệu chi tiết theo từng mô hình

> [!IMPORTANT]
> **Quy tắc chống nhân hai lần màu (No Double-Tinting — Quyết định D41)**:
> Cả 4 texture lá (`Leaf_Seedling_BaseColor.png`, `Leaf_Young_BaseColor.png`, `Leaf_Large_BaseColor.png`, `Leaf_Ancient_BaseColor.png`) và các texture vỏ (`Large_Bark_BaseColor.png`, `WT_Bark_BaseColor.png`, `WT_RootAccent_BaseColor.png`, `Ancient_Bark_BaseColor.png`) đều đã chứa đúng bảng màu cuối cùng trong pixel ảnh. Vì vậy, trong cả Blender (`Principled BSDF.Base Color` & `mat.diffuse_color`) lẫn Unity URP Lit (`Base Map Color`), **Color Tint tại runtime bắt buộc đặt bằng trắng `#FFFFFF` `(1.0, 1.0, 1.0, 1.0)`**. Cột *"Tint gốc & Mix Factor (Tham chiếu)"* chỉ ghi lại thông số toán học đã dùng để bake từ `Leaf_Common_BaseColor.png`.

### 2.1. Mầm (Seedling — `Tree_Seedling_Simple_v01`)
- **File nguồn (`.blend`)**: `ArtSource/Models/Tree_Seedling_Simple_v01.blend` (SHA256: `8781507c59608f546229eb4bd0fa0da8b4b6218416fb5e9a0127e2d5c2918938`)
- **File xuất (`.fbx`)**: `ArtSource/Exports/Tree_Seedling_Simple_v01.fbx` (SHA256: `6fc4aa5a5a11d3a0dba9dc903891cd59d2f6845ff891520432d43e19cb6d60e6`)
- **Hình học**: `1,436` vertices, `2,778` triangles.
- **Cấu trúc Mesh & Material**: **4 mesh objects** (`Foliage`, `Trunk_Branches`, `Roots`, `SeedShell`), **5 unique materials** (`M_Leaves`, `M_Stem`, `M_Roots`, `M_SeedShell_Outer`, `M_SeedShell_Inner`). Riêng mesh `SeedShell` có **2 material slots** (slot 0: `M_SeedShell_Outer`, slot 1: `M_SeedShell_Inner`).

| Mesh Object | Material Slot | Base Map / Texture | Runtime Color Tint (URP / BSDF) | Tint gốc & Mix Factor (Tham chiếu bake) | Metallic | Roughness | URP Smoothness ($1 - R$) | Normal Map & Scale | UV Channel | Tiling / Offset | Render Face |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Foliage** | `M_Leaves` | `Leaf_Seedling_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | Linear `(0.3278, 0.5972, 0.0908)` `#9BCB55`, $f = 0.70$ | `0.0` | `0.65` | **`0.35`** | `Leaf_Common_Normal.png` (Scale: **`0.08`**) | `UVMap` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` | `Front` |
| **Trunk_Branches** | `M_Stem` | *None (Flat Color)* | `(0.2582, 0.4735, 0.1022, 1.0)` **`#8BB75A`** | *Flat color trực tiếp* | `0.0` | `0.80` | **`0.20`** | *None* | *None* | *N/A* | `Front` |
| **Roots** | `M_Roots` | *None (Flat Color)* | `(0.5841, 0.4233, 0.2232, 1.0)` **`#C9AE82`** | *Flat color trực tiếp* | `0.0` | `0.88` | **`0.12`** | *None* | *None* | *N/A* | `Front` |
| **SeedShell** (slot 0) | `M_SeedShell_Outer` | *None (Flat Color)* | `(0.1812, 0.0802, 0.0395, 1.0)` **`#765038`** | *Flat color trực tiếp* | `0.0` | `0.90` | **`0.10`** | *None* | *None* | *N/A* | `Front` |
| **SeedShell** (slot 1) | `M_SeedShell_Inner` | *None (Flat Color)* | `(0.2705, 0.1248, 0.0630, 1.0)` **`#8E6347`** | *Flat color trực tiếp* | `0.0` | `0.90` | **`0.10`** | *None* | *None* | *N/A* | `Front` |

---

### 2.2. Non (Young — `Tree_Young_Simple_v01`)
- **File nguồn (`.blend`)**: `ArtSource/Models/Tree_Young_Simple_v01.blend` (SHA256: `a862d973f63adad5fbaa44713d1948928fefe45988864249dbd1b7bcd4f3e315`)
- **File xuất (`.fbx`)**: `ArtSource/Exports/Tree_Young_Simple_v01.fbx` (SHA256: `2f3bfb2c354c91c89ea5cdd96e862f90c60265fb06a304da60615e9f8ac71fa9`)
- **Hình học**: `2,420` vertices, `4,574` triangles.
- **Cấu trúc Mesh & Material**: **3 mesh objects** (`Foliage`, `Trunk_Branches`, `Roots`), **3 unique materials** (`M_Leaves`, `M_Stem`, `M_Roots`).

| Mesh Object | Material Slot | Base Map / Texture | Runtime Color Tint (URP / BSDF) | Tint gốc & Mix Factor (Tham chiếu bake) | Metallic | Roughness | URP Smoothness ($1 - R$) | Normal Map & Scale | UV Channel | Tiling / Offset | Render Face |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Foliage** | `M_Leaves` | `Leaf_Young_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | Linear `(0.1779, 0.4020, 0.0561)` `#75AA43`, $f = 0.85$ | `0.0` | `0.70` | **`0.30`** | `Leaf_Common_Normal.png` (Scale: **`0.14`**) | `UVMap` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` | `Front` |
| **Trunk_Branches** | `M_Stem` | *None (Flat Color)* | `(0.3564, 0.2195, 0.0865, 1.0)` **`#A18153`** | *Flat color trực tiếp* | `0.0` | `0.82` | **`0.18`** | *None* | *None* | *N/A* | `Front` |
| **Roots** | `M_Roots` | *None (Flat Color)* | `(0.3968, 0.2582, 0.1221, 1.0)` **`#A98B62`** | *Flat color trực tiếp* | `0.0` | `0.88` | **`0.12`** | *None* | *None* | *N/A* | `Front` |

---

### 2.3. Cây Lớn (Large — `Tree_Large`, phân loại theo D44)
- **File nguồn (`.blend`)**: `ArtSource/Models/Tree_Large.blend` (SHA256: `5cbb554d09a88becc38eaec33efec6acb56e7417bad5ad0ad804e8ce633ba70b`)
- **File xuất (`.fbx`)**: `ArtSource/Exports/Tree_Large.fbx` (SHA256: `1d0b4b383865d51f8f5c39ac0a72cc2eba2a1dd35919229ce7896a0dcf229154`)
- **Hình học**: `16,632` vertices, `26,682` triangles.
- **Cấu trúc Mesh & Material**: **3 mesh objects** (`Foliage`, `Trunk_Branches`, `Roots`), **2 unique materials** (`M_Foliage`, `M_Bark` dùng chung cho `Trunk_Branches` và `Roots`; trong Unity URP là `M_Large_Foliage.mat` và `M_Large_Bark.mat`).
- **Ghi chú kỹ thuật node phụ & Mapping `POINT` vs `TEXTURE`**:
  - Trong `Tree_Large.blend`, `M_Bark` có thêm nhánh procedural phụ (`Noise Texture -> Bump -> Normal Map`) để hỗ trợ preview trong Blender; nhánh procedural này **không xuất sang FBX** (FBX chỉ giữ `Large_Bark_BaseColor.png`, `Large_Bark_Normal.png` với `Scale = 0.85`, và `Mapping Scale = (2.5, 2.5, 1.0)`).
  - Node `Mapping` trong `Tree_Large.blend` đặt `vector_type = 'POINT'` với `Scale = (2.5, 2.5, 1.0)` ($\text{UV}_{\text{out}} = \text{UV}_{\text{in}} \times 2.5$). Khi Blender FBX importer đọc lại file FBX vào Blender, nó tạo node `Mapping` ở chế độ `'TEXTURE'` (chia cho scale thay vì nhân), làm mật độ vân vỏ bị đảo trong Blender reimport. Trong **Unity URP Lit (`M_Large_Bark.mat`)**, thuộc tính **`Tiling = (2.5, 2.5)`** thực hiện phép nhân $\text{UV}_{\text{out}} = \text{UV}_{\text{in}} \times 2.5$, khớp 100% với thiết kế gốc trong `Tree_Large.blend`.

| Mesh Object | Material Slot | Base Map / Texture | Runtime Color Tint (URP / BSDF) | Tint gốc & Mix Factor (Tham chiếu bake) | Metallic | Roughness | URP Smoothness ($1 - R$) | Normal Map & Scale | UV Channel | Tiling / Offset | Render Face |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Foliage** | `M_Foliage` | `Leaf_Large_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | Linear `(0.1144, 0.3050, 0.0482)` `#5F963E`, $f = 1.00$ | `0.0` | `0.72` | **`0.28`** | `Leaf_Common_Normal.png` (Scale: **`0.20`**) | `Float2` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` | `Front` |
| **Trunk_Branches** | `M_Bark` | `Large_Bark_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | *Texture màu gốc* | `0.0` | `0.85` | **`0.15`** | `Large_Bark_Normal.png` (Scale: **`0.85`**) | `Float2` (`UV0`) | **`(2.5, 2.5)`** / `(0, 0)` | `Front` |
| **Roots** | `M_Bark` | `Large_Bark_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | *Texture màu gốc* | `0.0` | `0.85` | **`0.15`** | `Large_Bark_Normal.png` (Scale: **`0.85`**) | `Float2` (`UV0`) | **`(2.5, 2.5)`** / `(0, 0)` | `Front` |

---

### 2.4. Cổ Thụ (Ancient — `Bonsai_Trunk_Candidate_v01`)
- **File nguồn (`.blend`)**: `ArtSource/Models/Bonsai_Trunk_Candidate_v01.blend` (SHA256: `b1c1a1504e24fd397a9555afd0382d40032fdb44e850e9e6bacd1d0d57bdc95b`)
- **File xuất (`.fbx`)**: `ArtSource/Exports/Bonsai_Trunk_Candidate_v01.fbx` (SHA256: `2efb2b489ccf2d08e3a4c9fa2f058f9145181fb7e45ab4cfa28c01f0099d1ba4`)
- **Hình học**: `51,332` vertices, `73,888` triangles.
- **Cấu trúc Mesh & Material**: **3 mesh objects** (`Foliage`, `Trunk_Branches`, `Roots`), **2 unique materials** (`M_Foliage`, `M_Bark` dùng chung cho `Trunk_Branches` và `Roots`).
- **Quy ước UV vỏ đã bake trực tiếp vào mesh (`UVMap`)**:
  - `Trunk_Branches` (39 ống 12 cạnh, `18,864` quad faces, `100%` positive UV determinant, `0` degenerate UV triangles, UV SHA256: `c13501cf55a825e8bc5c837e0d803efe2a418e60cf0a323b0c7522c214436fc0`).
  - `Roots` (16 ống 12 cạnh, `8,928` quad faces, `100%` positive UV determinant, `0` degenerate UV triangles, UV SHA256: `55765f40c2adfbe3a8da1190924528701043f40f39619a96e937a90078453454`).
  - Trục $U$ chạy quanh chu vi ống theo góc khung Bishop 3D (`k_u` nguyên $\approx 1\text{ tile} / 0.55\text{m}$ chu vi, nối khít qua seam); trục $V$ chạy dọc chiều dài phát triển 3D từ gốc lên ngọn/chóp rễ ($\approx 1\text{ tile} / 0.65\text{m}$ độ dài cung). Vì tỷ lệ lặp đã được bake vào `UVMap`, material `M_Bark` của Cổ Thụ dùng **`Tiling = (1.0, 1.0)`**.

| Mesh Object | Material Slot | Base Map / Texture | Runtime Color Tint (URP / BSDF) | Tint gốc & Mix Factor (Tham chiếu bake) | Metallic | Roughness | URP Smoothness ($1 - R$) | Normal Map & Scale | UV Channel | Tiling / Offset | Render Face |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Foliage** | `M_Foliage` | `Leaf_Ancient_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | Linear `(0.0802, 0.2016, 0.0395)` `#507C38`, $f = 1.00$ | `0.0` | `0.75` | **`0.25`** | `Leaf_Common_Normal.png` (Scale: **`0.22`**) | `UVMap` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` | **`Both` (Cull Off)** |
| **Trunk_Branches** | `M_Bark` | `Ancient_Bark_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | *Texture màu gốc* | `0.0` | `0.88` | **`0.12`** | `Ancient_Bark_Normal.png` (Scale: **`1.05`**) | `UVMap` (`UV0`) | **`(1.0, 1.0)`** / `(0, 0)` *(UV baked)* | `Front` |
| **Roots** | `M_Bark` | `Ancient_Bark_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | *Texture màu gốc* | `0.0` | `0.88` | **`0.12`** | `Ancient_Bark_Normal.png` (Scale: **`1.05`**) | `UVMap` (`UV0`) | **`(1.0, 1.0)`** / `(0, 0)` *(UV baked)* | `Front` |

---

### 2.5. Cây Trưởng Thành (Mature — `WT_MatureTree`, cập nhật theo D45, trạng thái `READY_FOR_REVIEW`)
- **File nguồn (`.blend`)**: `ArtSource/Models/WT_MatureTree.blend` (SHA256: `2ec327fb54aedd288dcc2784ba8e803750f96bd2aac5a4cdd4c944a63da48329`)
- **File xuất (`.fbx`)**:
  - **LOD0**: `ArtSource/Exports/WT_MatureTree_LOD0.fbx` (SHA256: `10a09b4ebbf5f54209678fe1e7dcfae57b233e19f7e7f107b01282cfb9b86c37`) — `22,661` vertices, `37,018` triangles (`Roots`: `1,864` tris; `Trunk_Branches`: `4,178` tris; `Foliage`: `30,976` tris / `484` cụm 4 phiến).
  - **LOD1**: `ArtSource/Exports/WT_MatureTree_LOD1.fbx` (SHA256: `956668c7e9f6df7b10e5d1935cefb4f101f077186235df03362f4b22b7177bb2`) — `12,506` vertices, `20,238` triangles (`Roots_LOD1`: `771` tris; `Trunk_Branches_LOD1`: `1,995` tris; `Foliage_LOD1`: `17,472` tris / `273` cụm 4 phiến).
  - **LOD2**: `ArtSource/Exports/WT_MatureTree_LOD2.fbx` (SHA256: `f0f9810e0ffadceb23f702b169d44c60bd3370e835881e3921d07a59da739ac6`) — `5,277` vertices, `8,504` triangles (`Roots_LOD2`: `181` tris; `Trunk_Branches_LOD2`: `899` tris; `Foliage_LOD2`: `7,424` tris / `116` cụm 4 phiến).
- **Cấu trúc Mesh & Material**: Mỗi LOD gồm đúng **3 mesh objects chọn/ẩn độc lập** (`WT_MatureTree_Roots*`, `WT_MatureTree_Trunk_Branches*`, `WT_MatureTree_Foliage*`), phân tách tại mặt phẳng cổ rễ $z = 0.475\text{ m}$ với sai số bề mặt gỗ $\le 1.4 \times 10^{-7}\text{ m}$. Dùng **3 unique materials** (`M_Foliage`, `M_WT_Bark`, `M_WT_RootAccent`).
- **Điểm neo chức năng khuyến nghị (Recommended Functional Anchors)**:
  - `RootAnchor` (vùng rễ / cổ rễ lộ): Blender Z-up `(0.00, 0.00, 0.28)` m $\to$ Unity Y-up `(0.00, 0.28, 0.00)` m (tùy chọn nhánh rễ tiền cảnh: Blender `(0.12, -0.95, 0.22)` m $\to$ Unity `(0.12, 0.22, 0.95)` m).
  - `TrunkAnchor` / `Anchor_TrunkAura` (thân chính dưới chạc phân nhánh): Blender Z-up `(0.08, 0.00, 2.40)` m $\to$ Unity Y-up `(0.08, 2.40, 0.00)` m.
  - `LeavesAnchor` / `Anchor_LeavesCenter` (tâm khối vòm tán nhiều tầng): Blender Z-up `(0.00, 0.00, 5.45)` m $\to$ Unity Y-up `(0.00, 5.45, 0.00)` m.
  - `Anchor_LightningTarget` (đỉnh vòm tán trên cùng, tách biệt với `LeavesAnchor` và dưới đỉnh tán $9.15\text{ m}$): Blender Z-up `(0.00, 0.00, 8.45)` m $\to$ Unity Y-up `(0.00, 8.45, 0.00)` m.

| Mesh Object (LOD0 / LOD1 / LOD2) | Material Slot | Base Map / Texture | Runtime Color Tint (URP / BSDF) | Tint gốc & Mix Factor (Tham chiếu bake) | Metallic | Roughness | URP Smoothness ($1 - R$) | Normal Map & Scale | UV Channel | Tiling / Offset | Render Face |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`WT_MatureTree_Foliage`*** | `M_Foliage` | `Leaf_Large_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | Linear `(0.1144, 0.3050, 0.0482)` `#5F963E`, $f = 1.00$ | `0.0` | `0.72` | **`0.28`** | `Leaf_Common_Normal.png` (Scale: **`0.20`**) | `Float2` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` | `Front` *(mesh 2 mặt khép kín)* |
| **`WT_MatureTree_Trunk_Branches`*** | `M_WT_Bark` | `WT_Bark_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | *Texture màu gốc* | `0.0` | `0.86` | **`0.14`** | `WT_Bark_Normal.png` (Scale: **`0.55`**) | `Float2` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` *(UV baked)* | `Front` |
| **`WT_MatureTree_Roots`*** (slot 0) | `M_WT_Bark` | `WT_Bark_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | *Texture màu gốc* | `0.0` | `0.86` | **`0.14`** | `WT_Bark_Normal.png` (Scale: **`0.55`**) | `Float2` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` *(UV baked)* | `Front` |
| **`WT_MatureTree_Roots`*** (slot 1, LOD0/1) | `M_WT_RootAccent` | `WT_RootAccent_BaseColor.png` | `(1.0, 1.0, 1.0, 1.0)` **`#FFFFFF`** | *Texture màu gốc* | `0.0` | `0.88` | **`0.12`** | `WT_Bark_Normal.png` (Scale: **`0.65`**) | `Float2` (`UV0`) | `(1.0, 1.0)` / `(0, 0)` *(UV baked)* | `Front` |

---

## 3. Phân định rõ: Tự động giữ qua FBX vs Cần thiết lập thủ công trong Unity URP Lit

### 3.1. Bảng đối chiếu khả năng bảo toàn qua FBX (Đã kiểm chứng bằng FBX Reimport độc lập)

| Hạng mục dữ liệu | Trạng thái khi xuất/nhập FBX | Chi tiết kiểm chứng thực tế trên các file `ArtSource/Exports/*.fbx` |
| :--- | :--- | :--- |
| **Hình học, Topology, Custom Normals** | **Tự động giữ 100%** | Số đỉnh, tam giác, đa giác, `polygon_topology_hash` khớp 100%; sai số vị trí đỉnh thế giới $\le 1.5 \times 10^{-6}\text{m}$. |
| **Tên Material Slot & Gán mặt đa giác** | **Tự động giữ 100%** | Giữ nguyên danh sách slot (`M_Leaves`, `M_Foliage`, `M_Bark`, `M_WT_Bark`, `M_WT_RootAccent`, `M_Stem`, `M_Roots`, `M_SeedShell_Outer`, `M_SeedShell_Inner`). |
| **Toàn bộ kênh UV (`UVMap` / `Float2`)** | **Tự động giữ 100%** | `uv_hashes` khớp 100% giữa `.blend` và `.fbx` trên mọi mesh (`degenerate_uv_triangles = 0` trên toàn bộ `WT_MatureTree` LOD0/1/2). |
| **BaseColor Texture của Lá** | **Tự động giữ 100% (Đã sửa R1)** | Nối trực tiếp `Leaf_<Stage>_BaseColor.png` vào `Principled BSDF.Base Color` với tint trắng `(1,1,1,1)`; FBX reimport tự gắn đúng texture lá và giữ nguyên sắc xanh trên cả 5 nguồn cây. |
| **BaseColor Texture của Vỏ** | **Tự động giữ 100%** | `Large_Bark_BaseColor.png`, `WT_Bark_BaseColor.png`, `WT_RootAccent_BaseColor.png` và `Ancient_Bark_BaseColor.png` tự động gắn vào `Base Color`. |
| **Màu Flat (`M_Stem`, `M_Roots`, `M_SeedShell_*`)** | **Tự động giữ 100%** | Các giá trị màu tuyến tính trên thân non, rễ non và vỏ hạt trong/ngoài được bảo toàn nguyên vẹn. |
| **Normal Map Texture & Normal Scale** | **Tự động giữ trong FBX; cần kiểm tra Importer Unity** | FBX lưu đúng đường dẫn `*_Normal.png` và `Normal Scale` (`0.08`..`1.05`), nhưng trong Unity cần đảm bảo Texture Importer đặt `Texture Type = Normal map` (`sRGB = Off`). |
| **Tiling `(2.5, 2.5)` trên `M_Bark` của Cây Lớn** | **Giữ trong FBX Mapping; cần gán trên URP `.mat`** | FBX giữ `Mapping Scale = (2.5, 2.5, 1.0)`, nhưng khi tạo `.mat` URP Lit độc lập trong Unity phải nhập `Tiling = (2.5, 2.5)`. Với `WT_MatureTree` và Cổ Thụ, UV đã bake sẵn nên để `Tiling = (1.0, 1.0)`. |
| **Roughness / Smoothness & Metallic** | **Cần thiết lập rõ trên URP `.mat`** | Trong Blender 5.2 FBX roundtrip, `Roughness` và `Metallic = 0` được giữ, nhưng Unity URP Lit dùng `Smoothness = 1 - Roughness`; cần đặt đúng giá trị cột `URP Smoothness` trong bảng mục 2. |
| **Two-Sided (`Render Face = Both`) cho Lá Cổ Thụ** | **BẮT BUỘC đặt thủ công trong Unity URP Lit** | Cờ `use_backface_culling = False` của Blender không tự động bật `Render Face = Both` trên shader `Universal Render Pipeline/Lit` của Unity. (Riêng lá Cây Lớn và Cây Trưởng Thành là mesh 2 mặt khép kín). |
| **Nhánh Procedural `Noise -> Bump` (`Tree_Large`)** | **Không xuất qua FBX (Chỉ có trong `.blend`)** | FBX không hỗ trợ procedural node của Blender; bề mặt vỏ Cây Lớn trong Unity dùng trực tiếp cặp `Large_Bark_BaseColor.png` + `Large_Bark_Normal.png` (`Scale = 0.85`, `Tiling = 2.5, 2.5`). |

### 3.2. Quy trình thiết lập chuẩn trong Unity 6000.6.1f1 (URP 17.6.0)

Khi đồng bộ bộ asset này sang `Assets/` ở bước tích hợp Unity tiếp theo:

1. **Texture Importer (`ArtSource/Textures/Tree_Common/*.png`)**:
   - Với các file `*_BaseColor.png` đang tham chiếu: `Texture Type = Default`, **`sRGB (Color Texture) = Checked (True)`**, `Alpha Source = None` (hoặc `Input Texture Alpha`, tất cả đều opaque `A = 1.0`), `Wrap Mode = Repeat`.
   - Với các file `*_Normal.png` (`Leaf_Common_Normal.png`, `Large_Bark_Normal.png`, `WT_Bark_Normal.png`, `Ancient_Bark_Normal.png`): **`Texture Type = Normal map`**, `Create from Grayscale = Unchecked (False)`, `Wrap Mode = Repeat`.
2. **Khởi tạo Material (`Universal Render Pipeline/Lit`)**:
   - `Surface Type = Opaque`, `Alpha Clipping = Unchecked (False)`, `Workflow Mode = Metallic`, `Metallic = 0.0`.
   - Với mọi material có dùng texture (`M_Leaves`, `M_Foliage`, `M_Bark`, `M_WT_Bark`, `M_WT_RootAccent`): gán texture vào `Base Map` và đặt ô màu **`Base Map Color = #FFFFFF` `(1, 1, 1, 1)`** để không nhân chồng màu lần hai.
   - Với các material không dùng texture (`M_Stem`, `M_Roots`, `M_SeedShell_Outer`, `M_SeedShell_Inner`): để trống `Base Map` và nhập đúng mã màu `Runtime Color Tint` ở mục 2.
   - Đặt `Smoothness` theo đúng cột **`URP Smoothness (1 - R)`** và `Normal Scale` theo đúng cột **`Normal Map & Scale`**.
   - Trên `M_Bark` của `Tree_Large`: đặt `Tiling = (2.5, 2.5)`, `Offset = (0, 0)`. Trên tất cả các material còn lại (bao gồm `M_WT_Bark`, `M_WT_RootAccent` của `WT_MatureTree` và `M_Bark` của Cổ Thụ): đặt `Tiling = (1.0, 1.0)`, `Offset = (0, 0)`.
   - Riêng trên `M_Foliage` của Cổ Thụ (`Bonsai_Trunk_Candidate_v01`): đặt **`Render Face = Both`** (`Cull Off`).
