import * as XLSX from 'xlsx'
const labels = { Admin: 'Quản trị viên', Cashier: 'Thu ngân', Kitchen: 'Bếp / Pha chế' }
const date = value => value ? value.slice(0,10) : ''
export function staffWorkbook(items) {
  const rows = items.map(e => ({ 'Mã nhân viên': e.code, 'Họ tên': e.name, 'Điện thoại': e.phone, 'Email': e.email, 'Giới tính': e.gender, 'Ngày sinh': date(e.birthday), 'Ngày vào làm': date(e.hireDate), 'Bộ phận': e.department, 'Chức danh': e.position, 'Địa chỉ': e.address, 'Trạng thái': e.isActive ? 'Đang làm' : 'Nghỉ việc', 'Tài khoản': e.userName || '', 'Vai trò': labels[e.roleName] || e.roleName || '', 'Ghi chú': e.note }))
  const book = XLSX.utils.book_new(), sheet = XLSX.utils.json_to_sheet(rows)
  sheet['!cols'] = Array.from({length:14}, () => ({wch:24}))
  XLSX.utils.book_append_sheet(book, sheet, 'Nhan vien')
  return book
}
