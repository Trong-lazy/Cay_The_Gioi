# Công việc để sau

Các mục dưới đây chưa nằm trong task khung hiện tại. Không bắt đầu chỉ vì đã thấy trong backlog. Khi người dùng yêu cầu hoặc task khung hoàn tất, lập task với phụ thuộc và nghiệm thu trước khi thực hiện.

- Hoàn thiện art/integration cho ba stage prototype, pivot/scale/material thống nhất; giữ ID stage và view anchor. Theo D44, `Tree_Large` là nguồn Cây Lớn; Mầm/Non cũng đã có nguồn. Mốc Cây Trưởng Thành cần model riêng theo ảnh tham chiếu người dùng đã chọn. Xem `Docs/Art/Tree_Model_Registry.md`.
- Build và nghiệm thu Android/iOS trên máy thật (Landscape), tạm hoãn theo yêu cầu 04/10/2026 để ưu tiên Windows; chi tiết tại Docs/Deferred/Mobile_Validation.md. Không chặn nghiệm thu T09 Windows.
- Thay UI/icon/VFX/audio tạm bằng asset có nguồn và quyền sử dụng rõ.
- Mở rộng stage Trưởng Thành, Cổ Thụ, Cổ Đại, Cây Thế Giới khi được giao riêng. Theo D43, Cổ Đại và Cây Thế Giới dùng chung base model Cổ Thụ trong prototype, nhưng cần material/VFX riêng và Play Mode validation; thay mỗi mốc bằng model riêng về sau là một backlog độc lập. Còn thiếu model Cây Trưởng Thành; không tự dựng/tải. Có nguồn art không đồng nghĩa đã thêm stage/config/save vào gameplay.
- Thêm Weather/status và cosmic/cosmetic sau khi balance loop ổn.
- Zoom thủ công, setting audio chi tiết và hiệu ứng cây nâng cao.
- Tuning nhóm tap và thời gian thưởng Sức sống sau playtest; giữ cap một giờ và buff 5% nếu chưa có yêu cầu thay đổi.

Backend, ads, IAP, multiplayer và nhiều map nằm ngoài scope hiện tại, không tự đưa vào roadmap.
