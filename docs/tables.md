# Phòng/Bàn – hướng dẫn chạy và test

## Chạy

Code đã lưu trên ổ đĩa. Visual Studio báo file thay đổi bên ngoài: chọn Yes to All nếu bạn không có sửa riêng chưa lưu. Shift+F5 rồi F5 để chạy backend mới; migration lịch sử bàn tự chạy khi khởi động. Frontend tiếp tục `npm run dev` tại thư mục frontend, mở http://localhost:5173.

Nếu LocalDB dừng, Package Manager Console: `sqllocaldb start MSSQLLocalDB`, rồi F5. Không xóa database.

## Chức năng đã hoàn thiện

- Khu vực: thêm/sửa, ngừng hoạt động/kích hoạt lại; xóa khu vực trống. Khu vực có bàn đang phục vụ không được ngừng hoạt động.
- Phòng/bàn: thêm/sửa tên, ghế, thứ tự, khu vực, ghi chú, trạng thái hoạt động. Không xóa lịch sử bàn; bỏ hoạt động qua form sửa.
- Hiển thị thẻ hoặc bảng, tìm theo tên/khu vực/ghi chú/số ghế, lọc bàn trống/đang phục vụ/ngừng, phân trang. Các chỉ số số bàn và sức chứa.
- Trạng thái trống/có người dựa trên hóa đơn mở, tránh sửa trạng thái trái với đơn hiện tại.
- Nút Bán hàng mở đúng bàn. Thao tác chuyển/gộp/tách dùng được từ Phòng/Bàn và từ Bán hàng.
- Chuyển toàn bộ: chuyển sang bàn trống hoặc thêm vào đơn của bàn đang phục vụ. Chuyển sang bàn trống giữ nguyên mã hóa đơn.
- Gộp: chuyển toàn bộ món sang bàn đang có hóa đơn. Đơn nguồn đóng không tính doanh thu, bàn nguồn trống.
- Tách: chọn số lượng từng dòng món chuyển sang bàn khác; bàn nguồn phải còn ít nhất một phần. Hai bàn thanh toán riêng. Tách toàn bộ dùng chuyển/gộp.
- Mỗi thao tác giữ giá bán/giá vốn, size, topping, công thức đã lưu, trạng thái bếp. Không cộng lại món có giá khác hoặc gộp dòng mất thông tin.
- Tách ưu tiên phần chưa báo bếp. Phần đã báo chuyển liên kết phiếu bếp và phần nguyên liệu đã tiêu thụ; không trừ tồn lần nữa. Hủy phần chờ sau tách hoàn đúng lô; hủy phần đã làm ghi hao hụt.
- Lịch sử 200 thao tác gần nhất: nguồn/đích, hóa đơn, món/số lượng, thời gian, tài khoản, ghi chú.
- Chặn đơn đã thanh toán, bàn đích ngừng, chuyển về chính bàn, dữ liệu món hoặc hóa đơn đích đã thay đổi. Gửi lại cùng yêu cầu không thực hiện lần thứ hai.
- Excel: mẫu, nhập bàn mới có xem trước, file tối đa 5 MB và 500 dòng; lỗi bất kỳ dòng không nhập một phần. Tạo khu vực trước, nhập tên khu vực đúng như danh sách. Xuất theo bộ lọc.
- QR từng bàn: xem/tải PNG; bộ QR của bàn đang hoạt động trong bộ lọc tải dưới dạng HTML để mở và Ctrl+P in.
- Thực đơn công khai theo bàn: tìm món, nhóm món, giá size và topping; không cần đăng nhập, không lộ giá vốn/công thức. Đặt món qua nhân viên, chưa có gọi món tự động.

## Dữ liệu và test nhanh

Mình tạo hai bàn **Bàn chuyển tách A (demo)**, **Bàn chuyển tách B (demo)**, đều trống. Đã thử 2 Latte size M, báo bếp, tách 1 sang B rồi gộp lại A, hủy cả hai khi còn chờ để hoàn kho, đóng đơn trống. Lịch sử tách/gộp vẫn giữ; đơn thử không ghi thu. Tồn sữa sau hoàn bằng tồn trước thử (850 ml tại thời điểm kiểm tra).

1. Phòng/Bàn → lọc Bàn trống → bàn A → Bán hàng. Thêm 2 Latte M hoặc món bạn muốn thử.
2. Báo bếp → Chuyển/gộp/tách → Tách → chọn B → số lượng chuyển 1 → Xác nhận. Mỗi bàn có 1 phần, giá và trạng thái bếp giữ nguyên, kho không đổi do thao tác tách.
3. Bàn B → gộp về A: bàn A có 2 dòng 1 phần, B trống. Hai dòng được giữ để không mất lịch sử giá/bếp.
4. Chuyển A sang bàn trống: mã đơn không đổi, bếp hiện bàn đích.
5. Hủy món chờ chế biến sau tách/gộp: nguyên liệu hoàn đúng phần; bắt đầu chế biến trước khi hủy: ghi hao hụt, không hoàn và không trừ thêm.
6. Thanh toán hai đơn sau tách: mỗi hóa đơn tạo một khoản thu Sổ quỹ; thao tác bàn không tạo thu/chi.
7. Sửa bàn đang phục vụ → bỏ Đang hoạt động: phải bị chặn. Khi thanh toán/đóng đơn trống thì cho ngừng. Mở lại bằng form sửa.
8. Nhập Excel số ghế 0 hoặc tên trùng: báo lỗi dòng, không thêm một phần file.
9. QR bàn A → mở liên kết để xem thực đơn không đăng nhập. Ngừng hoạt động bàn/khu vực thì liên kết không phục vụ nữa.

## Số khách, giảm giá và QR trên điện thoại

- Gộp cộng số khách nếu cả hai đơn đã biết; một bên chưa biết thì tổng chưa xác định.
- Tách có thể nhập số khách chuyển, cần còn ít nhất 1 khách ở nguồn. Nếu bỏ trống, hai đơn chưa xác định số khách; nhập lại trước thanh toán.
- Hai đơn có khách hàng khác nhau hoặc giảm giá đã lưu khác nhau không gộp trực tiếp. Giảm giá đang nhập trên màn hình POS nhưng chưa thanh toán cần kiểm tra/nhập lại sau thao tác bàn.
- Địa chỉ QR mặc định là frontend đang mở. localhost chỉ dùng trên máy này; điện thoại cần cùng mạng LAN hoặc địa chỉ triển khai.
- Để demo LAN, chạy frontend `npm run dev -- --host 0.0.0.0`, backend phải được phục vụ ở địa chỉ LAN, và cấu hình `VITE_API_URL` trỏ tới API mà điện thoại truy cập được. Nhập địa chỉ frontend LAN vào ô Địa chỉ quán cho mã QR trước khi tải/in QR. Cấu hình HTTPS/firewall theo môi trường máy, không tự đổi các thiết lập này.
- Các bàn cũ đang có IsActive=false vẫn ngừng hoạt động; không tự kích hoạt lại dữ liệu cũ. Nếu bàn cũ có số ghế 0, nhập số ghế hợp lệ khi sửa/kích hoạt.

## Kiểm tra tự động

```
dotnet run --project tests/TableChecks -c Release
dotnet run --project tests/MenuChecks -c Release
dotnet run --project tests/WarehouseChecks -c Release
cd frontend
npm run build
npm run lint
```

TableChecks dùng database tạm GUID, tự xóa khi kết thúc. Bao gồm 28 kiểm tra: giữ tiền/tồn, hoàn sau tách khi trước đó đã hủy một phần, trạng thái bếp sau chuyển, idempotency, đơn cũ đã đổi, ngừng bàn/khu vực, Excel và dữ liệu thực đơn công khai.
