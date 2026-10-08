# Sơ đồ database cho báo cáo

Mở **index.html** bằng trình duyệt; không cần chạy backend hoặc có mạng. Bộ sơ đồ được đối chiếu từ `CafeManagement.API/Migrations/AppDbContextModelSnapshot.cs`: **37 bảng nghiệp vụ, 50 ràng buộc khóa ngoại**. Chưa kiểm tra cấu trúc SQL Server đang chạy.

## Cách đưa vào báo cáo

1. Phần thiết kế tổng quan: dùng danh sách 7 nhóm, không nhét mọi cột của 37 bảng vào một trang.
2. Phần thiết kế chi tiết: mỗi nhóm dùng một hình SVG riêng. Ảnh SVG giữ chữ rõ khi phóng lớn.
3. Bảng đầy đủ cột / kiểu dữ liệu: tra ở “Tra cấu trúc bảng”, hoặc lấy từ schema.json.
4. Muốn PDF: chọn nhóm rồi dùng “In / lưu PDF”; bộ in mặc định A4 ngang. Sơ đồ đơn hàng có nhiều bảng nên có thể chọn A3 ngang nếu cần chữ lớn.
5. `all-tables.mmd` là sơ đồ toàn bộ để lưu phụ lục / tra cứu, không nên dùng làm hình chính trên slide. Các file .mmd là mã Mermaid có thể chỉnh tiếp.

## Danh sách hình

1. **Tài khoản, phân quyền và nhân viên (8 bảng)**: [Ảnh SVG](01-access.svg) · [Mã Mermaid](01-access.mmd)
2. **Thực đơn và định lượng (6 bảng)**: [Ảnh SVG](02-menu.svg) · [Mã Mermaid](02-menu.mmd)
3. **Nguyên liệu và tồn kho (5 bảng)**: [Ảnh SVG](03-stock.svg) · [Mã Mermaid](03-stock.mmd)
4. **Nhà cung cấp và chứng từ kho (5 bảng)**: [Ảnh SVG](04-warehouse.svg) · [Mã Mermaid](04-warehouse.mmd)
5. **Phòng bàn, đơn hàng và bếp (8 bảng)**: [Ảnh SVG](05-sales.svg) · [Mã Mermaid](05-sales.mmd)
6. **Khách hàng và tích điểm (3 bảng)**: [Ảnh SVG](06-customers.svg) · [Mã Mermaid](06-customers.mmd)
7. **Sổ quỹ và ca thu ngân (2 bảng)**: [Ảnh SVG](07-cash.svg) · [Mã Mermaid](07-cash.mmd)

## Cách nói khi trình bày

> Cơ sở dữ liệu có 37 bảng nghiệp vụ. Em chia thành 7 phân hệ để trình bày; mỗi sơ đồ thể hiện khóa chính, khóa ngoại và quan hệ trong phân hệ. Các quan hệ giữa các phân hệ được dẫn chiếu riêng, còn cấu trúc đầy đủ của từng bảng được trình bày trong phần từ điển dữ liệu.

## Phạm vi và lưu ý

- Chỉ ràng buộc khóa ngoại có thật trong EF Core ModelSnapshot mới được vẽ; không tự tạo đường nối chỉ vì tên cột có Id.
- Một số bảng như AccessAudit, TableOperation lưu nhật ký / tham chiếu dạng văn bản nên đứng riêng. ItemType hiện không có FK từ Food trong bản snapshot; đây là phản ánh cấu trúc hiện tại, không phải mất đường nối khi vẽ.
- Quan hệ một–nhiều / một–một dựa trên ràng buộc và chỉ mục unique. Quan hệ Employee → Account có FK duy nhất và nullable nên có thể không gắn tài khoản.
- PK/FK trong hình là bản rút gọn để đọc; nullable, kiểu SQL đầy đủ và các cột khác có trong index.html/schema.json.
- Không tính bảng kỹ thuật __EFMigrationsHistory hoặc sysdiagrams. Bộ tài liệu không chứa dữ liệu quán hay khóa bí mật.
