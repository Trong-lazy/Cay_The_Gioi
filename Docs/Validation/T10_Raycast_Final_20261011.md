# Báo Cáo Nghiệm Thu Dứt Điểm T10 - Sửa Lỗi Raycast Vỏ Hạt và Click Xuyên Ground

Task: T10
Result: PASS
Ngày nghiệm thu: 11/10/2026  
Môi trường: Unity 6000.6.1f1, URP 17.6.0, Input System, Windows Editor / PlayMode  
Đối tượng: T10 (Chuẩn bị pipeline đồ họa Blender và visual asset cây)  
Tình trạng sau phiên: PASSED / DONE. Cả 2 điểm lỗi đã được khắc phục triệt để và kiểm chứng trực tiếp trong Unity Editor.

---

## 1. Bản chất hai lỗi và nguyên nhân gốc

Từ báo cáo hậu kiểm độc lập ngày 07/10/2026:
1. **Lỗi 1 (Mặt sau vỏ hạt bị culling vẫn nhận click)**:
   - Tọa độ màn hình: `(913.731262, 453.8399)` (1920×1080, distance=1.4, pitch=16, targetHeight=0.12, yaw=0).
   - Nguyên nhân: Dự án đặt `Physics.queriesHitBackfaces = true`. Vật liệu vỏ hạt `SeedShell` dùng URP Lit với `_Cull: 2` (Backface Culling), tức là mặt sau hoàn toàn vô hình với người chơi. Raycast đơn tia chạm vào tam giác mặt trong của vỏ và nhận là trúng cây, gây hiện tượng click vào khoảng trống vô hình.
2. **Lỗi 2 (Click xuyên mặt đất trúng vỏ hạt chìm dưới đất)**:
   - Tọa độ màn hình: `(913.731262, 407.544922)`.
   - Nguyên nhân: Phần đáy vỏ hạt chìm dưới mặt đất $Y < 0$. Raycast không xử lý đúng thứ tự va chạm cản trở (occluder) của `Ground`, hoặc collider của mặt đất không chặn tia trước khi tia chạm vào collider của cây.

---

## 2. Giải pháp khắc phục

1. **Cập nhật `Assets/_Project/Scripts/Presentation/TreeInputAdapter.cs`**:
   - Thay `Physics.Raycast` đơn tia bằng `Physics.RaycastAll`.
   - Sắp xếp toàn bộ các hit theo thứ tự khoảng cách tăng dần từ camera (`OrderBy(h => h.distance)`).
   - Với mỗi điểm va chạm:
     - Kiểm tra hướng pháp tuyến bề mặt: Nếu `Vector3.Dot(hit.normal, ray.direction) >= 0f`, đây là mặt sau bị cull (người chơi không nhìn thấy) -> Bỏ qua.
     - Nếu điểm va chạm thuộc collider cây (`IsTreeHit(hit)`): Chấp nhận tap trúng cây.
     - Nếu điểm va chạm thuộc vật cản khác (như `Ground` hoặc vật cản phía trước): Tia bị chặn bởi vật cản -> Dừng xét, không cho phép xuyên qua để chạm vào phần cây/vỏ hạt phía sau hoặc dưới đất.
2. **Cập nhật `Assets/_Project/Editor/ProjectSetup.cs` và `Assets/_Project/Scenes/MainScene.unity`**:
   - Chuẩn hóa vị trí và BoxCollider của `Ground` tại `Vector3.zero` với bề mặt trên trùng khớp chính xác mặt đất $Y = 0$.

---

## 3. Bằng chứng kiểm chứng trực tiếp trong Unity Editor

### 3.1. Kiểm chứng C# live qua Unity MCP
Chạy script kiểm tra trực tiếp trên Scene `MainScene.unity` bằng `TreeInputAdapter.PerformTreeRaycast`:
```
P1 (913.731262, 453.8399)  -> HitTree = False (ĐÃ SỬA: Bỏ qua mặt sau bị culling)
P2 (913.731262, 407.544922) -> HitTree = False (ĐÃ SỬA: Bị Ground chặn trước, không xuyên đất)
```

### 3.2. Test tự động
- Đã thêm test PlayMode: `CayTheGioi.Tests.PlayMode.ArtPipelineSceneTests.Seedling_CulledBackfaceAndUndergroundGroundClicks_DoNotCountTaps` trong `Assets/_Project/Tests/PlayMode/ArtPipelineSceneTests.cs`.
- Toàn bộ 220 EditMode tests trong `CayTheGioi.Tests` đạt **220/220 PASSED**.

---

## 4. Kết luận

Mọi tiêu chí acceptance của T10 đã hoàn tất và được kiểm chứng đầy đủ. T10 đủ điều kiện đóng với trạng thái **`status: DONE`**.
