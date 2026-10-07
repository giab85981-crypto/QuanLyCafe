# Thu ngân / POS

- Thu ngân chỉ vào POS; Bếp / Bar dùng tài khoản riêng. Admin vẫn có đủ quyền.
- Hai tab Phòng / Bàn và Thực đơn; lọc khu vực, trạng thái và tìm bàn.
- Chọn món, size, topping, khách hàng; tạo đơn mang về; chuyển/gộp/tách bàn.
- F3 tìm món, F10 báo bếp, F9 mở thanh toán.
- Nguyên liệu được giữ ngay khi thêm món theo công thức; hủy món chưa báo trả lại phần giữ. Thực đơn hiển thị số phần còn nhận được.
- Thanh toán tự báo món chưa gửi, có bước xác nhận riêng: tiền mặt/chuyển khoản, số khách, giảm giá, đổi điểm.
- Thu ngân không xem/cập nhật hàng đợi bếp, quản lý, lịch sử đơn hàng hoặc Sổ quỹ. API trả 403 khi truy cập trái quyền.

## Kiểm tra

1. Khởi động backend bằng F5 và frontend bằng npm run dev, đăng xuất rồi đăng nhập lại.
2. staff.cashier / DemoCafe123: vào POS, menu chỉ có Đăng xuất. Thử /kitchen hoặc /dashboard sẽ quay về POS.
3. Chọn bàn, thêm món, báo bếp. staff.kitchen / DemoCafe123 nhận phiếu tại Bếp / Bar.
4. Trở lại Thu ngân, mở Thanh toán; kiểm tra tổng, giảm giá và xác nhận thu. Kiểm tra bàn trống sau khi thanh toán.
5. Admin vẫn dùng được các trang quản lý.

Đã kiểm tra: build frontend/backend; 34 kiểm tra API phân quyền; trình duyệt thực tế cho đăng nhập, menu Thu ngân, chọn bàn, hiển thị đơn, bước xác nhận thanh toán và chặn /kitchen.

Kiểm tra hồi quy: 41 kiểm tra nghiệp vụ đơn hàng đạt trên database thử nghiệm riêng (báo bếp, trừ kho, thanh toán, chống lặp giao dịch, hoàn tiền và lịch sử).
