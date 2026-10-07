import * as XLSX from 'xlsx'
export function customerWorkbook(rows) {
    const sheet = XLSX.utils.json_to_sheet(rows.map(c => ({ 'Mã khách hàng': c.code, 'Tên khách hàng': c.name, 'Điện thoại': c.phone, Email: c.email, 'Ngày sinh': c.birthday?.slice(0, 10) || '', 'Giới tính': c.gender, 'Địa chỉ': c.address, Nhóm: c.groupName === 'Chưa phân nhóm' ? '' : c.groupName, 'Ghi chú': c.note, 'Trạng thái': c.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động', 'Điểm': c.points, 'Số lần mua': c.visits, 'Tổng bán': c.totalSales, 'Đã hoàn': c.refundAmount, 'Chi tiêu thực thu': c.netSpend, 'Hóa đơn thiếu thực thu': c.estimated })))
    sheet['!cols'] = Array.from({ length: 16 }, (_, i) => ({ wch: i === 1 || i === 6 ? 30 : 22 }))
    const wb = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(wb, sheet, 'Khách hàng');
return wb
}
