# Rà soát luồng bán hàng và phân quyền — 07/10/2026

## Những lỗi đã sửa

- Cập nhật số khách và gắn khách hàng dùng cùng khóa giao dịch với thêm món, báo bếp và thanh toán. Nhân viên không thể ghi đè thông tin hóa đơn sau khi người khác thanh toán.
- Quyền thanh toán không còn cho phép sửa số khách hoặc đổi/gỡ khách hàng qua dữ liệu thanh toán nếu tài khoản thiếu quyền sửa đơn `POS_ORDER`.
- Thu ngân tải lại đơn trước khi mở thanh toán. Frontend gửi số tiền dự kiến (`expectedTotal`); nếu số tiền backend tính khác do đơn vừa thay đổi, backend trả 409 và hoàn tác toàn bộ lần thanh toán. Giao diện tải lại đơn, yêu cầu người dùng xác nhận số mới, không tự thu tiếp.
- Thu hồi quyền giảm giá sẽ xóa mức giảm và số điểm đổi đang chọn, khóa ô nhập. Thu hồi quyền thêm khách đóng form thêm nhanh; mất quyền sửa đơn đóng cửa sổ chọn khách.
- Trang Đơn hàng ẩn nút tạo mang về và lối vào bán tại bàn theo quyền tương ứng. Thu hồi quyền xem Bếp/Bar chuyển nhân viên ra khỏi trang bằng cơ chế đồng bộ quyền hiện có.

Không có migration mới. Khởi động lại backend bằng F5 và tải lại frontend để dùng bản sửa.

## Luồng đã kiểm tra qua API thực tế

Kiểm tra chạy trên database GUID riêng và tự xóa sau khi hoàn tất, không dùng database quán.

1. Admin nhập 100ml nguyên liệu, đơn giá 100đ/ml, trả 4.000đ: tổng nhập 10.000đ, còn nợ nhà cung cấp 6.000đ. Gửi lại yêu cầu nhập không tăng tồn hoặc ghi chi lần hai.
2. Thu ngân A mở ca 200.000đ, nhận 7 phần dùng 10ml/phần. Thu ngân B không nhận được 4 phần, chỉ nhận được 3 phần còn lại. Hủy một phần chưa gửi ở B giải phóng phần giữ.
3. A báo bếp; báo lại không xuất thêm. Hủy một phần khi chờ làm trả 10ml về lô gốc. Bếp bắt đầu làm; hủy thêm một phần ghi hao hụt, không trả kho. Bếp hoàn thành 5 phần còn lại.
4. Thu hồi quyền sửa đơn của A ngay khi đang đăng nhập: API sửa số khách bị chặn. Gửi số khách/khách hàng khác qua thanh toán cũng bị chặn.
5. Hai yêu cầu thanh toán đồng thời chỉ tạo một phiếu thu 125.000đ, gắn đúng ca và tích đúng 12 điểm khách hàng. Người thiếu quyền thanh toán không thể trả đơn thực tế đang phục vụ.
6. B thanh toán hai phần bằng chuyển khoản 50.000đ, tự báo bếp phần chưa gửi. Tồn thực tế còn 20ml, không còn nguyên liệu đang giữ. Tiền mặt ca B giữ nguyên 50.000đ đầu ca.
7. A chốt ca: dự kiến 325.000đ. Số dự kiến cũ bị từ chối; thực đếm thiếu tiền phải có lý do.
8. A mở ca mới 200.000đ và hoàn hóa đơn cũ 125.000đ. Gửi lại hoàn tiền không tạo chi lần hai; điểm tích được đảo. Ca mới còn dự kiến 75.000đ, ca cũ giữ nguyên 325.000đ; nguyên liệu đã dùng không hoàn lại.
9. Báo cáo đối chiếu được doanh số 175.000đ, hoàn 125.000đ, doanh thu ròng 50.000đ, công nợ nhà cung cấp 6.000đ.
10. Thử cập nhật khách/số khách cùng lúc thanh toán: cả ba thao tác tuân theo khóa giao dịch, không có lỗi 500 và không ghi đè hóa đơn đã thanh toán.

## Phân quyền

Đã gọi từng endpoint nội bộ với tài khoản không quyền và không đăng nhập: 105 cặp route/phương thức đều trả 403 và 401 tương ứng, trước khi xử lý dữ liệu gửi lên. API đăng nhập và thực đơn QR công khai được kiểm tra riêng, không thuộc danh sách endpoint nội bộ này.

Các kiểm tra khác xác nhận: Admin có toàn bộ quyền; quyền cá nhân từ chối thắng quyền nhóm; quyền phụ thuộc bị gỡ nếu quyền nền bị tắt; nhóm tùy chỉnh rỗng không được seed cấp quyền lại; quyền đổi trên cùng JWT có hiệu lực ngay; phiên bị khóa/đổi mật khẩu mất hiệu lực; nhân viên được giao quản lý tài khoản không thể tạo hoặc sửa Admin ngoài phạm vi quyền; lịch sử đổi quyền được ghi lại.

## Kết quả kiểm tra

- WorkflowChecks: 49 trường hợp qua API đang chạy.
- AccessChecks: 388 trường hợp về quyền và truy cập endpoint.
- ReservationChecks: 28; ShiftChecks: 27; OrdersChecks: 43.
- WarehouseChecks: 27; FinancialChecks với `--database`: 19.
- TableChecks: 30; CustomerChecks: 48.
- Tổng 659 kiểm tra tự động đạt. Build backend và frontend thành công. Frontend còn cảnh báo kích thước bundle, không ảnh hưởng kết quả chạy; tối ưu tải trang thuộc công việc sau.

Kiểm tra trình duyệt bằng tài khoản demo riêng: thu hồi quyền thêm khách khi form đang mở, thu hồi quyền giảm giá khi đang nhập 10%, thu hồi quyền Bếp/Bar khi đang ở trang bếp, ẩn nút tạo đơn khi mất quyền; đều cập nhật mà không đăng nhập lại.

Đã thử phiên khác thêm món khiến hóa đơn từ 25.000đ thành 50.000đ trong lúc hộp thanh toán mở: lần xác nhận theo số cũ bị chặn, đơn tải lại và chỉ thu 50.000đ sau xác nhận lần nữa. Ảnh: `workflow-payment-review.png`.

## Bạn test lại nhanh

1. Mở cùng một bàn ở hai trình duyệt/tài khoản. Ở A mở thanh toán; B thêm món. A xác nhận: phải báo đơn thay đổi, tải số tiền mới, chưa thanh toán.
2. Đăng nhập nhân viên, mở form thêm nhanh khách. Admin tắt quyền thêm khách. Trong vài giây, form trở về tìm kiếm và không còn nút thêm nhanh.
3. Nhân viên mở thanh toán, nhập giảm 10%. Admin tắt quyền giảm giá: giá trở về không giảm, ô giảm giá bị khóa.
4. Nhân viên đang xem Bếp/Bar. Admin tắt quyền xem bếp: trang tự chuyển về chức năng còn được phép.
5. Tắt quyền thêm món/tạo đơn nhưng giữ quyền xem lịch sử đơn: nút tạo đơn mang về không xuất hiện. Người còn quyền thanh toán vẫn thanh toán đơn có sẵn với thông tin khách/số khách không đổi.

Quyền UI đồng bộ mỗi 3 giây khi trang đang hiển thị, hoặc khi quay lại tab. Backend kiểm tra quyền hiện tại trên mỗi request. Request đã được cấp quyền và đang chạy trước lúc thu hồi không bị hủy hồi tố.

`expectedTotal` là trường tùy chọn để giữ tương thích với các client API cũ; frontend hiện tại luôn gửi trường này. Client tích hợp mới cần gửi số tiền đã hiển thị để có cơ chế đối chiếu tương tự.
