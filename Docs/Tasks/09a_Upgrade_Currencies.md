---
id: T09A
title: Sửa phân bổ tài nguyên cho nâng cấp
status: DONE
depends_on: ["T09"]
validation: "Docs/Validation/T09A_Upgrade_Currencies_20261009.md"
---

# T09A Sửa phân bổ tài nguyên cho nâng cấp

## Mục tiêu

Theo yêu cầu trực tiếp ngày 09/10/2026, ưu tiên sửa vòng kinh tế trước khi chỉnh giao diện. Rễ dùng Water, Lá dùng Quang hợp (`photosynthesis`), Thân dùng Growth. Evolution cần cả ba; Cây Trưởng Thành mở khả năng mua nhánh hấp thụ Linh khí bằng lượng lớn cả ba tài nguyên. Game mới được cấp vốn cho L0→L1 của các nhánh mặc định. Coin tiếp tục mua cosmetic.

## Thực hiện

- [x] Loại tài nguyên chi trả theo từng nhánh/cấp nằm trong config với ID ổn định.
- [x] Quote, kiểm tra số dư, giao dịch và UI dùng cùng loại tài nguyên.
- [x] Quang hợp/Growth/Linh khí có state, sản xuất, HUD, save và offline nhất quán; không lẫn Quang hợp với Growth.
- [x] Game mới cấp đủ vốn L0→L1 cho nhánh mặc định; nguồn sản xuất không tạo vòng khóa.
- [x] Evolution giao dịch cả Water/Quang hợp/Growth nguyên tử.
- [x] Mốc `mature_tree` và giao dịch mở hấp thụ Linh khí có điều kiện, chi phí config, lưu và reset đúng.
- [x] Rebirth/DEAD và nguồn sản xuất được kiểm tra lại theo luật mới.
- [x] Save cũ giữ level và dữ liệu vĩnh viễn; migration một lần bổ sung Quang hợp và đủ Growth cho Thân L0, không cấp lại khi load config 6.

## Acceptance

- [x] Rễ mọi cấp dùng Water; Lá mọi cấp dùng Quang hợp; Thân dùng Growth.
- [x] Thiếu đúng tài nguyên thì không nâng, không trừ nhầm tài nguyên khác, không phát event thành công.
- [x] Số dư và level commit trước event; không âm/double-spend/cấp level miễn phí khi mất độ chính xác.
- [x] UI ghi đúng chi phí, loại tài nguyên thiếu và điều kiện mở khóa; mouse/touch không tap xuyên UI.
- [x] Compile, EditMode, PlayMode, thử trực tiếp MainScene và hồi quy save/evolution/rebirth/shop đạt; bằng chứng thật được lưu.

## Bàn giao

10/10/2026: hoàn tất config prototype-6 và luật D47. Rễ/Water, Lá/Quang hợp, Thân/Growth; chuỗi Rễ → Lá → Thân sản xuất đủ ba loại. Evolution tiêu cả ba nguyên tử. Mốc mature_tree mở mua hấp thụ Linh khí bằng 1000 mỗi tài nguyên, cấp sau dùng Linh khí. Giá, nguồn sản xuất, caps, hiệu ứng point và vốn sau Rebirth là PROTOTYPE_DEFAULT trong config; chưa phải số cân bằng người dùng chốt.

Các nhóm file sửa: Domain (PrototypeRules, GameState, Production/Resource/Upgrade/Evolution/Rebirth, Offline, Save, Tutorial, PermanentEffects), Presentation (HUD/Upgrade/Evolution/Rebirth), QA/Editor setup, Tests, config asset và tài liệu kỹ thuật/GDD. Bản Word GDD đã xuất lại và kiểm tra đủ 6 trang. Validation ghi chi tiết cấu hình và bằng chứng.

Unity 6000.6.1f1, MainScene, config prototype-6: 220/220 EditMode và 65/65 PlayMode PASS; thử trực tiếp đủ ba Evolution, mở Linh khí, nâng bằng Linh khí, lưu và Rebirth. Console lần cuối không lỗi/cảnh báo. MainScene và save thật/backup giữ nguyên SHA-256; toàn bộ thử trực tiếp dùng thư mục save cô lập, đã phục hồi override và thoát Play. Các lần chạy tạm/fail trước đó được giữ làm lịch sử, không tính nghiệm thu.

Giới hạn: Cây Trưởng Thành dùng model Cây Lớn placeholder; chưa build Windows lại với config mới. Hai lỗi raycast T10 giữ nguyên phạm vi, còn chờ xử lý. Mobile thiết bị thật tiếp tục tạm hoãn.

## Tiếp theo

T09B: bố cục và giao diện chức năng từ asset production đã tách. Sau đó tiếp tục lỗi raycast T10 và build Windows cuối.
