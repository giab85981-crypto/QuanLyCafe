# Gọi món qua QR phòng/bàn

## Chức năng

- Khách quét QR của bàn, tìm/lọc món, chọn size, topping, số lượng, ghi chú và gửi giỏ món.
- Giỏ hiển thị giá tạm tính. Backend tự đối chiếu món và giá, không tin giá khách gửi.
- Thu ngân có nút **QR** kèm số yêu cầu chờ ở đầu màn hình Bán hàng. Danh sách tự cập nhật mỗi 5 giây, có tên bàn, món, tùy chọn, số lượng, ghi chú và tổng tiền.
- Thu ngân xác nhận toàn bộ yêu cầu hoặc từ chối kèm lý do. Quyền duyệt dùng quyền động **POS_ORDER / Thêm món**, không gán cứng theo tên vai trò. Người chỉ có quyền xem hoặc Bếp/Bar không được duyệt.
- Xác nhận thành công thêm món vào hóa đơn đang mở của bàn hoặc tạo hóa đơn nếu chưa có. Hệ thống kiểm tra và giữ nguyên liệu ngay khi xác nhận; báo bếp và thanh toán tiếp tục dùng luồng hiện có.
- Khách tự thấy trạng thái chờ, đã nhận, từ chối hoặc hết hạn. Mã riêng lưu trên trình duyệt để tải lại vẫn theo dõi được. Không dùng mã hóa đơn hoặc thông tin khách khác làm mã tra cứu.
- Ghi chú khách được đưa vào dòng món và chuyển sang Bếp/Bar.

## Chạy và test trên máy

1. Dừng phiên debug cũ rồi F5 backend. Migration `QrGuestOrdering` tự thêm bảng yêu cầu QR khi backend khởi động. Không cần chạy SQL thủ công.
2. Khởi động lại frontend bằng `npm run dev` để nhận cấu hình API proxy mới.
3. Mở Phòng/Bàn → QR của một bàn → mở link. Có thể dùng cửa sổ ẩn danh làm màn hình khách.
4. Chọn món, size/topping nếu có; nhập ghi chú rồi gửi yêu cầu.
5. Trong Bán hàng, bấm **QR**. Đối chiếu đúng bàn rồi **Xác nhận & thêm món**.
6. Khách phải thấy đã xác nhận. Mở bàn đó tại Bán hàng: món có đủ giá/tùy chọn/ghi chú. Bấm Báo bếp; Bếp/Bar phải thấy món và ghi chú.
7. Gửi một yêu cầu khác và từ chối kèm lý do: khách phải thấy lý do, hóa đơn không thêm món.
8. Kiểm tra thiếu nguyên liệu: yêu cầu vẫn được gửi, nhưng lúc duyệt sẽ báo thiếu; không thêm một phần giỏ. Nhân viên có thể từ chối và nhờ khách chọn lại.

## Quét bằng điện thoại trong quán

- Điện thoại và máy chạy frontend dùng cùng mạng Wi-Fi/LAN.
- Chạy frontend: `npm run dev -- --host 0.0.0.0`.
- Trong Phòng/Bàn, ô **Địa chỉ quán cho mã QR** nhập địa chỉ LAN thật của máy, ví dụ `http://192.168.1.20:5173` (thay bằng IP máy của bạn), rồi tải/in QR. `localhost` chỉ mở được trên chính máy đang chạy.
- Frontend gọi `/api` cùng địa chỉ; Vite chuyển tiếp tới backend HTTPS `https://localhost:7053` trên máy chủ. Backend vẫn chạy bằng profile **https** như thường lệ.
- Nếu đã đặt `VITE_API_URL` trong môi trường, dùng `/api` cho demo LAN để tránh điện thoại gọi localhost của nó. Có thể đặt `API_PROXY_TARGET` nếu backend dùng địa chỉ khác.
- Khi triển khai bản build, cấu hình web server chuyển `/api` tới backend hoặc đặt `VITE_API_URL` thành địa chỉ API công khai. Proxy Vite chỉ áp dụng lúc chạy dev.

## Giới hạn hiện tại

- Khách gửi yêu cầu, nhân viên xác nhận; chưa phải khách tự thanh toán hoặc tự báo bếp.
- Mỗi yêu cầu tối đa 30 dòng, 100 phần; mỗi dòng tối đa 20 phần. Tối đa 10 loại topping/dòng, mỗi loại 20 phần. Ghi chú tối đa 300 ký tự.
- Yêu cầu chờ hết hạn sau 30 phút. Một bàn tối đa 20 yêu cầu đang chờ; giới hạn 20 lần gửi/phút theo nguồn kết nối và bàn, tránh một bàn chặn các bàn khác khi đi qua proxy.
- Khi món/giá/tùy chọn đổi sau lúc khách gửi, cần từ chối và nhờ khách gửi lại để xác nhận giá mới.
- Mã gửi chống trùng cả khi gửi lại sau lỗi mạng và khi hai thu ngân cùng xác nhận. Khi lỗi mạng/máy chủ chưa rõ kết quả, giỏ gửi được giữ nguyên và gửi lại bằng cùng mã.

## Kiểm tra thực hiện

- `tests/QrOrderingChecks`: 37 kiểm tra HTTP + database thật trên database tạm, tự dọn sau khi chạy. Bao gồm gửi trùng đồng thời, tra cứu riêng, quyền duyệt, giá bị sửa, size/topping, giữ nguyên liệu, rollback toàn giỏ, từ chối, hết hạn, đổi giá, bàn ngừng và giới hạn gửi độc lập theo bàn.
- `WorkflowChecks`: 49 kiểm tra nghiệp vụ bán hàng đạt.
- `AccessChecks`: 394 kiểm tra phân quyền đạt; migration khớp model.
- Backend Release và frontend build thành công. Frontend còn cảnh báo kích thước bundle lớn; dự án test có cảnh báo không tải được dữ liệu kiểm tra lỗ hổng NuGet trong môi trường hiện tại.
- Trình duyệt: khách gửi → thu ngân xác nhận → khách cập nhật; tải lại vẫn theo dõi được; gửi mới và từ chối có lý do. Kiểm tra khung điện thoại rộng 390px: thực đơn, hộp chọn món, giỏ và nút gửi không tràn ngang. Kiểm tra gọi API qua proxy cùng địa chỉ frontend đạt.
- Chưa quét trên điện thoại thật trong Wi-Fi của bạn; làm theo phần LAN để kiểm tra kết nối tại máy bạn.

Ảnh kiểm tra: `qr-cashier-review.png`, `qr-guest-review.png`. Dữ liệu demo nằm trong database riêng; không sửa database đang dùng của quán trong các bài test.
