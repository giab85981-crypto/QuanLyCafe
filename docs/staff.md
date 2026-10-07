# Nhân viên

Đã triển khai hồ sơ nhân viên và tài khoản đăng nhập cho quán. Chưa triển khai chấm công, ca làm, tính lương, ứng lương hoặc công nợ nhân viên.

## Chạy và thử

Dừng backend đang chạy, tải lại những file Visual Studio báo đã đổi bên ngoài, rồi F5. Frontend chạy `npm run dev`, mở http://localhost:5173/ và đăng nhập admin như cũ. Migration EmployeeManagement đã được áp dụng cho database demo; máy khác tự áp dụng khi backend khởi động.

Vào Nhân viên (nếu thanh menu hẹp, cuộn thanh menu ngang). Các hồ sơ demo: Mai (thu ngân), Nam (pha chế), Duy (phục vụ chưa có tài khoản), admin.

1. Thêm nhân viên: họ tên, điện thoại/email, giới tính, ngày sinh, địa chỉ, bộ phận, chức danh, ngày vào làm, ghi chú. Không bắt buộc tạo tài khoản.
2. Mở Hồ sơ để sửa thông tin và xem lịch sử thay đổi. Mã NV tự sinh; tài khoản đã gắn không được chuyển/gỡ để giữ lịch sử.
3. Tìm tên/mã/điện thoại/tài khoản, lọc bộ phận/chức danh/trạng thái; danh sách chia trang 12 người.
4. Cấp đăng nhập: username 3–100 ký tự chữ/số/chấm/gạch, mật khẩu ít nhất 8 ký tự và tối đa 72 byte; chọn vai trò từ backend. Hoặc gắn tài khoản chưa thuộc ai trong Hồ sơ.
5. Tài khoản: đổi vai trò hoặc đặt mật khẩu mới; để trống mật khẩu để giữ nguyên. Khóa đăng nhập vẫn giữ trạng thái nhân viên đang làm.
6. Nghỉ việc giữ hồ sơ/giao dịch và khóa tài khoản. Đi làm lại không tự mở khóa tài khoản; cần bấm Mở đăng nhập riêng.
7. Xuất Excel → Tải file Excel: xuất toàn bộ kết quả trong bộ lọc (không chỉ trang hiện tại), không xuất mật khẩu.

## Vai trò cố định

- Admin: toàn bộ chức năng; quản lý hồ sơ, tài khoản và đặt lại mật khẩu. Không được tự khóa/hạ quyền đang dùng, hoặc khóa/hạ quyền quản trị viên hoạt động cuối cùng.
- Cashier (Thu ngân): chỉ màn hình POS, chọn/tạo nhanh khách, báo bếp, chuyển/gộp/tách bàn và thanh toán. Không truy cập quản lý, lịch sử đơn hàng, Sổ quỹ hoặc xem/cập nhật Bếp / Bar. Quyền được kiểm tra ở cả giao diện và API.
- Kitchen (Bếp / Pha chế): màn hình Bếp / Bar, xem phiếu, Chờ → Đang làm → Hoàn thành. Không đọc hóa đơn, tạo phiếu báo bếp xuất kho hoặc truy cập POS/nhân viên.

Các giới hạn được kiểm tra ở backend, kể cả gọi API trực tiếp. Giao diện chuyển tới màn hình phù hợp theo vai trò. Chưa có trình sửa quyền riêng cho từng tài khoản; các dòng Permission cũ được giữ để tương thích.

Đổi mật khẩu/vai trò hoặc khóa làm token cũ mất hiệu lực. Mở lại tài khoản không khôi phục token cũ; cần đăng nhập lại. Mật khẩu được băm BCrypt; API danh sách/hồ sơ/lịch sử không trả mật khẩu hoặc hash. Không xóa cứng tài khoản hoặc hồ sơ.

## Tài khoản demo thêm trong lần này

- `staff.cashier` / `DemoCafe123`: Mai, Thu ngân, tự vào POS.
- `staff.kitchen` / `DemoCafe123`: Nam, Bếp, tự vào màn hình chế biến.

Admin demo trước đây có IsActive=false nhưng backend cũ vẫn cho đăng nhập. Lần này đã mở lại admin, giữ nguyên mật khẩu, ghi lịch sử. Migration chỉ phục hồi một admin khi không có admin hoạt động nào.

## Kiểm tra

- 30 kiểm tra hồ sơ/tài khoản trên database tạm riêng: tạo/gắn tài khoản, kiểm tra dữ liệu, tự khóa/hạ quyền, quản trị cuối cùng, nghỉ/đi làm lại, BCrypt byte limit, đổi mật khẩu, ghi lịch sử và nâng cấp dữ liệu admin cũ.
- 28 kiểm tra HTTP thực tế: 401/403 đúng quyền, token cũ bị thu hồi sau khóa/mở lại/đổi mật khẩu/đổi vai trò, QR menu vẫn công khai. HttpDemoChecks.ps1 dùng backend http://localhost:7063 và tài khoản demo, không dùng làm bộ test cho database thật.
- 48 kiểm tra Khách hàng và 41 kiểm tra Đơn hàng đều qua. Swagger sinh được 69 đường dẫn.
- Backend Release build không có lỗi/cảnh báo. Frontend build thành công; còn cảnh báo React Fast Refresh/effect cũ và kích thước bundle, không phải lỗi build.
- Giao diện đã thử thêm hồ sơ Duy, lưu ngày sinh/ngày vào làm, tìm kiếm, tạo link xuất Excel và đăng nhập hai vai trò. Không thay đổi phiếu bếp/hóa đơn cũ khi kiểm tra giao diện.
