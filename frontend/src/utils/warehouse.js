import * as XLSX from 'xlsx'
export const number = value => Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 4 })
export const money = value => `${number(value)} ₫`
export const date = value => value ? new Date(value).toLocaleString('vi-VN') : '—'
export const day = (value = new Date()) => { const d = new Date(value); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}` }
export const errorText = error => typeof error.response?.data === 'string' ? error.response.data : error.response?.data?.title || 'Không thể lưu dữ liệu. Kiểm tra backend và thử lại.'
export const exportSheet = (rows, name) => { const book = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(book, XLSX.utils.json_to_sheet(rows), 'Dữ liệu'); XLSX.writeFile(book, `${name}.xlsx`) }
