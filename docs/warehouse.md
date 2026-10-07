# Kho hàng & Sổ quỹ

## Chạy phần mềm

Code đã lưu trên ổ đĩa. Nếu Visual Studio báo file thay đổi từ bên ngoài, chọn **Yes to All** nếu không có sửa riêng chưa lưu; nếu có, giữ lại bản sửa để đối chiếu trước. Khi hỏi chuẩn hóa xuống dòng có thể chọn Yes.

1. Dừng backend cũ bằng Shift+F5 rồi F5 lại. Migration kho tự chạy khi backend khởi động. Không cần xóa database hay dữ liệu cũ.
2. Frontend: `cd frontend`, `npm run dev`, mở `http://localhost:5173` như trước.
3. Đăng nhập, chọn **Kho hàng** hoặc **Sổ quỹ** trên thanh menu.
4. Nếu LocalDB lại dừng: trong Package Manager Console, chạy `sqllocaldb start MSSQLLocalDB`, rồi F5.

## Chức năng

- Nguyên liệu: mã tự sinh, nhóm, đơn vị cơ sở, quy đổi đơn vị mua, định mức cảnh báo, ngừng sử dụng và kích hoạt lại. Tìm kiếm, phân trang, lọc tồn thấp/hết/hết hạn/trạng thái; giá trị tồn theo lô.
- Nhóm nguyên liệu: thêm, đổi tên, xóa nhóm trống.
- Nhà cung cấp: tên, điện thoại, địa chỉ, trạng thái, tổng mua và công nợ.
- Nhập hàng: nhiều dòng, mua theo kg/lít/thùng với quy đổi, giá theo đơn vị mua, mã lô, hạn dùng tùy chọn; thanh toán toàn bộ hoặc một phần bằng tiền mặt/chuyển khoản.
- Phiếu nhập: chi tiết số lượng mua và cơ sở, giá mua, lô, lịch sử trả tiền; nút Trả nợ. Chặn trả vượt nợ, bấm gửi lại không ghi thêm lần nữa.
- Xuất ngoài bán hàng / hủy: lý do bắt buộc, tự chọn lô hoặc chỉ định lô. Lô hết hạn chỉ được hủy hoặc điều chỉnh qua kiểm kê.
- Kiểm kê: tồn thực tế, chênh lệch và lịch sử trước/sau. Chặn nếu tồn thay đổi sau khi mở form; đóng và mở lại để lấy số liệu mới.
- Lô hàng: tồn còn lại, giá vốn và hạn dùng. Bếp và xuất thường ưu tiên hạn gần nhất, không dùng lô đã hết hạn. Không ghi hạn được xếp sau các lô có hạn.
- Lịch sử kho: nhập, xuất, hủy, kiểm kê, báo bếp, hoàn nguyên liệu, hao hụt tại bếp; tối đa 1.000 thay đổi gần nhất.
- Excel: tải mẫu, kiểm tra trước khi nhập nguyên liệu mới; file tối đa 5 MB, 500 nguyên liệu. Không ghi đè tồn hiện tại. Xuất Excel theo tab và bộ lọc.
- Sổ quỹ: ghi tiền thu bán hàng khi thanh toán, ghi tiền thực trả nhà cung cấp, phiếu thu/chi thủ công, lọc ngày/loại/phương thức/tìm kiếm, xuất Excel.

## Dữ liệu demo đã thử trên giao diện

- Nguyên liệu **Cà phê hạt (kho demo)**: đơn vị g, quy đổi 1 kg = 1.000 g, định mức 500 g.
- **Nhà cung cấp demo**, phiếu **PN1**: 2 kg × 200.000đ = 400.000đ; trả lúc nhập 150.000đ tiền mặt, trả thêm 50.000đ chuyển khoản. Đã trả 200.000đ, còn nợ 200.000đ.
- Lô **CA-PHE-DEMO**: nhập 2.000 g; kiểm kê hao hụt 50 g, còn 1.950 g, giá vốn 200đ/g. Các lần thao tác có lịch sử.
- Hai khoản chi liên kết PN1 xuất hiện trong Sổ quỹ. Đây là dữ liệu ghi sổ demo, không thực hiện chuyển tiền qua ngân hàng.

## Cách test nhanh

1. Kho hàng → Nhập hàng → PN1 → Chi tiết: kiểm tra quy đổi 2 kg = 2.000 g và hai khoản thanh toán.
2. Trả nợ 10.000đ → công nợ còn 190.000đ; Sổ quỹ thêm đúng một khoản chi 10.000đ. Thử trả 200.001đ trước khi trả khoản này sẽ bị chặn vì vượt công nợ 200.000đ.
3. Xuất / hủy → chọn cà phê, xuất 100 g, ghi lý do: tồn từ 1.950 xuống 1.850 g. Thử xuất quá tồn: toàn bộ phiếu bị chặn.
4. Kiểm kê → cà phê → nhập tồn thực tế 1.800 g: lịch sử ghi chênh lệch −50 g. Không thay đổi công nợ hoặc tiền chi.
5. Thêm phiếu nhập mới với hạn dùng khác nhau; xem lô. Lô hết hạn có thể mô phỏng bằng hạn hôm nay rồi kiểm tra ngày tiếp theo. Không cho nhập lô đã hết hạn.
6. Bán hàng → món có công thức → Báo bếp: trừ nguyên liệu một lần; báo lại không trừ. Hủy khi chờ: hoàn đúng lô. Hủy khi đang làm/hoàn thành: ghi hao hụt và không trừ thêm. Thanh toán bằng tiền mặt/chuyển khoản: Sổ quỹ ghi đúng tiền sau giảm giá.
7. Tải mẫu Excel, đổi tên/mã rồi nhập: xem trước hợp lệ mới xác nhận. Thử số lượng âm hoặc dòng không hợp lệ: không nhập một phần file.

## Quy tắc dữ liệu

- Tồn theo đơn vị cơ sở. Công thức món dùng đơn vị cơ sở; quy đổi kg/lít/thùng nhập ở nguyên liệu.
- Giá vốn hiện tại là bình quân gia quyền các lô còn tồn. Tổng giá trị tồn bao gồm lô hết hạn; số lượng có thể dùng loại trừ lô hết hạn. Hao hụt sau chế biến là sự kiện ghi nhận, không phải một lần giảm tồn thứ hai.
- Phiếu đã xác nhận giữ nguyên để đối chiếu. Điều chỉnh tồn bằng phiếu mới; chưa có chức năng trả hàng nhập hoặc đảo thanh toán.
- Phiếu nhập cũ chưa lưu số tiền đã trả hiển thị **chưa ghi nhận/chưa xác định**, không tự tính thành nợ. Hóa đơn cũ chưa lưu phương thức hiển thị **chưa ghi nhận**; số tiền ước tính được đánh dấu.
- Sổ quỹ ghi tổng thu/chi của khoảng ngày, không thay thế báo cáo lợi nhuận và không suy đoán số dư đầu kỳ. Trả nợ qua phiếu nhập để liên kết công nợ; phiếu chi thủ công không giảm nợ nhà cung cấp.
- Một kho mặc định, chưa quản lý chuyển nhiều kho. Các tài khoản đăng nhập đều truy cập chức năng; phân quyền chi tiết theo vai trò là bước riêng.

## Kiểm tra tự động

Các bộ test dùng database tạm tên GUID và tự xóa khi kết thúc:

```
dotnet run --project tests/WarehouseChecks -c Release
dotnet run --project tests/MenuChecks -c Release
dotnet run --project tests/DashboardChecks -c Release -- --database
cd frontend
npm run build
npm run lint
```

Dừng backend Release đang chạy trước khi build Release để tránh khóa file.


Tồn kho hiển thị riêng phần đang giữ cho đơn chưa báo bếp và phần có thể nhận thêm (tồn còn hạn trừ đang giữ). Xuất, hủy và kiểm kê giảm không được lấy phần còn hạn đã giữ. Hủy lô hết hạn vẫn thực hiện được khi không ảnh hưởng phần giữ còn hạn. Ngừng sử dụng nguyên liệu đang giữ bị chặn.
