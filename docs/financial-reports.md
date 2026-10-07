# Báo cáo và Sổ quỹ

## Chạy
Dừng backend cũ rồi F5 lại. Chạy frontend bằng npm run dev và tải lại trang. Không cần migration hoặc chạy lại database cho thay đổi này.
Admin vào menu Báo cáo / Sổ quỹ. Nhân viên phải được cấp REPORT_VIEW hoặc CASHBOOK_VIEW; lập phiếu cần CASHBOOK_CREATE.

## Chức năng
- Báo cáo: doanh thu theo ngày, tiền hoàn, doanh thu thuần, giá vốn và lãi gộp; món bán phân bổ sau giảm giá / đổi điểm; dòng tiền theo phương thức; tồn kho hiện tại và công nợ nhà cung cấp hiện tại; tìm kiếm và xuất Excel.
- Sổ quỹ: đối chiếu lũy kế trước kỳ, thu / chi trong kỳ, lũy kế cuối kỳ theo tiền mặt / chuyển khoản / chưa rõ phương thức. Tổng theo bộ lọc hiển thị riêng với tổng toàn kỳ.
- Hóa đơn cũ chưa có phiếu thu vẫn hiển thị để đối chiếu, không tính vào tổng tiền đã ghi sổ.
- Dashboard doanh thu thuần và báo cáo cùng tính hoàn tiền vào ngày hoàn. Biểu đồ hiển thị được khoản âm.

## Quy tắc số liệu
Bán hàng ghi ở ngày thanh toán, kể cả hóa đơn về sau được hoàn. Khoản hoàn ghi vào ngày hoàn, không xóa doanh thu lịch sử ngày bán. Giá vốn dùng giá chụp ở dòng món, giữ nguyên khi hoàn vì luồng hiện tại không trả lại nguyên liệu đã dùng.
Lãi gộp = doanh thu sau giảm giá / đổi điểm - hoàn tiền trong kỳ - giá vốn món thanh toán trong kỳ. Chưa trừ lương, thuê mặt bằng và chi phí vận hành. Giá vốn phụ thuộc số liệu đã lưu trên dòng món.
Doanh thu ước tính của hóa đơn cũ được ghi rõ trong báo cáo; không coi đó là tiền thực nhận trong Sổ quỹ. Các món vẫn được tính là đã phục vụ khi hoàn tiền, bảng món ghi rõ doanh thu trước hoàn.
Lũy kế quỹ tính từ giao dịch đầu tiên đã ghi sổ; muốn đối chiếu với tiền thực tế cần ghi tiền ban đầu bằng phiếu thu thích hợp. Không cộng các phương thức chưa xác định vào tiền mặt.
Tồn kho và công nợ là số liệu hiện tại, không chạy theo bộ lọc ngày doanh thu. Giá trị tồn theo từng lô, gồm lô hết hạn chưa xuất hủy. Tồn còn hạn chưa trừ phần giữ cho đơn chưa báo bếp. Phiếu nhập cũ không theo dõi thanh toán được báo số lượng, không tự quy thành công nợ.
Danh sách Đơn hàng thống kê hóa đơn theo trạng thái và ngày thanh toán; Báo cáo thống kê bán hàng / hoàn tiền theo ngày phát sinh tương ứng. Khác phạm vi thống kê nên bộ lọc Đơn hàng có thể không bằng doanh thu thuần của báo cáo.

## Test thủ công
1. Thanh toán một hóa đơn 100.000đ, giảm 10%: báo cáo bán hàng 90.000đ, Sổ quỹ thu 90.000đ; bảng món phân bổ tổng 90.000đ.
2. Hoàn hóa đơn: tiền chi hoàn ghi một lần; báo cáo có hoàn 90.000đ. Nếu bán và hoàn khác ngày, ngày bán giữ doanh thu; ngày hoàn có khoản giảm doanh thu. Dashboard khớp khi chọn cùng khoảng ngày.
3. Lập phiếu thu tiền mặt 500.000đ, phiếu chi 20.000đ: dòng tiền ghi thu 500.000đ, chi 20.000đ, chênh lệch 480.000đ. Đổi bộ lọc phương thức trong Sổ quỹ và đối chiếu tổng theo bộ lọc.
4. Nhập hàng 1.000.000đ, trả 300.000đ: Sổ quỹ chi 300.000đ; công nợ hiện tại còn 700.000đ. Trả thêm 200.000đ: công nợ còn 500.000đ.
5. Chọn tồn kho: đối chiếu lô hết hạn, số lượng, giá vốn với Kho hàng. Chọn xuất Excel và mở file trong Excel để kiểm tra số tiền là số.
6. Tài khoản chỉ xem báo cáo không được lập phiếu; tài khoản chỉ xem Sổ quỹ không được vào Báo cáo.

## Kiểm tra đã chạy
- Backend Release / frontend build.
- FinancialChecks: 19 kiểm tra tính tiền, hoàn khác ngày, giá lịch sử, cuối ngày, dữ liệu cũ, dòng tiền, công nợ, lô hết hạn và truy vấn SQL.
- WarehouseChecks: 27; OrdersChecks: 43; AccessChecks: 145; DashboardChecks đạt toàn bộ.
- Các kiểm tra SQL dùng database GUID tạm và tự dọn; database quán không thay đổi.
- Trình duyệt: bốn tab báo cáo, tạo phiếu thu ở database demo và đối chiếu số tiền với báo cáo dòng tiền. Công cụ trình duyệt không trả được sự kiện tải file Excel; phần tải Excel cần xác nhận thủ công theo bước 5.
