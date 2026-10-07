# Giữ nguyên liệu khi nhận món

- Thêm món: giữ nguyên liệu của size và topping theo công thức đã chụp trong dòng đơn. Không giảm tồn thực tế, không ghi doanh thu.
- Khả dụng để nhận thêm = tồn còn hạn - nguyên liệu giữ cho mọi đơn đang phục vụ, chưa báo bếp.
- Báo bếp: xuất đúng phần mới, giải phóng phần giữ tương ứng. Báo lại không xuất thêm.
- Hủy món chưa gửi: giảm phần giữ. Hủy món đã gửi vẫn áp dụng hoàn kho khi chờ làm, ghi hao hụt khi đã làm.
- Thanh toán: tự gửi phần chưa báo trong cùng giao dịch với xuất kho, tích/đổi điểm và ghi tiền. Nếu lỗi, toàn bộ bước mới được hoàn tác.
- Chuyển/gộp/tách giữ nguyên tổng lượng đã giữ. Hai yêu cầu đồng thời được tuần tự hóa bằng khóa giao dịch trong SQL Server, kể cả nhiều tiến trình API.
- Xuất/hủy/kiểm kê không dùng phần còn hạn đã hứa cho khách. Nguyên liệu đang giữ không được ngừng sử dụng. Nếu phần còn hạn đã hết hạn trong khi đơn đang mở, báo bếp/thanh toán bị chặn để nhân viên xử lý kho hoặc hủy món.

Phần giữ được tính từ Count - SentCount và IngredientsJson của BillInfo trong đơn Status = 0. Dữ liệu đã lưu trong database nên khởi động lại không mất phần giữ; không cần migration mới. Món không có công thức không thể kiểm soát nguyên liệu: POS ghi rõ chưa giới hạn theo công thức.

## Test thủ công

1. Tạo nguyên liệu có tồn còn hạn 100 ml và món có công thức dùng 10 ml mỗi phần.
2. Bàn A thêm 7 phần: tồn thực tế 100 ml, đang giữ 70 ml, còn nhận 3 phần. Bàn B đặt 4 phần bị chặn; đặt 3 phần được.
3. Hủy 2 phần chưa báo ở A: đang giữ tổng 80 ml, tồn vẫn 100 ml.
4. Báo bếp A: xuất 50 ml, đang giữ 30 ml cho B, còn nhận 2 phần.
5. Thêm 2 phần ở A rồi thanh toán trực tiếp: tự tạo phiếu cho 2 phần mới, xuất thêm 20 ml một lần; B vẫn được giữ 30 ml.
6. Thử xuất hoặc kiểm kê giảm vào phần giữ: báo lỗi. Hủy món B chưa báo: phần giữ được trả lại.

Đã đạt 28 kiểm tra riêng về giữ nguyên liệu, đồng thời, hết hạn và rollback thanh toán; 20 kiểm tra thực đơn, 41 đơn hàng, 27 kho, 30 phòng/bàn và 48 khách hàng. Backend và frontend build thành công. Các kiểm tra SQL chạy trên database GUID riêng rồi dọn database thử nghiệm.
