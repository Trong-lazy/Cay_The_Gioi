# UI tham chiếu dùng trong game

Nguồn: chat “Tách UI assets từ ảnh tham chiếu”, bộ outputs/ui_assets ngày 07/10/2026. Chọn 10 PNG có status production: 2 nền nút, 2 nền card và 6 icon. Provenance, source rect, reconstruction limitations và SHA-256 nằm trong UI_ReferencePack.json. Copy nguyên byte, Unity sinh GUID/meta và import Sprite Single, alpha transparency, bilinear, không mipmap/nén, PPU100.

Các nền dùng border từ manifest nine_slice_ltrb; chuyển sang Unity Vector4(left, bottom, right, top). Không stretch frame dây leo cố định, không dùng needs_review/reference_only. Fine texture nằm vùng bị che đã được bộ nguồn tái tạo; không tuyên bố khôi phục chính xác layer gốc. Icon category_vfx là placeholder trình bày Linh khí, không thay nghĩa gameplay.

GameUiTheme tải sprite từ Resources/UI/ReferencePack và thống nhất giấy/nút xanh/chữ xanh đậm. Các view chỉ đọc state hoặc gọi API domain. HUD bốn card, HP/Sức sống/thời tiết gọn, top menu và bottom action, panel upgrade 620 units Landscape để chừa cây. Card, panel và button chặn input theo adapter chung; icon/text không tạo thêm hit region.
