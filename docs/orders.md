# Đơn hàng, đơn mang về và Bếp/Bar

Source đã được lưu trực tiếp trong dự án. Khởi động lại backend bằng Shift+F5 rồi F5, chạy frontend bằng npm run dev và chọn Đơn hàng. Nếu Visual Studio hỏi tải lại file, chọn Yes to All. Database tự cập nhật migration OrdersAndRefunds khi backend chạy; không cần xóa hoặc tạo lại database.

Nếu LocalDB đang dừng, chạy trong Package Manager Console:

    sqllocaldb start MSSQLLocalDB

## Chức năng

- Danh sách hóa đơn tại quán và mang về: tìm mã HD, tên bàn, khách hàng hoặc ghi chú; lọc thời gian, trạng thái, phương thức thanh toán và bàn; phân trang; thống kê theo toàn bộ bộ lọc.
- Chi tiết: món, size/topping, số lượng, số đã báo bếp, giá lưu khi gọi món, giảm giá, tổng tiền, người thu, lịch sử chuyển/gộp/tách và thu/hoàn tiền. Tên món và tên bàn được lưu để hóa đơn đã thanh toán không đổi theo lần sửa thực đơn/bàn sau đó.
- Tiếp tục phục vụ đơn mở từ trang Đơn hàng. Đơn tại bàn mở đúng bàn; đơn mang về mở đúng hóa đơn riêng.
- Đơn mang về không chiếm bàn. Có thể tạo từ Đơn hàng (nhãn khách, ghi chú) hoặc nút + Mang về trên POS. Đơn mang về không dùng chuyển/gộp/tách bàn.
- Bếp/Bar riêng tại tab Đơn hàng: tìm bàn/món/mã đơn, lọc Chờ/Đang làm, tự làm mới mỗi 15 giây, chuyển Chờ → Đang làm → Hoàn thành. Phiếu hoàn thành rời danh sách đang chế biến.
- In tạm tính cho đơn mở, in hóa đơn đã thanh toán/đã hoàn. Bản in có món, tùy chọn, giảm giá, phương thức, ghi chú và tình trạng hoàn tiền. Dùng hộp thoại in của trình duyệt để chọn máy in hoặc Save as PDF.
- Xuất Excel theo toàn bộ bộ lọc, không chỉ trang đang xem; số tiền lưu dạng số. Giới hạn 10.000 hóa đơn mỗi lần xuất.
- Hủy toàn bộ hóa đơn đã thanh toán: bắt buộc lý do, chọn tiền mặt/chuyển khoản để ghi chi hoàn tiền trong Sổ quỹ. Giữ nguyên phiếu thu và lịch sử món/giá. Nếu hóa đơn cũ có số thực thu nhưng chưa có phiếu thu riêng, hệ thống ghi lại phiếu thu ở thời điểm thanh toán cũ trước khi ghi hoàn, để số dư không bị lệch.

## Quy tắc tiền và kho

Thêm món giữ nguyên liệu theo công thức đã lưu trong dòng đơn, chưa xuất kho thực tế. Kho khả dụng trừ phần đã giữ của tất cả đơn đang phục vụ. Báo bếp chỉ xuất phần mới chưa báo và giải phóng phần giữ tương ứng. Thanh toán tự báo bếp các món chưa gửi trong cùng giao dịch; lỗi thanh toán không để lại phiếu bếp hay xuất kho dở dang. Chờ chế biến mà hủy món trong đơn mở thì hoàn đúng nguyên liệu/lô; đang làm hoặc đã xong thì ghi hao hụt, không xuất kho lần thứ hai.

Hủy hóa đơn đã thanh toán được xem là hoàn tiền cho món đã bán: giữ nguyên lượng nguyên liệu đã sử dụng, không tự hoàn kho. Hóa đơn chuyển sang trạng thái Đã hủy hoàn tiền và bị loại khỏi doanh thu, số hóa đơn bán và hiệu quả thực đơn. Sổ quỹ giữ cả khoản thu gốc lẫn khoản chi hoàn tiền. Hủy hóa đơn cũ không làm trống bàn đang có một đơn mới.

Không cho sửa món/giá của hóa đơn đã thanh toán. Yêu cầu thanh toán, tạo đơn mang về và hoàn tiền gửi lại không tạo thêm khoản thu/chi hoặc đơn trùng. Phiếu bếp của hóa đơn đã hoàn tiền không thể mở lại.

Các hóa đơn mẫu cũ chưa lưu số thực thu được ghi nhãn Ước tính. Tổng tiền và thống kê có thể dùng dữ liệu còn lại để ước tính, nhưng cột đã trả không giả định đã thu số đó. Hệ thống chặn tự động hoàn tiền cho các hóa đơn chưa có số thực thu xác thực. Hóa đơn giảm 100% vẫn giữ tổng bằng 0.

## Test nhanh

1. Vào Đơn hàng → + Đơn mang về. Đặt nhãn Demo của tôi, thêm một Latte (demo), chọn size M.
2. Báo bếp. Vào Đơn hàng → Bếp/Bar: phiếu phải hiện nhãn Demo của tôi, M và số lượng đúng. Bắt đầu chế biến rồi Hoàn thành. Phiếu rời danh sách bếp.
3. Vào tab Hóa đơn, mở đơn và bấm Tiếp tục phục vụ. Nhập giảm giá 10%, chọn Chuyển khoản, thanh toán. Một Latte M 35.000đ sẽ thu 31.500đ.
4. Mở lại hóa đơn: kiểm tra món, size M, giảm 3.500đ và tổng 31.500đ. Bấm In hóa đơn, có thể chọn Save as PDF.
5. Bấm Hủy và hoàn tiền, nhập lý do và chọn phương thức. Xác nhận. Hóa đơn phải ghi Đã hủy hoàn tiền và số hoàn 31.500đ. Không còn nút hủy lần thứ hai.
6. Vào Sổ quỹ: phải có Thu 31.500đ và Chi hoàn 31.500đ cùng mã HD; doanh thu hóa đơn này còn 0. Kho giữ lượng đã dùng cho Latte.
7. Quay lại Đơn hàng, thử tìm mã HD, lọc Mang về, lọc Đã hủy hoàn tiền, chọn Toàn thời gian và Xuất Excel.
8. Với đơn tại quán, bấm Bán tại bàn hoặc mở đơn đang phục vụ để vào đúng bàn. Chuyển/gộp/tách vẫn hoạt động như trước.
9. Tạo một đơn mang về trống, bấm Đóng đơn trống trên POS: không có phiếu thu/chi.

## Dữ liệu demo đã kiểm tra

Đã để lại HD000010 — Mang về · kiểm thử hoàn tiền (demo), gồm một Latte M 35.000đ, giảm 10%, đã thu và hoàn 31.500đ bằng chuyển khoản. Có hai phiếu Sổ quỹ cùng tham chiếu HD000010. Đây là giao dịch demo, không chuyển tiền qua ngân hàng. Nguyên liệu cho một Latte đã được ghi dùng đúng quy tắc; không tự hoàn kho sau khi hoàn tiền.

Hóa đơn HD000001 và HD000002 là dữ liệu cũ chưa lưu thực thu, được hiển thị ước tính và không cho tự động hoàn tiền.

## Kiểm tra đã chạy

- 41 kiểm thử vòng đời Đơn hàng trên database thử độc lập, tự dọn sau khi chạy: đơn mang về, báo bếp, trạng thái, giá/tên lịch sử, thanh toán, retry, hoàn tiền, hóa đơn cũ, Sổ quỹ và Dashboard.
- 28 kiểm thử Phòng/Bàn, 20 Thực đơn, 27 Kho đều đạt.
- Kiểm thử Dashboard và bộ phân tích LocalDB đạt; Swagger tạo được 56 đường dẫn API.
- Backend build thành công (còn ba cảnh báo nullable cũ ở Auth/Account); frontend build thành công; lint không có lỗi, còn cảnh báo React.
- Kiểm tra trình duyệt: tạo đơn mang về → chọn size → báo bếp → Chờ/Đang làm/Hoàn thành → thanh toán giảm giá → hủy hoàn tiền → đối chiếu Sổ quỹ.
- File Excel tải thực tế có 7 hóa đơn, số hoàn 31.500 là dữ liệu số, doanh thu hóa đơn hủy là 0, hai hóa đơn cũ có dấu ước tính.
- Nội dung bản in đã kiểm tra có món/size, giảm giá và tình trạng đã hoàn; chưa in ra máy in vật lý.

Lệnh chạy kiểm thử:

    dotnet run --project tests/OrdersChecks -c Release
    dotnet run --project tests/TableChecks -c Release
    dotnet run --project tests/MenuChecks -c Release
    dotnet run --project tests/WarehouseChecks -c Release
    dotnet run --project tests/DashboardChecks -c Release
    dotnet run --project tests/SwaggerChecks -c Release

Trong thư mục frontend:

    npm run build
    npm run lint
