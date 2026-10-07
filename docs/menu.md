# Thực đơn và luồng bếp – kho

## Chạy ứng dụng

1. Dừng phiên backend đang chạy trong Visual Studio (Shift+F5), rồi F5 lại để nạp code mới.
2. Trong thư mục `frontend`, chạy `npm run dev`; mở `http://localhost:5173`.
3. Nếu LocalDB báo Stopped, chạy `sqllocaldb start MSSQLLocalDB` bằng cùng tài khoản đang mở Visual Studio.
4. Backend tự áp dụng migration trước khi seed. Database demo hiện tại đã cập nhật hai migration thực đơn.

## Chức năng

- Danh sách/bảng hoặc thẻ món, tìm tên/mã, lọc nhóm, loại, phân loại, trạng thái, yêu thích và topping; phân trang 12 món.
- Thêm/sửa ảnh JPG/PNG/WebP tối đa 2 MB, tên, mã duy nhất, mô tả, giá bán, giá vốn, trạng thái.
- Thêm/đổi tên/xóa nhóm và loại. Không xóa phân nhóm đang chứa món; đổi tên loại cập nhật các món liên quan.
- Topping là một món được đánh dấu riêng, có giá và công thức riêng; không hiện độc lập trên POS. Gắn topping được phép cho từng món, chọn đến 20 phần mỗi topping khi gọi.
- Size có giá, trạng thái và công thức riêng. Size bị bỏ khỏi biểu mẫu được ngừng bán, giữ khóa để bảo toàn hóa đơn cũ.
- Giá vốn tính theo định lượng × giá vốn nguyên liệu; khi không có công thức dùng giá vốn nhập tay.
- Tồn khả dụng là số phần tối đa theo công thức. Món có size hiển thị số phần lớn nhất giữa các size đang bán, chưa tính topping tùy chọn. Món không có công thức hiển thị dấu —.
- Nguyên liệu: thêm/sửa tên, đơn vị, số lượng, mức cảnh báo và giá vốn mỗi đơn vị; mọi thay đổi tồn được ghi nhật ký. Không đổi đơn vị nguyên liệu đã có công thức/giao dịch.
- Excel: tải mẫu, xuất các món đang lọc, nhập tối đa 500 món/lần, file tối đa 5 MB. Kiểm tra lỗi trước khi xác nhận; không nhập dở một phần.
- Excel dùng dữ liệu cơ bản (mã, tên, nhóm, loại, phân loại, giá, trạng thái, mô tả). Tạo nhóm/loại trước; mã trùng bị chặn. Size, ảnh, topping và công thức cấu hình trong màn hình sửa món.
- Xóa món có lịch sử bán/bếp hoặc đang dùng làm topping sẽ chuyển thành ngừng bán. Món chưa có lịch sử được xóa thật.

## Gọi món, bếp và kho

- POS chỉ hiện món đang bán; có hộp thoại size và số phần topping. Dòng hóa đơn lưu giá bán, giá vốn, tùy chọn và định lượng tại thời điểm gọi.
- Mỗi lần báo bếp chỉ gửi số phần mới chưa gửi. Bấm lại khi không có món mới không tạo phiếu hoặc trừ kho.
- Kiểm tra nguyên liệu cho toàn bộ đợt mới trong transaction. Thiếu một nguyên liệu thì không xuất bất kỳ nguyên liệu nào trong đợt đó.
- Bếp/Bar trong POS hiển thị phiếu đang làm: Chờ → Đang làm → Hoàn thành. Có nút làm mới.
- Bấm dấu − giảm món chưa báo bếp trực tiếp. Món đã báo bếp cần nhập lý do hủy.
- Hủy món đang chờ hoàn kho. Hủy món đang làm/hoàn thành ghi hao hụt; không cộng lại kho và không trừ lần nữa.
- Nhật ký 200 giao dịch gần nhất phân biệt báo bếp, hoàn kho, hao hụt, điều chỉnh và nhập kho.
- Thanh toán chặn nếu còn món chưa báo bếp, cộng đúng giá size/topping và giảm giá hóa đơn. Hủy hết món có thể “Đóng bàn trống”, hóa đơn hủy không tính doanh thu.
- Món không có công thức vẫn bán và báo bếp được, nhưng không xuất nguyên liệu. Các phiếu bếp cũ trước tính năng này không có lịch sử xuất kho để hồi tố.

## Dữ liệu demo đã chuẩn bị

- `Sữa tươi (demo)`: 1.000 ml, giá vốn 30 đ/ml, mức cảnh báo 100 ml.
- `Kem sữa (demo)`: giá 5.000 đ/phần; mỗi phần dùng 10 ml sữa.
- `Latte (demo)`: size M 35.000 đ / 100 ml; size L 45.000 đ / 150 ml. Topping cho phép: Kem sữa.
- `Bàn demo thực đơn`: bàn trống, dùng để kiểm tra luồng mới.
- Đã chạy một lượt gọi L + 2 kem, báo bếp và hủy trước chế biến: xuất 170 ml rồi hoàn 170 ml. Tồn sữa đã trở về 1.000 ml, bàn trống, không tạo doanh thu.

## Kịch bản test nhanh

1. Vào Thực đơn, tìm “demo”, mở Latte; kiểm tra size M/L, công thức và topping.
2. Vào Bán hàng, chọn Bàn demo thực đơn. Gọi Latte size L + 2 phần Kem sữa: hóa đơn 55.000 đ, giá vốn 5.100 đ.
3. Trước báo bếp, tồn thực tế sữa vẫn 1.000 ml nhưng 170 ml đã giữ cho đơn, còn nhận thêm từ 830 ml. Báo bếp: tồn 830 ml, phần giữ của món này được giải phóng. Bấm báo bếp lần nữa: tồn vẫn 830 ml.
4. Bấm −, nhập lý do hủy khi phiếu chưa bắt đầu làm: sữa trở lại 1.000 ml. Đóng bàn trống.
5. Gọi lại, báo bếp, mở Bếp/Bar → Bắt đầu chế biến. Hủy dòng món: sữa vẫn 830 ml, nhật ký có hao hụt 170 ml.
6. Thêm món mới sau khi đã báo bếp: lần báo tiếp chỉ xuất phần mới.
7. Điều chỉnh sữa còn ít hơn công thức, thử báo bếp: bị chặn, số lượng và tồn kho không thay đổi.
8. Gọi món rồi đổi giá/công thức thực đơn: dòng đã gọi giữ giá và định lượng cũ; dòng gọi mới dùng cấu hình mới.
9. Báo bếp rồi thanh toán với giảm giá 10%: L + 2 kem cần trả 49.500 đ. Dashboard nhận doanh thu từ giá dòng hóa đơn.
10. Nhập Excel theo file mẫu; thử mã trùng hoặc nhóm không tồn tại để xem lỗi từng dòng trước nhập.

## Kiểm tra kỹ thuật

- `dotnet build CafeManagement.API -c Release --no-restore`
- `dotnet run --project tests/MenuChecks -c Release`: 17 kiểm thử tích hợp với SQL Server, database kiểm thử riêng tự xóa.
- `dotnet run --project tests/DashboardChecks -c Release`: kiểm tra doanh thu, phân nhóm, migration và parser LocalDB.
- `npm run build`, `npm run lint` trong `frontend`.
- Đã kiểm tra trực tiếp bằng trình duyệt: sửa/lưu món có size, gọi size/topping, báo bếp hai lần, hủy/hoàn kho, đóng bàn trống, nhật ký kho.

Backend vẫn có 3 cảnh báo nullable cũ trong Auth/Account. Frontend còn cảnh báo lint ở các màn hình cũ và cảnh báo effect tải dữ liệu; không có lỗi build.
