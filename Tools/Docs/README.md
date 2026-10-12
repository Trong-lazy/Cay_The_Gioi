# Công cụ bảo trì tài liệu

Chạy từ project root. Python 3.10 trở lên cho updater; exporter cần python-docx. Codex desktop dùng runtime trả về từ workspace dependencies, không cài package vào Unity project.

```powershell
& 'C:\Users\Asus\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' Tools/Docs/update_progress.py
& 'C:\Users\Asus\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' Tools/Docs/export_gdd_docx.py
```

Agent khác có thể dùng runtime tương đương của nó. Không bắt gameplay hoặc build phụ thuộc Python.

Updater đọc task frontmatter, kiểm tra trạng thái/phụ thuộc, chỉ có một task hoạt động, DONE không còn checkbox trống và có Validation PASS tương ứng. Nó tạo lại Progress/CURRENT_TASK, không sửa nguồn task. Ghi tạm rồi replace từng file; nếu phiên bị ngắt giữa hai file tổng hợp, chạy lại để đồng bộ.

Exporter tạo Word từ GDD Markdown. Kiểm tra bố cục sau khi xuất lại; không dùng Word làm nguồn chỉnh riêng. Các tài liệu Technical/Tasks vẫn là file riêng và được GDD dẫn tới.
