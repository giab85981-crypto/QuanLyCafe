# Trợ lý quán – sử dụng, kết nối Gemini và kiểm tra

## Bạn dùng ở đâu?

Khởi động lại backend bằng F5, chạy frontend như bình thường, đăng nhập admin và mở **Trợ lý AI** trên thanh điều hướng. Thu ngân được cấp quyền có thể mở từ menu thu ngân.

Bạn có thể thử ngay các câu hỏi gợi ý khi chưa có API key. Khi đó giao diện ghi rõ **Tra cứu cơ bản**, số liệu vẫn lấy thật từ database. Đây chưa phải chế độ hiểu câu hỏi bằng Gemini.

## Kết nối AI thật (chỉ cần làm một lần)

1. Đăng nhập Google tại [Google AI Studio](https://aistudio.google.com/app/apikey). Tạo API key theo [hướng dẫn Google](https://ai.google.dev/gemini-api/docs/api-key). Chọn dự án dùng Free Tier nếu muốn thử miễn phí; hạn mức và mô hình phụ thuộc tài khoản.
2. Trong Visual Studio, nhấn chuột phải project **CafeManagement.API** → **Manage User Secrets / Quản lý bí mật người dùng**.
3. Thêm cấu hình sau vào file secrets mở ra. Thay phần đánh dấu bằng API key vừa tạo. Nếu file đã có cấu hình khác, giữ các cấu hình đó và thêm thuộc tính Assistant.

```json
{
  "Assistant": {
    "ApiKey": "DAN_API_KEY_CUA_BAN_VAO_DAY"
  }
}
```

4. Lưu file, dừng backend rồi F5 lại. Trang Trợ lý AI sẽ hiện **Đã cấu hình Gemini**. Gửi một câu hỏi để kiểm tra kết nối; nhãn câu trả lời **Gemini** cho biết câu hỏi đã được AI phân loại thành công. Nhận xét chỉ hiện khi lần gọi tạo nhận xét thành công.

Không đặt key vào appsettings.json, frontend hoặc GitHub; không cần gửi key trong cuộc trò chuyện. File User Secrets nằm ngoài source project. User Secrets phù hợp phát triển local, không phải kho bí mật mã hóa cho triển khai thật.

Mô hình mặc định: `gemini-3.5-flash-lite`, chọn từ [bảng giá Google](https://ai.google.dev/gemini-api/docs/pricing). Có thể sửa `Assistant:Model` nếu Google đổi mô hình hoặc tài khoản của bạn dùng mô hình khác. `Assistant:Enabled=false` tắt gọi Google. Gemini Free Tier có hạn mức; không bật trả phí chỉ để chạy thử đồ án. Dữ liệu ở dịch vụ miễn phí có thể được Google dùng để cải thiện sản phẩm theo chính sách trên trang giá; tính năng này chỉ gửi câu hỏi và các báo cáo tổng hợp được phép xem, không gửi hồ sơ khách/nhân viên, token, mật khẩu hay connection string.

## Phạm vi bản đầu

- Doanh thu: số tiền đã thu sau giảm giá/đổi điểm, hoàn tiền ghi nhận trong kỳ và doanh thu thuần. Doanh thu không phải lợi nhuận.
- Số hóa đơn đã thanh toán, số hóa đơn hoàn trong kỳ; không tính đơn đang phục vụ hay đơn đã đóng chưa thanh toán.
- Top 5 món theo số lượng bán, giá trị phân bổ trước hoàn theo số tiền đã thu.
- Nguyên liệu sắp hết: tồn còn dùng trừ phần đã giữ cho đơn chưa báo bếp, bỏ lô hết hạn, so với mức tối thiểu. Tồn kho là hiện tại; tối đa 10 dòng.
- Khoảng thời gian: hôm nay, hôm qua, 7/30 ngày qua (gồm hôm nay), tuần này từ thứ Hai, tháng này đến hôm nay. So sánh doanh thu/số đơn với kỳ liền trước cùng số ngày; các ngày hiển thị rõ trong câu trả lời.
- Tính theo giờ Việt Nam. Kỳ đang diễn ra chưa hoàn tất, nên thận trọng khi so với kỳ trước.
- Mỗi câu hỏi độc lập: hỏi đầy đủ nội dung và khoảng thời gian, không dùng câu tiếp nối như “còn hôm qua thì sao?”. Chưa hỗ trợ ngày tùy chọn, lợi nhuận, nợ nhà cung cấp, dự báo hay thao tác ghi dữ liệu.
- Cuộc trò chuyện giữ trong bộ nhớ trang, tối đa 20 lượt. Không lưu lịch sử vào database hoặc trình duyệt; đổi tài khoản/quyền sẽ làm mới cuộc trò chuyện.

## Phân quyền

Quyền mới **Trợ lý AI → Dùng trợ lý tình hình quán** (`AI_VIEW`) chỉ cho mở trang, không tự cấp quyền số liệu. Admin toàn quyền; các nhóm/nhân viên khác được cấp tại trang Phân quyền.

- Doanh thu/món bán chạy: cần Xem tổng quan hoặc Xem báo cáo.
- Đơn đã thanh toán: cần Xem đơn hàng, Xem tổng quan hoặc Xem báo cáo.
- Tồn kho: cần Xem kho hoặc Xem báo cáo.
- Câu hỏi tổng quan chỉ bao gồm các loại dữ liệu được phép xem.
- Quyền và trạng thái tài khoản được kiểm tra lại trước đọc số liệu và trước trả kết quả, để xử lý cả trường hợp quyền đổi trong thời gian chờ Gemini.

## Cách hoạt động và giới hạn

Gemini chọn một thao tác đọc và khoảng thời gian trong danh sách cố định, không viết SQL hay tự mở database. Backend kiểm tra quyền rồi tính số liệu bằng cùng bộ tính báo cáo của đồ án. Số liệu luôn hiển thị riêng với nguồn và thời gian cập nhật. Lần gọi AI thứ hai chỉ viết nhận xét định tính; đầu ra có chữ số bị bỏ để tránh đưa số không được kiểm chứng vào nhận xét. Nhận xét vẫn là gợi ý tham khảo, không thay thế số liệu.

Mỗi câu hỏi AI tối đa 2 lần gọi, mỗi lần timeout 20 giây; tối đa 6 câu hỏi/phút/tài khoản, câu hỏi tối đa 500 ký tự. Không gửi lại tự động. Khi thiếu key, lỗi mạng, key/mô hình sai hoặc hết hạn mức, câu hỏi cơ bản được tra cứu tại backend và có thông báo rõ. Câu ngoài phạm vi trả hướng dẫn, không tự suy diễn.

## Kiểm tra đã thực hiện

- 40 kiểm tra trợ lý: adapter Gemini bằng phản hồi mô phỏng, xử lý quota/phản hồi lỗi, giới hạn thao tác/khoảng thời gian, loại nhận xét có số chưa kiểm chứng, doanh thu/hoàn tiền, so sánh số đơn, tồn khả dụng trừ giữ nguyên liệu, phân quyền HTTP, thu hồi quyền, câu hỏi quá dài và rate limit.
- 400 kiểm tra phân quyền toàn backend sau khi thêm quyền và endpoint mới.
- Frontend build thành công, lint không có lỗi (còn cảnh báo React từ các phần đã có).
- Trình duyệt: mở từ menu, câu hỏi gợi ý, Enter, cuộc trò chuyện mới, số liệu có nguồn, sáng/tối, viewport điện thoại 390px không tràn ngang.
- Dùng database test/demo riêng và dọn sau kiểm tra; không sửa dữ liệu quán.
- Chưa gọi Gemini thật vì chưa có API key. Kiểm tra trực tiếp với Google cần hoàn tất bước kết nối phía trên.

## Kịch bản demo

1. Hỏi “Doanh thu hôm nay bao nhiêu?” và đối chiếu Tổng quan.
2. Hỏi “So sánh doanh thu 7 ngày qua với kỳ liền trước”.
3. Hỏi “Hôm nay có bao nhiêu đơn đã thanh toán?”.
4. Hỏi “Món nào bán chạy trong tháng này?”.
5. Hỏi “Nguyên liệu nào sắp hết?” và đối chiếu Kho hàng.
6. Cấp một nhân viên quyền AI và chỉ quyền Kho hàng: câu hỏi tồn kho được trả lời, doanh thu bị từ chối. Thu hồi quyền và hỏi lại.
7. Hỏi “Xóa đơn hàng giúp tôi” hoặc một ngày ngoài phạm vi để chứng minh chatbot không thay đổi dữ liệu hay đoán số liệu.

Tài liệu tích hợp chính thức: [Structured output](https://ai.google.dev/gemini-api/docs/structured-output), [Generate content](https://ai.google.dev/api/generate-content).
