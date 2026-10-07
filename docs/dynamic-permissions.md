# Phân quyền động

## Chạy và kiểm tra

Khởi động lại backend bằng F5 (dừng phiên cũ trước). Backend tự chạy migration `DynamicPermissions` và cấp mặc định lần đầu cho các nhóm cũ. Frontend vẫn chạy `npm run dev`, tải lại trang sau khi cập nhật source.

Đăng nhập Admin, vào **Phân quyền** trên menu chính.

1. **Nhóm quyền**: thêm nhóm mới hoặc sao chép nhóm hiện có; tích quyền cần thiết rồi lưu. Có tìm kiếm và chọn/bỏ từng nhóm chức năng.
2. **Nhân viên**: chọn tài khoản, chọn nhóm; mỗi quyền có **Theo nhóm / Cho phép riêng / Không cho phép**. Quyền riêng ưu tiên hơn nhóm. Nút xóa quyền riêng đưa nhân viên về kế thừa nhóm. Đổi nhóm tại trang này cũng xóa các quyền riêng cũ trên bản nháp, lưu mới áp dụng.
3. Admin luôn toàn bộ 44 quyền; không sửa nhóm Admin, không bỏ quyền riêng của Admin, không tự hạ quyền hay loại quản trị viên hoạt động cuối cùng.
4. Danh mục quyền là những thao tác ứng dụng hỗ trợ. Có thể tạo/sửa nhóm và quyền từng tài khoản mà không sửa source; bổ sung một nghiệp vụ hoàn toàn mới vẫn cần lập trình endpoint và thêm mã quyền.
5. API đọc quyền mới từ database trên **mỗi request**, thay thế claim quyền/nhóm cũ trong JWT. Màn hình đang mở đồng bộ mỗi 3 giây khi hiển thị và khi trở lại cửa sổ. Không phải đăng nhập lại. Mất quyền trang đang mở sẽ chuyển đến trang còn được phép. Không có quyền nào thì hiển thị màn hình chờ cấp quyền.
6. Quyền thao tác cần quyền nền tương ứng. Chọn thao tác trong giao diện tự chọn quyền nền; nếu chặn quyền nền thì quyền phụ thuộc không còn hiệu lực. Ví dụ bỏ thanh toán cũng bỏ hiệu lực giảm giá.
7. Xem có thể xuất dữ liệu đang được phép xem (không có quyền xuất riêng ở phiên bản này).
8. Báo cáo có trang riêng dùng API báo cáo hiện có, tách khỏi quyền xem tổng quan.

## Kịch bản demo đồ án

Mở Admin ở cửa sổ bình thường và nhân viên ở cửa sổ ẩn danh để tách phiên.

- Tạo nhóm `Phục vụ`, chọn **Mở bán hàng**, **Thêm món**, **Báo bếp**; không chọn **Thanh toán**. Gán nhân viên vào nhóm. Người này nhận món và báo bếp, không có nút Thanh toán.
- Cấp thêm **Xem thực đơn**, giữ không cho thêm/sửa/xóa: nhân viên xem được danh sách và thông tin món, không lưu sửa được. Gọi thẳng API tạo món cũng bị 403.
- Nhóm Thu ngân có thanh toán nhưng một nhân viên chọn **Không cho phép** riêng: chỉ người đó mất quyền. Chọn **Theo nhóm** để khôi phục.
- Cấp quyền xem Bếp, không cấp cập nhật: vào được màn hình Bếp nhưng không có nút bắt đầu/hoàn thành.
- Thu hồi quyền khi nhân viên đang đăng nhập: API chặn ngay, giao diện tự thay đổi sau tối đa khoảng 3 giây khi hiển thị.
- Mở **Lịch sử** để xem người thay đổi, đối tượng và thời điểm.

## Thiết kế backend

`RolePermission`: quyền nhóm. `AccountPermission`: cho phép/chặn riêng. `AccessAudit`: trước/sau thay đổi. `Role.AccessConfigured`: đảm bảo seed mặc định chỉ một lần, khởi động lại không ghi đè cấu hình đã sửa.

Quyền hiệu lực = quyền nhóm, cộng các cho phép riêng, trừ các chặn riêng, sau đó loại quyền thiếu điều kiện nền. Admin được toàn bộ danh mục.

`DynamicAccess.Required` ánh xạ endpoint sang mã nghiệp vụ, không ánh xạ theo tên nhóm. Endpoint chưa được khai báo bị chặn mặc định với nhân viên. `StaffAccessFilter` kiểm tra quyền thật trên server. Các payload đặc biệt được kiểm tra thêm: giảm giá/đổi điểm, loại xuất/kiểm kê/hủy kho, xem hóa đơn lịch sử.

Chỉ Admin có API cấu hình quyền. Nhân viên được cấp quản lý tài khoản chỉ quản lý tài khoản/nhóm trong phạm vi quyền mình đang có, không được tạo/sửa Admin hoặc tự nâng quyền. Đổi mật khẩu/khóa vẫn thu hồi phiên bằng SecurityVersion; đổi nhóm không thu hồi phiên để áp dụng động.

Cập nhật quyền dùng transaction, khóa cấu hình và revision chống ghi đè bản đã thay đổi. Quyền không hợp lệ hoặc lặp bị từ chối. Có lịch sử trước/sau trong database.

## Kiểm tra tự động

`tests/AccessChecks` tạo database có tên GUID riêng và tự xóa sau khi chạy; kiểm tra qua HTTP/JWT thật: cấp/thu hồi cùng token, quyền riêng, phụ thuộc, nhóm mới, khởi động lại, revision, Admin, payload và toàn bộ endpoint. Các tham số preview chỉ chấp nhận database `CafeAccessPreview_` cộng 32 ký tự hex.

`tests/StaffChecks` kiểm tra quản trị viên cuối cùng, mật khẩu, khóa, hồ sơ và nâng cấp database. `tests/OrdersChecks` kiểm tra luồng đơn/stock/kitchen/checkout/refund vẫn đúng.
