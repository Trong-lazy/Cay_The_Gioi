---
id: T12
title: Hiển thị chi phí trên nút nâng cấp
status: DONE
depends_on: ["T09B"]
validation: "Docs/Validation/T12_Upgrade_Button_Cost_UI_20261011.md"
---

# T12 Hiển thị chi phí trên nút nâng cấp

## Mục tiêu

Yêu cầu trực tiếp 11/10/2026: chỉ thay nhãn nút nâng cấp +1 bằng số lượng và tên nguyên liệu cần trả, rồi kiểm thử. Không sửa VFX, model, economy, nút Mở khóa/Tiến hóa hoặc bố cục khác.

## Checklist và Acceptance

- [x] Nút nâng cấp hiển thị chi phí từ quote hiện hành và đúng loại tài nguyên; cập nhật sau nâng cấp.
- [x] Không đổi logic giao dịch, trạng thái thiếu tài nguyên/đạt cap; giữ Mở khóa.
- [x] Unity compile không lỗi liên quan, Console0 error; kiểm thử hồi quy nâng cấp7/7 PASS. Cảnh báo API cũ QaBuildRunner được ghi trong Validation.
- [x] Play trực tiếp MainScene và kiểm tra nhãn không tràn ở 1920×1080 và 1366×768.
- [x] Validation, Evidence và bàn giao; save thật bảo toàn; cập nhật con trỏ.

## Bàn giao

12/10/2026: Review toàn game chạy lại Unity PlayMode suite69/69PASS, UpgradeSceneTests7/7PASS. Không phát hiện hồi quy chức năng nâng cấp trong các ca này; giữ DONE (phụ thuộc T09B, không phải T11). Không coi kết quả đó là kiểm tra mới mọi panel/2độ phân giải. XML: Evidence/Revalidation_20261012/PlayModeResults.xml; giới hạn: Docs/Validation/Game_Independent_Revalidation_20261012.md.

11/10/2026: DONE. Chỉ sửa UpgradePanelView.cs (nhãn giá hai dòng + best-fit) và ReferenceUiLayout.cs (không ghi đè nhãn), cùng tài liệu/Evidence. Không sửa T11/VFX/model/economy/config. Rễ/Lá/Thân/Linh khí đúng ví, đúng giá; cập nhật giá sau mua, thiếu tiền/cap vẫn khóa. UpgradeSceneTests7/7 PASS theo XML Unity mới; MCP có timeout/domain reload, giữ log và không tính lượt0 tests là PASS. Xem Validation hiện hành và Evidence/T12_Upgrade_Button_Cost_UI_20261011. Save thật giữ hash; MainScene sạch, ngoài Play, save/clock override null. Không build Windows/mobile cho thay đổi nhãn này.
