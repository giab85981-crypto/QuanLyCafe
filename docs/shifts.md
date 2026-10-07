# Ca làm việc và chốt ca thu ngân

## Chạy và cấp quyền

Khởi động lại backend bằng F5 để tự cập nhật database, chạy frontend rồi tải lại trang.

Admin có toàn bộ quyền. Với nhóm Thu ngân đã được cấu hình từ trước, vào Phân quyền, bật **Ca làm việc → Xem / mở / chốt ca của mình**, lưu lại. Hệ thống giữ các quyền đã cấu hình, không tự cấp thêm quyền cho nhóm cũ. Thu ngân cần mở ca trước khi thanh toán hóa đơn, kể cả chuyển khoản.

Hai quyền dành cho người quản lý là **Xem ca và giao dịch của mọi nhân viên** và **Chốt ca của nhân viên**. Quyền chốt ca người khác cần quyền xem mọi ca. Người chỉ có quyền ca của mình không xem hoặc chốt ca của người khác, kể cả khi gọi API trực tiếp.

## Quy trình sử dụng

1. Vào Ca làm việc hoặc nút Mở ca trên màn hình Thu ngân. Nhập tiền mặt đầu ca và ghi chú. Số 0 được chấp nhận.
2. Bán hàng bình thường. Hóa đơn thuộc ca của tài khoản thực hiện thanh toán. Phiếu thu thanh toán được liên kết cùng ca.
3. Phiếu thu/chi thủ công, trả nhà cung cấp và hoàn tiền được liên kết với ca đang mở của người thực hiện. Nếu người đó chưa mở ca, những giao dịch này hiển thị Ngoài ca trong Sổ quỹ.
4. Chọn ca để xem hóa đơn, các khoản thu/chi, tiền mặt dự kiến và chuyển khoản. Có lọc lịch sử, xuất Excel giao dịch và in đối chiếu.
5. Chốt ca: nhập tiền thực đếm. Nếu thiếu hoặc dư tiền, bắt buộc ghi lý do. Ca đã chốt giữ cố định số liệu và không nhận thêm giao dịch.

Đăng xuất không tự chốt ca. Các đơn chưa thanh toán vẫn được giữ sau khi chốt ca; khi thanh toán sau đó, đơn thuộc ca mới của người thanh toán.

## Cách tính

**Tiền mặt dự kiến = tiền đầu ca + thu tiền mặt − chi tiền mặt.** Chuyển khoản được thống kê riêng và không cộng vào tiền mặt thực đếm.

Tiền đầu ca là tiền có sẵn trong ngăn kéo, không tạo phiếu thu mới và không tăng doanh thu. Vì vậy tiền dự kiến của một ca không nhất thiết bằng số dư toàn bộ Sổ quỹ.

Hóa đơn hoàn vào ca sau vẫn giữ doanh số của ca bán ban đầu; tiền hoàn ghi chi ở ca thực hiện hoàn. Báo cáo theo ngày ghi nhận tiền hoàn theo ngày hoàn. Giao dịch và hóa đơn cũ trước nâng cấp không được tự gán vào ca mới.

Mỗi tài khoản có tối đa một ca đang mở. Đây là ca theo tài khoản, chưa phải mô hình nhiều máy thu ngân dùng chung một két vật lý. Nếu nhiều người dùng chung ngăn kéo, cần quy ước bàn giao để không kê trùng tiền đầu ca.

## Bài test demo

- Mở ca với 200.000đ. Thanh toán một hóa đơn tiền mặt 25.000đ, một hóa đơn chuyển khoản 35.000đ và ghi chi tiền mặt 5.000đ bằng cùng tài khoản. Tiền mặt dự kiến phải là 220.000đ; chuyển khoản thu 35.000đ hiển thị riêng.
- Nhập thực đếm 219.000đ: hệ thống báo thiếu 1.000đ. Không nhập lý do thì không chốt được; nhập lý do rồi chốt thành công.
- Thu ngân chỉ thấy ca của mình. Quản lý có quyền xem mọi ca nhưng chưa có quyền chốt thay thì không được chốt ca nhân viên.
- Sau khi chốt, mở ca mới và hoàn một hóa đơn cũ: ca cũ giữ nguyên, ca mới có khoản chi hoàn tiền.
- Nếu có giao dịch mới trong lúc đang nhập chốt ca, hệ thống kiểm tra lại số dự kiến và yêu cầu cập nhật trước khi chốt. Giao dịch phát sinh sau khi ca đóng không được thêm vào ca đã chốt.

## Kiểm tra đã thực hiện

Build backend và frontend thành công. Kiểm tra tự động trên database tách riêng: 27 kiểm tra ca, 169 kiểm tra phân quyền, 43 kiểm tra đơn hàng, 27 kiểm tra kho và 19 kiểm tra tài chính đều đạt.

Kiểm tra giao diện thực tế: mở ca 200.000đ, thanh toán hóa đơn tiền mặt 25.000đ, đối chiếu dự kiến 225.000đ, chốt thực đếm 224.000đ với lý do thiếu 1.000đ. Ảnh minh họa: `shifts-preview.png`.

Các kiểm tra không xác nhận hộp thoại in hoặc tệp tải Excel trên máy người dùng; bạn có thể thử hai nút này khi demo.
