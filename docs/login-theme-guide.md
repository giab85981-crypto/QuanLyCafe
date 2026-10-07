# Đăng nhập và chế độ sáng/tối

## Sử dụng

- Trang đăng nhập có nút Quản lý và Bán hàng. Enter đăng nhập theo lựa chọn Quản lý; nếu không có quyền mở Tổng quan, hệ thống chuyển đến màn hình được cấp quyền.
- Nút mắt hiện/ẩn mật khẩu. Nhập thiếu hoặc sai thông tin có thông báo rõ ràng; không gửi nhiều lần khi đang đăng nhập.
- Bỏ liên kết Quên mật khẩu chưa hoạt động và checkbox Duy trì đăng nhập chưa có tác dụng. Thay bằng hướng dẫn liên hệ quản trị viên. Quản trị viên đặt lại mật khẩu tại Nhân viên → tài khoản đăng nhập → mật khẩu mới.
- Nút mặt trăng/mặt trời ở góc dưới bên trái có trên mọi màn hình. Bấm để chuyển sáng/tối, không cần tải lại.
- Chế độ được lưu trên trình duyệt, giữ khi đổi trang hoặc tải lại và đồng bộ giữa các tab cùng địa chỉ. Lần đầu lấy theo chế độ của thiết bị.
- Áp dụng cho đăng nhập, các trang quản lý, Thu ngân, Bếp/Bar và thực đơn QR. QR và hóa đơn xuất/in giữ màu sáng để dễ đọc và quét.

## Đã kiểm tra

- Build frontend thành công; lint không có lỗi (còn cảnh báo React của các phần đã có).
- Trình duyệt: nhập thiếu, nhập sai, hiện/ẩn mật khẩu, đăng nhập bằng Enter với tài khoản quản trị và chuyển trang theo quyền với tài khoản thu ngân.
- Chuyển sáng/tối và tải lại vẫn giữ chế độ.
- Kiểm tra chế độ tối qua các trang quản lý, Thu ngân, Bếp/Bar, QR; xem form thêm món và bảng thực đơn.
- Trang đăng nhập ở viewport 390 × 844: không tràn ngang.
- Kiểm tra bằng database demo riêng, đã dọn sau khi test.

## Test lại trên máy của bạn

1. Mở frontend bằng npm run dev; mở /login (đăng xuất trước nếu cần).
2. Thử bỏ trống, sai mật khẩu, rồi đăng nhập đúng bằng Enter hoặc nút Bán hàng.
3. Bấm nút sáng/tối ở góc dưới bên trái; chuyển trang và F5 để kiểm tra lưu lựa chọn.
4. Xem Thực đơn, mở form thêm món; kiểm tra Thu ngân, Bếp/Bar và QR.

Ảnh đối chiếu: login-light.png và login-dark.png trong thư mục docs.
