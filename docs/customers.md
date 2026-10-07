# Khách hàng — hướng dẫn sử dụng và test

Frontend, backend và migration `20261006070833_CustomerManagementAndLoyalty` đã hoàn thiện. Database demo QuanLyCafe_V2 đã cập nhật; dữ liệu cũ được giữ. Khách cũ có mã/ngày tạo/trạng thái hoạt động; hóa đơn cũ không tự được cộng điểm hồi tố.

## Chức năng

- Trang Khách hàng trên thanh điều hướng, phân trang đầy đủ thay vì giới hạn 10 người của tìm kiếm POS.
- Thêm/sửa tên, điện thoại, email, ngày sinh, giới tính, địa chỉ, nhóm, ghi chú; tự tạo mã KH. Điện thoại chuẩn hóa +84/84 về 0, bỏ dấu ngăn và khoảng trắng; chặn trùng tại API/database. Kiểm tra email, ngày sinh tương lai và độ dài dữ liệu.
- Nhóm Khách thường/VIP có sẵn; thêm/sửa nhóm, chỉ xóa nhóm trống.
- Ngừng hoạt động/kích hoạt lại, giữ hóa đơn/điểm/lịch sử. Khách ngừng hoạt động không được chọn thanh toán mới.
- Tìm mã/tên/điện thoại/email, lọc nhóm, trạng thái, giới tính, tháng sinh, ngày tạo, chi tiêu; sắp xếp mới nhất/tên/chi tiêu.
- Chi tiết khách có thông tin, lịch sử mua và sổ điểm có phân trang; mã hóa đơn mở chi tiết tại Đơn hàng. Hóa đơn cũ thiếu thực thu được đánh dấu, không suy diễn số tiền vào chi tiêu khách.
- POS chọn/thay/bỏ khách, thêm nhanh tên/điện thoại. Lưu khách vào đơn phục vụ; tên/điện thoại được chụp lại khi thanh toán để hóa đơn cũ không đổi theo hồ sơ.
- Nhập xlsx/xls/csv: file mẫu, kiểm tra/xem trước và báo dòng lỗi, kiểm tra lại, mỗi lần tối đa 500 khách/5 MB. Chỉ thêm khách mới; file lỗi không nhập một phần; không ghi đè điểm.
- Xuất toàn bộ bộ lọc, tối đa 10.000 khách. Điện thoại dạng văn bản; tiền/điểm dạng số. Bấm Xuất Excel rồi Tải file Excel. Tải file mẫu cũng chuẩn bị một liên kết tải.

## Tiền và điểm

1. Giảm phần trăm trước, sau đó trừ điểm: 1 điểm = 100đ. Điểm đổi nguyên không âm, không vượt số dư dương hoặc tiền hóa đơn sau giảm giá.
2. Cộng floor(thực trả / 10.000) điểm sau thanh toán. Chỉ đổi điểm từ lần mua trước; khách lẻ không tích/đổi điểm.
3. Hoàn tiền đúng thực trả, trả điểm đã đổi và thu hồi điểm đã cộng. Sổ điểm ghi thay đổi ròng = điểm trả lại − điểm thu hồi; một điều chỉnh ròng 0 vẫn có lịch sử.
4. Nếu điểm đã được dùng ở đơn khác trước khi hoàn đơn cũ, số dư có thể âm. Chưa thể đổi thêm; điểm tích ở lần mua sau bù lại. Đây không phải nợ tiền.
5. Transaction và khóa cập nhật số dư giúp hai đơn đồng thời không đổi cùng số điểm. Bấm thanh toán/hoàn tiền lại không tạo thêm tiền/điểm.
6. Trả hết bằng điểm là hóa đơn thực thu 0 hợp lệ, không phải ước tính dữ liệu cũ. Không tạo phiếu thu/chi tiền giả; hoàn hóa đơn đó chỉ hoàn điểm.
7. Doanh thu, Dashboard, Sổ quỹ và báo cáo doanh thu tính sau đổi điểm. In/chi tiết hóa đơn tách giảm phần trăm, giảm điểm và điểm tích.
8. Quán thanh toán đủ; chưa có mua chịu/thu nợ khách. Hoàn tiền hóa đơn đã trả giữ nguyên tồn kho theo quy tắc Đơn hàng đã chốt.

## Chạy lại

1. Visual Studio: Shift+F5. Nếu thông báo source thay đổi ngoài editor, chọn Yes to All nạp source đã lưu.
2. F5 chạy backend; migration tự chạy khi khởi động.
3. Frontend: npm run dev, mở http://localhost:5173 rồi vào Khách hàng.
4. Nếu LocalDB dừng, Package Manager Console: sqllocaldb start MSSQLLocalDB, rồi F5 lại.

## Demo đã test trực tiếp

- KH000001 Lan · khách demo, 0906000001, VIP, sinh 15/01/2000: hiện 3 điểm, 1 lần mua thành công, chi tiêu thực thu 35.000đ.
- HD000011 Latte M 35.000đ tích 3 điểm. Phiếu bếp demo đã hoàn tất.
- HD000012 Latte M 35.000đ đổi 3 điểm giảm 300đ, thực trả 34.700đ, tích 3 điểm. Đã hoàn 34.700đ và điều chỉnh điểm ròng 0, khách giữ 3 điểm từ HD000011. Hóa đơn hoàn vẫn có trong lịch sử.
- KH000002 Minh và KH000003 An được nhập từ Excel; chưa có hóa đơn/điểm.
- customer-import-demo.xlsx là file đã nhập: nhập lại báo trùng điện thoại. Đổi điện thoại mới để test lại.
- customer-export-demo.xlsx xuất từ dữ liệu thực tế bằng cùng hàm frontend, đã đọc ngược kiểm tra đủ 3 khách, đúng tiền/điểm/ngày sinh và số 0 đầu điện thoại.

## Test cho bạn

1. Thêm khách mới; thử trùng điện thoại, sai email, ngày sinh tương lai. Sửa và lọc theo nhóm/tháng sinh.
2. Bấm Lan → Lịch sử mua/điểm. Mở HD000012: kiểm tra 300đ đổi điểm và hoàn 34.700đ.
3. POS → đơn mới → thêm món → chọn khách → Báo bếp → Thanh toán; kiểm tra điểm tăng.
4. Đơn sau đổi điểm; kiểm tra tiền thực trả và Sổ quỹ. Hoàn tiền tại Đơn hàng rồi xem doanh thu/sổ điểm.
5. Ngừng hoạt động khách test, xác nhận không được chọn tại POS; kích hoạt lại.
6. Nhập file với điện thoại mới; thử file trùng/lỗi. Xuất bộ lọc và bấm Tải file Excel bằng Chrome.

## Kiểm tra

- CustomerChecks: 48 đạt trên database tạm riêng, gồm CRUD/nhóm/import/filter, nhận/đổi/hoàn điểm, retry, điểm âm, hai đơn đồng thời, thực thu 0, doanh thu và Sổ quỹ.
- OrdersChecks 41 đạt; TableChecks 28 đạt; Dashboard 15 kiểm tra chính và 5 LocalDB đạt; Swagger sinh được 63 đường dẫn.
- Backend Release build thành công, còn 3 cảnh báo nullable có sẵn ở Auth/Account. Frontend build/lint không lỗi, còn cảnh báo React và dung lượng bundle.
- Giao diện thật đã thử thêm/sửa/ngày sinh, hai lần thanh toán, đổi và hoàn điểm, chi tiết hóa đơn, nhập Excel và chuẩn bị xuất.
- Trình duyệt nhúng chưa xác nhận được tải file xuống. Workbook đã tạo/đọc ngược bằng cùng hàm xuất và lưu demo; thử tải xuống trên Chrome ở localhost:5173.

Ảnh minh chứng: customers-preview.png, customers-points-preview.png, customers-pos-preview.png.
