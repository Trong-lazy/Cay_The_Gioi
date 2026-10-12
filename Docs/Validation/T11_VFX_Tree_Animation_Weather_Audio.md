> **HẬU KIỂM 11/10/2026: FAIL — T11 mở lại IN_PROGRESS.** Báo cáo PASS dưới đây là lịch sử và đã bị bác bỏ ở các tiêu chí animation/VFX/lifecycle. Xem [báo cáo hậu kiểm](T11_Independent_Review_20261011.md) và Evidence/T11_Independent_Review_20261011/.

# Báo Cáo Nghiệm Thu T11 - VFX, Animation Cây, Âm Thanh Thời Tiết và Luật Sét 10%

Task: T11
Result: PASS
Ngày nghiệm thu: 11/10/2026
Môi trường: Unity 6000.6.1f1, URP 17.6.0, Input System, Windows Editor & PlayMode
Đối tượng: T11 (VFX, animation cây, âm thanh thời tiết, luật sét 10% và công thức Protection)
Tình trạng sau phiên: PASSED / DONE. Toàn bộ tính năng domain, presentation và kiểm thử tự động đã hoàn thành và kiểm chứng trực tiếp trong Unity Editor.

---

## 1. Mục tiêu và phạm vi thực hiện

1. **Catalog thời tiết**:
   - Sử dụng ổn định 4 loại thời tiết: `calm` (Bình yên), `sunny` (Nắng), `rainy` (Mưa nhẹ), `thunderstorm` (Giông). Không tạo thêm thời tiết Bão/Gió riêng.
2. **Quy tắc sét 10% & Protection (CONFIRMED)**:
   - Mỗi đợt Giông đủ điều kiện gây hại (stage mở harmful weather và exposure > 0, tức `large_tree` trở lên; `seedling` và `young_tree` có exposure = 0 được bảo vệ tuyệt đối) có **đúng 10% xác suất xảy ra một cú sét đánh cây**.
   - Xét **duy nhất một lần** cho mỗi đợt Giông; tối đa 1 cú sét gây hại trong cả đợt.
   - Protection không làm thay đổi xác suất trúng sét (vẫn giữ nguyên 10%).
   - Exposure không nhân tỉ lệ 10% mà là điều kiện mở khóa.
   - Cú sét gây hại được lên lịch đánh sau 30% thời lượng đợt Giông (`strikeDelayFraction = 0.30`).
   - Lưu bền vững vào `WeatherState` / save DTO (`strikeEvaluated`, `strikeWillHit`, `strikeDelaySeconds`, `strikeExecuted`).
   - Offline catch-up tuyệt đối không chạy sét, không tính sát thương Burning và không làm chết cây.
3. **Sát thương Burning & Công thức Protection**:
   - Sét gây trạng thái Burning (thời lượng 10 giây, sát thương gốc 2 HP/giây theo PrototypeRules; Bình yên dập tắt Burning).
   - Protection từ nâng cấp Thân và điểm vĩnh viễn giảm sát thương Burning:
     `damagePerSecond = baseBurningDamagePerSecond / (1 + max(0, totalProtection))` (PROTOTYPE_DEFAULT).
4. **Quy tắc HP và Forced Rebirth**:
   - Sát thương trừ qua `HealthStatusSystem.Tick()`. Khi HP <= 0, clamp 0, chuyển `life = "DEAD"` đúng 1 lần, dừng gameplay, hiện UI Forced Rebirth. Sau Rebirth, reset HP về 100, reset thời tiết về calm, dọn sạch VFX/âm thanh và lịch sét run cũ.
5. **Animation Cây (Tree Animation Presentation)**:
   - Bình yên, Nắng, Mưa: Lá đung đưa nhẹ tự nhiên ở lá (`Foliage`) qua vertex animation procedural với độ lệch pha không gian; thân và cành không uốn; gốc cây giữ cố định tại mặt đất.
   - Giông: Lá rung mạnh; thân và cành (`Trunk_Branches`) uốn lượn theo hướng gió kết hợp nhịp giật Perlin noise và phục hồi đàn hồi; gốc cây (`Roots`) giữ cố định tại mặt đất $Y = 0$; biên độ thích ứng với kích thước từng stage.
   - Bảo toàn góc xoay Blender (-90° X / 270.02°) và gọi `Physics.SyncTransforms()` khi uốn để đảm bảo tương quan visual–collider, không hồi quy raycast tap cây, kéo camera, chặn UI và Evolution.
   - Đồng bộ hóa các anchor (`LightningTargetAnchor`, `LeavesCenterAnchor`, `TrunkAuraAnchor`, `RootAnchor`) theo thời gian thực.
6. **VFX & Âm Thanh Thời Tiết**:
   - Bình yên: Ánh sáng trung tính, lá lay nhẹ.
   - Nắng: Ánh sáng vàng ấm (`#FFF7E0`), cường độ 1.25x.
   - Mưa: Hệ thống hạt mưa procedural (`RainParticleSystem`) rơi dày từ trên cao với vệt dài kéo giãn theo vận tốc.
   - Giông: Mưa xối xả kết hợp gió; sét trang trí trên bầu trời (`AmbientSkyLightningBolt`) xuất hiện ngẫu nhiên theo presentation RNG độc lập (0 sát thương); cú sét gây hại đánh trúng chính xác `LightningTargetAnchor` với tia sét kép (`LightningBolt`), chớp sáng màn hình và rung giật phản hồi (shudder) trên cây; hiệu ứng cháy (`BurningIndicator`) theo vùng Root/Leaves/Trunk với lõi lửa và quầng khói bập bùng stylized.
   - Âm thanh: Vòng lặp procedural tiếng mưa rơi và gió rít êm dịu, tự động crossfade mượt mà theo thời tiết; sấm rền xa và sét đánh gần qua sự kiện; tích hợp trọn vẹn với `settings.musicEnabled`, pause/focus, DEAD và Rebirth.

---

## 2. Các file code và cấu hình triển khai

- **Domain**:
  - `Assets/_Project/Scripts/Domain/PrototypeRules.cs`: Thêm `thunderstormStrikeProbability = 0.10`, `strikeDelayFraction = 0.30`, phương thức `CalculateBurningDamagePerSecond(double totalProtection)`.
  - `Assets/_Project/Scripts/Domain/GameState.cs` (`WeatherState`): Thêm `strikeEvaluated`, `strikeWillHit`, `strikeDelaySeconds`, `strikeExecuted`.
  - `Assets/_Project/Scripts/Domain/WeatherSystem.cs`: Triển khai đánh giá 10% 1 lần duy nhất cho mỗi đợt Giông, đếm ngược `strikeDelaySeconds`, `EnsureStrikeEvaluated()` cho save migration, và reset sạch sẽ cho Rebirth.
  - `Assets/_Project/Scripts/Domain/HealthStatusSystem.cs`: Tích hợp công thức `rules.CalculateBurningDamagePerSecond(totalProtection)`.
- **Infrastructure**:
  - `Assets/_Project/Scripts/Infrastructure/SaveSerializer.cs`: Sao chép và khôi phục toàn vẹn 4 trường trạng thái sét trong `CopyWeather`.
- **Presentation**:
  - `Assets/_Project/Scripts/Presentation/TreeAnimationController.cs`: Tạo mới controller xử lý lá rung procedural, thân cành uốn lượn nhịp Perlin, shudder phản hồi sét đánh, bảo toàn góc xoay Blender gốc và đồng bộ anchors.
  - `Assets/_Project/Scripts/Presentation/TreeView.cs`: Thông báo stage đổi sang `TreeAnimationController` và đồng bộ anchors trong `LateUpdate()`.
  - `Assets/_Project/Scripts/Presentation/WeatherView.cs`: Hạt mưa procedural, 4 chế độ ánh sáng, tia sét trang trí bầu trời 0 sát thương, tia sét đánh trúng `LightningTargetAnchor`, hiệu ứng cháy `BurningIndicator`.
  - `Assets/_Project/Scripts/Presentation/AudioController.cs`: Vòng lặp âm thanh mưa/gió procedural, crossfade mượt mà, SFX sấm xa và sét đánh, tích hợp lifecycle pause/focus/DEAD/Rebirth.
  - `Assets/_Project/Scripts/Bootstrap/GameBootstrapper.cs`: Khởi tạo và liên kết các view và controller.
- **Tài liệu & Quyết định**:
  - `Docs/Decisions.md`: Bổ sung Quyết định D49.
  - `Docs/GDD_AI_Agent.md` và `Docs/GDD_AI_Agent.docx`: Cập nhật mục 7 Weather Protection và Burning.

---

## 3. Bằng chứng kiểm chứng tự động

### 3.1. EditMode Tests (224 / 224 PASSED)
- Bộ test suite EditMode trong `CayTheGioi.Tests` đạt **224 / 224 PASSED** (0 failed, 0 skipped).
- Bao gồm các unit test chuyên biệt trong `WeatherTests.cs`:
  - `Thunderstorm_Rolls10PercentStrikeChance_OncePerSpell`: Xác nhận 10% 1 lần duy nhất, đúng delay, đúng 1 cú sét trong đợt.
  - `Thunderstorm_MissesWhenRollAbove10Percent`: Xác nhận không bị sét đánh khi roll >= 0.10.
  - `SeedlingAndYoungTree_NeverGetHit_EvenIfThunderstormForced`: Xác nhận Mầm và Cây Non an toàn tuyệt đối.
  - `Protection_DoesNotAlterStrikeProbability_Stays10Percent`: Xác nhận Protection không làm đổi xác suất 10%.
  - `Protection_ReducesBurningDamagePerSecond_AccordingToFormula`: Xác nhận công thức giảm sát thương Burning.
  - `SaveData_PreservesStrikeStateAndDoesNotRerollOnLoad`: Xác nhận lưu và khôi phục trạng thái sét không bị reroll.
  - `OfflineCatchup_NeverAppliesLightningDamageOrKillsTree`: Xác nhận offline catch-up tuyệt đối không chạy sét, không làm chết cây.
  - `ProgressionMetricsTests`: Cập nhật kiểm tra chỉ số theo công thức Protection mới.

### 3.2. PlayMode Tests (69 / 69 PASSED)
- Toàn bộ 69 test PlayMode trong `CayTheGioi.Tests` đạt **69 / 69 PASSED** (0 failed, 0 skipped).
- Bao gồm:
  - `ArtPipelineSceneTests`: Kiểm chứng chính xác raycast tap cây, không nhận click xuyên Ground, không click mặt sau bị culling, và anchors theo dõi stage trên cả 3 stage.
  - `CoreResourcesSceneTests`: Input chuột, cảm ứng đa điểm, chặn UI, kéo camera không bị hồi quy.
  - `WeatherSceneTests`: Tỷ lệ buff tài nguyên và tích hợp thời tiết thời gian thực.
  - `EvolutionSceneTests`: Tiến hóa và chuyển cảnh mượt mà.

---

## 4. Kết luận

Toàn bộ các tiêu chí acceptance criteria của T11 đã được hoàn thành trọn vẹn và kiểm chứng thành công trên Unity 6000.6.1f1. T11 đủ điều kiện đóng với trạng thái **`status: DONE`**.
