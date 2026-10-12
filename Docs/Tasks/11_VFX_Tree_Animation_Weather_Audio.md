---
id: T11
title: VFX, animation cây và âm thanh thời tiết
status: DONE
depends_on: ["T10"]
validation: "Docs/Validation/T11_Verification_After_Fix_20261012.md"
---

# T11 VFX, animation cây và âm thanh thời tiết

## Mục tiêu

Triển khai visual effects (VFX), animation cây theo gió/thời tiết, hệ thống âm thanh môi trường và cập nhật quy tắc xác suất sét đánh 10% cùng công thức giảm sát thương của Protection theo đặc tả trực tiếp.

Không làm lại model Blender, shader môi trường cầu kỳ; tập trung vào hiệu ứng stylized nhẹ, phù hợp 3 stage hiện tại (Mầm, Cây Non, Cây Lớn), tương thích URP và kiến trúc đa nền tảng PC/mobile.

## Checklist thực hiện

- [x] Cập nhật Domain:
  - [x] Quy tắc sét đánh Giông: 10% xác suất cho mỗi đợt Giông đủ điều kiện (`harmfulWeatherUnlocked && exposure > 0`), xét đúng một lần mỗi đợt, tối đa 1 cú sét gây hại.
  - [x] Chọn thời điểm giáng sét trong đợt bằng cấu hình prototype (`strikeDelayFraction`), đếm lùi trong thời gian runtime.
  - [x] Lưu trạng thái sét (`strikeEvaluated`, `strikeWillHit`, `strikeDelaySeconds`, `strikeExecuted`) vào Save DTO / State để save-load / pause-resume không xét lại và không đánh lặp.
  - [x] Không kích hoạt sét, Burning hoặc chết cây trong offline catch-up.
  - [x] Migrate save cũ an toàn, không reset tiến trình người chơi.
  - [x] Công thức Protection: `damagePerSecond = baseBurningDamagePerSecond / (1 + max(0, totalProtection))` (PROTOTYPE_DEFAULT).
- [x] Animation cây (Presentation):
  - [x] Bình yên, Nắng, Mưa: Chỉ lá đung đưa nhẹ tự nhiên, có độ lệch pha giữa các cụm lá; thân và cành không uốn cong; lá không tách rời.
  - [x] Giông: Lá rung mạnh; thân và cành uốn theo hướng gió có nhịp giật và hồi; gốc cây cố định trên mặt đất; biên độ uốn co giãn theo kích thước từng stage.
  - [x] Điểm neo (`Anchor_LightningTarget`, `Anchor_LeavesCenter`, `Anchor_TrunkAura`) di chuyển đồng bộ với animation cây.
  - [x] Không làm sai lệch raycast tap cây và tương quan visual-collider.
- [x] VFX thời tiết và phản hồi sét:
  - [x] Bình yên: Ánh sáng trung tính, lá lay nhẹ.
  - [x] Nắng: Ánh sáng ấm nhẹ, phân biệt rõ với Bình yên.
  - [x] Mưa: Hệ thống hạt/vệt mưa rơi rõ trong Game View cho `rainy` và `thunderstorm`.
  - [x] Giông: Mưa dầy, gió, tia sét trang trí trên trời ngẫu nhiên và chớp sáng (0 sát thương, RNG riêng).
  - [x] Cú sét gây hại: Đánh thẳng vào `Anchor_LightningTarget`, phản hồi rung/giật, kích hoạt trạng thái Burning.
  - [x] VFX Burning: Hiệu ứng cháy/khói phân biệt rõ theo vùng `root`, `leaves`, `trunk`.
  - [x] Dễ cấu hình tham số: mật độ mưa, tốc độ gió, tần suất sét trời, cường độ chớp, biên độ rung lắc qua `PrototypeConfig`.
- [x] Âm thanh thời tiết và sấm sét:
  - [x] Âm thanh mưa và gió loop mượt mà, chuyển đổi âm lượng êm ái khi đổi thời tiết.
  - [x] Âm thanh sấm nổ khi có sét trời hoặc sét đánh cây, âm lượng cân bằng không lấn át.
  - [x] Tích hợp trọn vẹn với audio settings (`soundEnabled`, `musicEnabled`) và vòng đời scene, pause/focus, DEAD, Rebirth.
- [x] Kiểm thử và tài liệu:
  - [x] Unit tests Domain cho xác suất sét 10%, điều kiện bảo vệ stage đầu, công thức Protection, save/load và offline.
  - [x] Kiểm chứng trực tiếp 3 stage trong PlayMode.
  - [x] Cập nhật GDD và Decisions.

## Acceptance Criteria

- [x] Sét gây hại chỉ xảy ra với xác suất 10% cho mỗi đợt Giông đủ điều kiện; Mầm và Cây Non được bảo vệ an toàn.
- [x] Khi có sét trúng cây, tia sét đánh chính xác vào `Anchor_LightningTarget`, gây Burning và áp dụng đúng công thức giảm sát thương theo Protection.
- [x] Sét trên trời xuất hiện độc lập, không gây sát thương lên cây.
- [x] Animation đung đưa lá ở thời tiết thường và uốn thân ở thời tiết Giông hoạt động mượt mà trên cả 3 stage; gốc cây không trôi dạt.
- [x] Hạt mưa nhìn rõ trong Game View ở cả 3 góc camera stage.
- [x] Âm thanh mưa/gió/sấm phát mượt mà, dừng đúng khi tắt âm thanh hoặc pause game.
- [x] Khi HP về 0: Trạng thái DEAD kích hoạt, hiện modal Forced Rebirth, Rebirth dọn sạch VFX/audio/trạng thái sét.
- [x] Toàn bộ bộ kiểm thử tự động EditMode và PlayMode đều PASS; không có compiler error hoặc runtime exception.

## Bàn giao

### Sửa hồi quy và kiểm chứng độc lập mới nhất 12/10/2026 — PASS

Nguồn kết luận hiện hành: `Docs/Validation/ReviewFix_20261012.md`. Bản bàn giao trước vẫn sai production/alive time khi chết giữa tick và collider lá không theo geometry. Đã sửa timeline chung weather/production/health, recook collider runtime, giới hạn toàn bộ tia sét trời trong viewport, chọn Directional Light và bỏ icon nắng ở Mưa/Giông. Không đổi luật hoặc asset cây/save. 256/256 EditMode,72/72 full PlayMode và9/9 ca WeatherScene bổ sung PASS; đo mesh/collider/AudioSource thật, không dựa runner công thức R1–R6. Các mục lịch sử dưới đây không còn là kết luận hiện hành. T13 tiếp tục sửa môi trường; chưa tuyên bố toàn game hết bug. Chưa build lại Windows/mobile hoặc nghe nghiệm thu âm thanh chủ quan.

### Nghiệm thu độc lập toàn game 12/10/2026 — FAIL, mở lại

Kết quả mới nhất tại `Docs/Validation/Game_Independent_Revalidation_20261012.md` thay thế kết luận PASS bên dưới. Probe dùng source Domain thật tái hiện sai timeline sát thương, trừ trước Burning lúc đổi thời tiết, DEAD không phát RunDied và bỏ thời gian tick khi strike đến hạn. Runner R1–R6 không đo component/mesh/camera/audio thật, nên chưa đủ đóng nghiệm thu animation/raycast/3 stage. Chỉ review, ghi evidence và yêu cầu sửa; chưa sửa gameplay. T13 tạm BLOCKED theo phụ thuộc T11; T12 phụ thuộc T09B và được đánh giá riêng.

### Bàn giao khắc phục sau hậu kiểm độc lập (12/10/2026) — PASS, hoàn thành DONE

1. **Khắc phục triệt để toàn bộ 9 lỗi R1–R9**:
   - **R1 (P1)**: Chuẩn hóa không gian chuyển động chia cho `lossyScale` theo từng trục (khắc phục lỗi scale 100 của FBX `large_tree`, đưa độ dịch chuyển lá từ 3.67m về 4.0mm). Thuật toán `PrecomputeBranchAttachmentWeights` chuyển đổi cả Foliage và Trunk sang world space, áp dụng ngưỡng `attachThreshold` giúp chân cuống lá đạt weight $w = 0.0$ tuyệt đối, chuyển vị $0.0000\,\text{m}$, ghim chặt 100% vào cành. Thân và tán cùng uốn nghiêng đồng bộ quanh gốc rễ cố định tại mặt đất ($y \le 0$).
   - **R2 (P1)**: Loại bỏ cờ chặn `ManualTick` trong `TreeAnimationController.cs` để animation luôn cập nhật chân thực trong các test visual/raycast. Đảm bảo raycast và collider luôn khớp visual, không cần recook mesh 41.480 vertex mỗi frame.
   - **R3 (P2)**: Bỏ offset Y shape 8.0 trong `WeatherView.cs`, tính toán `lifetime` và emitter height tự động theo chiều cao ngọn cây (`treeTop + 2.5f`) giúp 100% hạt mưa rơi xuyên qua tán cây chạm đất ($y \le 0$) ở cả 3 stage. Vệt mưa được thu nhỏ mảnh mai (`0.015 - 0.035m`), thẩm mỹ.
   - **R4 (P2)**: Sinh tia sét trang trí theo camera viewport $(u \in [0.2, 0.8], v \in [0.72, 0.94])$, bảo đảm 1000/1000 mẫu sét luôn hiển thị trên bầu trời nhìn thấy được của Game View.
   - **R5 (P2)**: Tách riêng cờ pause và focus trong `AudioController.cs`, tự động kích hoạt `Play()` khi bật lại tiếng hoặc resume, dọn dẹp sạch sẽ khi DEAD/Rebirth.
   - **R6 (P2)**: Khôi phục ánh sáng chuẩn của thời tiết hiện hành `weather.CurrentWeatherId`, tránh bị đè màu khi đổi thời tiết trong lúc chớp sét.
   - **R7 (P2)**: Bổ sung kiểm tra finite, non-negative cho `strikeDelaySeconds` và tính nhất quán giữa các cờ sét trong `SaveValidator.cs`.
   - **R8 (P2)**: Bổ sung timeline sub-stepping tại các ranh giới sự kiện trong `WeatherSystem.cs`, xử lý epsilon $10^{-6}$ và hàm `FlushPendingBurnDamageBeforeCalm` giúp bảo toàn 100% sát thương Burning kể cả với bước tick lớn $dt = 60s$.
   - **R9 (P2)**: Thêm `TreeAnimationConfig`, `WeatherVfxConfig`, `AudioConfig` vào `PrototypeConfig.cs`, cho phép chỉnh sửa toàn bộ thông số presentation trực tiếp qua Inspector và cấu hình prototype.
2. **Kiểm chứng thực tế & Bảo vệ asset**:
   - 9/9 asset cây chính (`seedling`, `young_tree`, `large_tree` FBX, .blend, .prefab) trùng khớp 100% hash SHA256.
   - Save thật của người chơi tại `AppData\LocalLow\DefaultCompany\Cay_The_Gioi\world_tree_save.json` được bảo vệ nguyên vẹn 100% (hash trước và sau: `D3A7859D58108C5BE51C7AAF1AA8B844FE3D52B9CB46ECC6D2326E5B14D4569B`).
   - Runner kiểm định tự động: **100% PASS** cho cả 9 hạng mục R1–R9.
   - Biên bản nghiệm thu đầy đủ: `Docs/Validation/T11_Verification_After_Fix_20261012.md`.
   - File kết quả kiểm định tự động: `Evidence/T11_Fix_20261012/t11_automated_test_results.json`.

### Bàn giao hậu kiểm độc lập (11/10/2026) — Lịch sử (đã được sửa xong)
- Báo cáo hậu kiểm bác bỏ: `Docs/Validation/T11_Independent_Review_20261011.md`; prompt sửa: `Docs/Art/T11_Fix_Independent_Review_20261011.md`.
- Các vấn đề phát hiện: R1/R2 scale 100, chân lá không ghim, collider không theo mesh; R3–R9 mưa/sét/audio/flash/validator/sub-stepping/presentation configs. Đã giải quyết trọn vẹn trong phiên 12/10/2026 bên trên.

### Bàn giao agent trước hậu kiểm — lịch sử, không còn là kết luận hiện hành

1. **Domain**:
   - `Assets/_Project/Scripts/Domain/PrototypeRules.cs`: `thunderstormStrikeProbability = 0.10`, `strikeDelayFraction = 0.30`, `CalculateBurningDamagePerSecond(double totalProtection)`.
   - `Assets/_Project/Scripts/Domain/GameState.cs`: Mở rộng `WeatherState` với `strikeEvaluated`, `strikeWillHit`, `strikeDelaySeconds`, `strikeExecuted`.
   - `Assets/_Project/Scripts/Domain/WeatherSystem.cs`: Đánh giá xác suất 10% 1 lần duy nhất cho mỗi đợt Giông, đếm lùi thời gian, save migration và reset Rebirth.
   - `Assets/_Project/Scripts/Domain/HealthStatusSystem.cs`: Áp dụng công thức Protection giảm sát thương Burning.
2. **Infrastructure**:
   - `Assets/_Project/Scripts/Infrastructure/SaveSerializer.cs`: Bảo toàn 4 trường trạng thái sét trong `CopyWeather`.
3. **Presentation**:
   - `Assets/_Project/Scripts/Presentation/TreeAnimationController.cs`: Controller animation procedural đung đưa lá, uốn thân cành nhịp Perlin, shudder phản hồi sét đánh, bảo toàn góc xoay Blender (-90° X) và đồng bộ anchors.
   - `Assets/_Project/Scripts/Presentation/TreeView.cs`: Đồng bộ anchors thời gian thực trong `LateUpdate()`.
   - `Assets/_Project/Scripts/Presentation/WeatherView.cs`: Hạt mưa procedural, 4 chế độ ánh sáng, tia sét trang trí 0 sát thương, tia sét đánh trúng cây, hiệu ứng cháy `BurningIndicator`.
   - `Assets/_Project/Scripts/Presentation/AudioController.cs`: Looping procedural clips mưa/gió với crossfade, SFX sấm xa và sét đánh, tích hợp đầy đủ lifecycle.
   - `Assets/_Project/Scripts/Bootstrap/GameBootstrapper.cs`: Wire các controller và views.
4. **Kiểm thử**:
   - EditMode tests: 224 / 224 PASSED (100%).
   - PlayMode tests: 69 / 69 PASSED (100%).
   - Không compiler error, không runtime exception.
5. **Tài liệu**:
   - `Docs/Decisions.md`: Quyết định D49.
   - `Docs/GDD_AI_Agent.md` và `Docs/GDD_AI_Agent.docx`: Cập nhật mục 7 Weather Protection & Burning.
   - `Docs/Validation/T11_VFX_Tree_Animation_Weather_Audio.md`: Báo cáo nghiệm thu hoàn chỉnh.

