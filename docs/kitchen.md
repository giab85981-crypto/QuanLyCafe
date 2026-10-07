# Bếp / Bar

Màn hình /kitchen dành cho Admin và Kitchen; Thu ngân không truy cập được. Có bố cục riêng không dùng thanh quản lý.

- Ba cột Chờ làm, Đang làm, Hoàn thành. Số phiếu và số phần tổng quan; hiển thị phiếu chờ lâu từ 15 phút.
- Tìm bàn, mã hóa đơn, phiếu, món và tùy chọn; lọc Tại quán/Mang về.
- Theo phiếu: tên bàn/nhãn mang về, giờ báo bếp, số lượng, size/topping và ghi chú đơn.
- Theo món: tổng số phần theo món/tùy chọn; Xem phiếu để thao tác. Bắt đầu/Hoàn thành áp dụng toàn phiếu, không áp dụng riêng một món.
- Tự cập nhật 15 giây; có Làm mới, trạng thái kết nối và thông báo lưu. Phiếu mới trước hoặc sau thanh toán đều xử lý được.
- Cột Hoàn thành hiển thị các phiếu báo trong hôm nay đã làm xong, loại trừ đơn hủy/hoàn tiền. Đây là lịch sử làm xong, chưa có bước xác nhận giao món cho khách.
- Thao tác chế biến không xuất kho lần nữa; nguyên liệu đã xuất khi báo bếp.

## Kiểm tra

1. Khởi động lại backend và frontend. Thu ngân tạo đơn, thêm món, Báo bếp hoặc thanh toán trực tiếp.
2. Đăng nhập staff.kitchen / DemoCafe123: phiếu xuất hiện ở Chờ làm.
3. Bắt đầu làm rồi Hoàn thành: phiếu chuyển cột đúng. Kiểm tra tổng hợp Theo món và tìm kiếm.
4. Admin cũng mở được Bếp/Bar từ menu cạnh Thu ngân. Kitchen chỉ dùng Bếp/Bar và Đăng xuất.

Backend/frontend build đạt; 43 kiểm tra đơn hàng đạt, gồm danh sách hoàn thành hôm nay và loại trừ phiếu cũ. Trình duyệt đã kiểm tra tài khoản Kitchen, tìm kiếm, tổng hợp món, bắt đầu và hoàn thành bằng phiếu demo có nhãn riêng; các phiếu demo được đóng sau kiểm tra.
