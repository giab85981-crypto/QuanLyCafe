# Bước 3 — giao diện và phản hồi thao tác

## Đã chỉnh

- Thống nhất thông báo mất kết nối, quá thời gian chờ, hết phiên, thiếu quyền và lỗi máy chủ cho các luồng quản lý, bán hàng, khách hàng và Bếp / Bar. Giữ nguyên thông báo nghiệp vụ như thiếu nguyên liệu hoặc hóa đơn vừa thay đổi; không hiển thị trang lỗi HTML / stack trace của máy chủ.
- Dùng trạng thái tải có biểu tượng và lời hướng dẫn; phân biệt tải thất bại với danh sách trống ở Thực đơn, Kho, Phòng / Bàn, Báo cáo, Sổ quỹ và thực đơn QR. Đơn hàng không hiển thị dòng cũ trong lúc tải danh sách mới.
- Sổ quỹ và Kho hiển thị dấu — thay số tiền / thống kê khi chưa tải xong hoặc tải thất bại. Khóa xuất dữ liệu khi chưa có số liệu hợp lệ tại các trang đã chỉnh.
- Sổ quỹ kiểm tra khoảng ngày, thông báo ghi phiếu thành công và giữ lỗi lưu trong hộp thoại để sửa rồi thử lại.
- Hộp thoại giữ Tab / Shift+Tab bên trong, Escape đóng hộp trên cùng, trả focus về nút mở và giữ khóa cuộn khi còn hộp con. Nút chân hộp tự xuống dòng trên màn hình nhỏ.
- Bổ sung nhãn cho trường đăng nhập, nút hiện / ẩn mật khẩu và khoảng ngày; thống nhất viền focus bàn phím và trạng thái nút bị khóa.

## Kiểm tra đã chạy

- `node --test tests/UiFeedbackChecks.mjs`: 6 nhóm kiểm tra đạt, gồm lỗi mạng, quyền, bảo vệ thông tin lỗi máy chủ, lỗi nghiệp vụ, validation và dữ liệu lỗi trống.
- `npm run build` tại frontend: thành công. Vẫn có cảnh báo kích thước bundle lớn từ thư viện hiện tại.
- `npm run lint` tại frontend: không có lỗi; còn các cảnh báo React ở những trang hiện tại. Lệnh lint đã giới hạn vào `src`, tránh quét thư viện phụ thuộc.
- Trình duyệt, harness dùng chính Modal / PageState hiện tại, React StrictMode: focus đầu vào, Tab quay vòng, Escape với hai hộp, trả focus và phục hồi cuộn đạt.
- Thực đơn QR thật khi API không kết nối: thông báo tiếng Việt, nút thử lại hoạt động; ảnh `ui-feedback-review.png`.

## Bạn test lại

1. Chạy frontend như thường lệ và tải lại trang. Không cần cập nhật database cho bước này.
2. Mở Thực đơn / Kho / Phòng bàn / Báo cáo / Sổ quỹ, tìm từ khóa không tồn tại rồi đổi bộ lọc lại.
3. Mở hộp thêm món hoặc phiếu thu; nhấn Tab, Shift+Tab, Escape. Kiểm tra ô nhập giữ focus và hộp đóng về đúng nút mở.
4. Trong Sổ quỹ, chọn từ ngày sau đến ngày; phải báo khoảng ngày chưa hợp lệ và khóa xuất Excel. Chọn lại khoảng ngày hợp lệ để tải tiếp.
5. Thử chế độ màn hình hẹp: nút trong chân hộp xuống dòng, nội dung hộp có thể cuộn.

Thay đổi bước này nằm ở frontend, không bổ sung migration hoặc sửa dữ liệu quán. Các sửa nghiệp vụ và kiểm tra của bước 1–2 vẫn được giữ trong source.
