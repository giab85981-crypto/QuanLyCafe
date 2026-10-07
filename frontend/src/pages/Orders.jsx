import { can, useAccess } from '../utils/staffAccess'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import * as XLSX from 'xlsx'
import axiosClient from '../api/axiosClient'
const errMsg = (e, fallback) => typeof e.response?.data === 'string' ? e.response.data : e.response?.data?.message || e.message || fallback
import Modal from '../components/Modal'
import './Orders.css'
import KitchenBoard from './KitchenBoard'

const money = n => Number(n || 0).toLocaleString('vi-VN')
const date = d => d ? new Date(d).toLocaleString('vi-VN') : '—'
const statuses = ['Đang phục vụ', 'Đã thanh toán', 'Đã đóng / gộp', 'Đã hủy hoàn tiền']
const method = m => ({ Cash: 'Tiền mặt', Transfer: 'Chuyển khoản', Unknown: 'Chưa ghi nhận' }[m] || '—')
const code = id => `HD${String(id).padStart(6, '0')}`
const today = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}` }
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]))
function printBill(b) {
  const frame = document.createElement('iframe'); frame.style.cssText = 'position:fixed;width:0;height:0;border:0'; document.body.appendChild(frame)
  const receipt = `<html><head><title>${code(b.idBill)}</title><style>body{font:14px Arial;color:#172c38;max-width:520px;margin:30px auto}h1{text-align:center;font-size:23px}h2{text-align:center;font-size:17px}table{width:100%;border-collapse:collapse}td,th{padding:9px 3px;border-bottom:1px solid #ddd;text-align:left}td:last-child,th:last-child{text-align:right}small{display:block;color:#666}p{line-height:1.7}.total{font-size:18px;text-align:right}@media print{body{margin:0} @page{margin:12mm}}</style></head><body><h1>QUÁN CÀ PHÊ</h1><h2>${b.status === 0 ? 'PHIẾU TẠM TÍNH' : 'HÓA ĐƠN BÁN HÀNG'}</h2><p>${code(b.idBill)} · ${esc(statuses[b.status])}<br>${esc(b.orderType === 'Takeaway' ? 'Mang về' : 'Tại quán')} · ${esc(b.tableName)}<br>Khách: ${esc(b.customerName)}<br>Giờ vào: ${esc(date(b.dateCheckIn))}<br>Thanh toán: ${esc(date(b.dateCheckOut))}</p><table><thead><tr><th>Món</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>${b.items.map(i => `<tr><td>${esc(i.foodName)}<small>${esc(i.optionLabel)}</small></td><td>${i.count}</td><td>${money(i.price)}</td><td>${money(i.price * i.count)}</td></tr>`).join('')}</tbody></table><p>Tổng tiền hàng: ${money(b.gross)} đ<br>Giảm giá ${b.discount}%: ${money(b.gross * b.discount / 100)} đ<br>Đổi ${b.pointsRedeemed || 0} điểm: ${money(b.pointDiscount)} đ<br>Điểm tích: ${b.pointsEarned || 0}<br>Phương thức: ${esc(method(b.paymentMethod))}</p><p class="total"><b>Tổng cộng: ${money(b.totalAmount)} đ</b></p>${b.estimated ? `<p>ƯỚC TÍNH: Hóa đơn cũ chưa ghi số tiền thực thu.</p>` : ''}${b.status === 3 ? `<p>ĐÃ HỦY · Đã hoàn: ${money(b.refundAmount)} đ (${esc(method(b.refundMethod))})<br>Lý do: ${esc(b.cancellationReason)}</p>` : ''}<p>${esc(b.note)}</p><p style="text-align:center">Cảm ơn quý khách!</p></body></html>`
  frame.onload = () => { frame.contentWindow.focus(); frame.contentWindow.print(); setTimeout(() => frame.remove(), 60000) }
  frame.srcdoc = receipt
}
export default function Orders() {
  useAccess()
  const navigate = useNavigate()
  const [tab, setTab] = useState('invoices')
  useEffect(() => { if (tab === 'kitchen' && !can('KITCHEN_VIEW')) setTab('invoices') }, [tab, can('KITCHEN_VIEW')])
  const [filters, setFilters] = useState({ search: '', status: '', type: '', payment: '', tableId: '', from: today(), to: today() })
  const [page, setPage] = useState(1)
  const [list, setList] = useState({ rows: [], total: 0, revenue: 0, refunded: 0, serving: 0 })
  const [tables, setTables] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState('')
  const [detail, setDetail] = useState(null)
  const [refund, setRefund] = useState(false)
  const [reason, setReason] = useState('')
  const [refundMethod, setRefundMethod] = useState('Cash')
  const [takeaway, setTakeaway] = useState(null)
  const [label, setLabel] = useState('')
  const [note, setNote] = useState('')
  useEffect(() => { const id = Number(new URLSearchParams(window.location.search).get('invoice')); if (id > 0) axiosClient.get(`/Orders/${id}`).then(r => setDetail(r.data)).catch(e => setError(errMsg(e, 'Không tải được hóa đơn.'))) }, [])
  const sequence = useRef(0)
  const params = useCallback(() => {
    const p = Object.fromEntries(Object.entries(filters).filter(([, v]) => v !== ''))
    if (p.from) p.from += 'T00:00:00'
    if (p.to) { const d = new Date(`${p.to}T00:00:00`); d.setDate(d.getDate() + 1); p.to = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}T00:00:00` }
    return p
  }, [filters])
  const load = useCallback(async () => {
    const ticket = ++sequence.current
    setLoading(true)
    try { const r = await axiosClient.get('/Orders', { params: { ...params(), page } }); if (ticket === sequence.current) { setList(r.data); setError('') } }
    catch (e) { if (ticket === sequence.current) setError(errMsg(e, 'Không tải được hóa đơn.')) }
    finally { if (ticket === sequence.current) setLoading(false) }
  }, [params, page])
  useEffect(() => { const timer = setTimeout(load, 250); return () => { clearTimeout(timer); sequence.current++ } }, [load])
  useEffect(() => { axiosClient.get('/TableFood').then(r => setTables(r.data)).catch(() => {}) }, [])
  const filter = (key, value) => { setPage(1); setFilters(f => ({ ...f, [key]: value })) }
  const run = async fn => { setBusy(true); setError(''); try { await fn() } catch (e) { setError(errMsg(e, 'Thao tác thất bại.')) } finally { setBusy(false) } }
  const open = id => run(async () => { const r = await axiosClient.get(`/Orders/${id}`); setDetail(r.data); setRefund(false); setReason('') })
  const create = () => run(async () => { const r = await axiosClient.post('/Orders/takeaway', { requestKey: takeaway, label, note }); navigate(`/pos?bill=${r.data.idBill}`) })
  const cancelPaid = () => run(async () => { await axiosClient.post(`/Orders/${detail.idBill}/refund`, { reason, paymentMethod: refundMethod }); const r = await axiosClient.get(`/Orders/${detail.idBill}`); setDetail(r.data); setRefund(false); setNotice('Đã ghi chi hoàn tiền và cập nhật doanh thu.'); await load() })
  const exportExcel = () => run(async () => {
    const first = (await axiosClient.get('/Orders', { params: { ...params(), page: 1, pageSize: 100 } })).data
    if (first.total > 10000) throw new Error('Hãy thu hẹp bộ lọc xuống tối đa 10.000 hóa đơn.')
    const rows = [...first.rows]
    for (let n = 2; n <= Math.ceil(first.total / 100); n++) rows.push(...(await axiosClient.get('/Orders', { params: { ...params(), page: n, pageSize: 100 } })).data.rows)
    const sheet = XLSX.utils.json_to_sheet(rows.map(b => ({ 'Mã hóa đơn': code(b.id), 'Giờ vào': date(b.dateCheckIn), 'Giờ thanh toán': date(b.dateCheckOut), 'Loại đơn': b.orderType === 'Takeaway' ? 'Mang về' : 'Tại quán', 'Phòng/Bàn': b.tableName, 'Khách hàng': b.customerName, 'Trạng thái': statuses[b.status], 'Tiền hàng': b.gross, 'Giảm giá (%)': b.discount, 'Tổng sau giảm': b.total, 'Đã thu': [1,3].includes(b.status) && !b.estimated ? b.total : 0, 'Đã hoàn': b.refundAmount, 'Doanh thu còn lại': b.status === 1 ? b.total : 0, 'Phương thức': method(b.paymentMethod), 'Ước tính': b.estimated ? 'Có' : 'Không' })))
    sheet['!cols'] = Array.from({ length: 14 }, () => ({ wch: 23 }))
    const book = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(book, sheet, 'Hoa don'); XLSX.writeFile(book, `hoa-don-${today()}.xlsx`)
  })
  return <main className="orders-page">
    <div className="orders-head"><div><span className="orders-eyebrow">GIAO DỊCH CỦA QUÁN</span><h1>Đơn hàng</h1><p>Theo dõi hóa đơn, chế biến và lịch sử thanh toán.</p></div><div className="orders-tools"><button disabled={busy} onClick={() => { setTakeaway(crypto.randomUUID()); setLabel(''); setNote(''); setError('') }}>+ Đơn mang về</button><button className="primary" onClick={() => navigate('/pos')}>Bán tại bàn</button></div></div>
    <div className="orders-tabs"><button className={tab === 'invoices' ? 'active' : ''} onClick={() => setTab('invoices')}>Hóa đơn</button><button hidden={!can('KITCHEN_VIEW')} className={tab === 'kitchen' ? 'active' : ''} onClick={() => setTab('kitchen')}>Bếp / Bar</button></div>
    {notice && <div className="orders-notice">{notice}<button onClick={() => setNotice('')}>×</button></div>}
    {error && <p className="orders-error" role="alert">{error}</p>}
    {tab === 'kitchen' ? <KitchenBoard embedded /> : <>
      <div className="orders-stats"><article><small>Hóa đơn trong bộ lọc</small><b>{list.total}</b></article><article><small>Doanh thu còn lại</small><b>{money(list.revenue)} đ</b>{list.estimated > 0 && <small>Gồm {list.estimated} hóa đơn cũ ước tính</small>}</article><article><small>Đã hoàn tiền</small><b>{money(list.refunded)} đ</b></article><article><small>Đang phục vụ</small><b>{list.serving}</b></article></div>
      <div className="orders-workspace"><aside className="orders-filters"><h3>Bộ lọc hóa đơn</h3><label>Từ ngày<input type="date" value={filters.from} onChange={e => filter('from', e.target.value)} /></label><label>Đến ngày<input type="date" value={filters.to} onChange={e => filter('to', e.target.value)} /></label><div className="orders-range"><button onClick={() => { setPage(1); setFilters(f => ({ ...f, from: today(), to: today() })) }}>Hôm nay</button><button onClick={() => { setPage(1); setFilters(f => ({ ...f, from: '', to: '' })) }}>Toàn thời gian</button></div><label>Trạng thái<select value={filters.status} onChange={e => filter('status', e.target.value)}><option value="">Tất cả trạng thái</option>{statuses.map((s,i) => <option key={s} value={i}>{s}</option>)}</select></label><label>Loại đơn<select value={filters.type} onChange={e => filter('type', e.target.value)}><option value="">Tất cả</option><option value="DineIn">Tại quán</option><option value="Takeaway">Mang về</option></select></label><label>Phương thức<select value={filters.payment} onChange={e => filter('payment', e.target.value)}><option value="">Tất cả</option><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option><option value="Unknown">Chưa ghi nhận</option></select></label><label>Phòng/Bàn<select value={filters.tableId} onChange={e => filter('tableId', e.target.value)}><option value="">Tất cả bàn</option>{tables.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label></aside>
      <section className="orders-list"><div className="orders-listbar"><input placeholder="Tìm mã HD, bàn, khách hoặc ghi chú..." value={filters.search} onChange={e => filter('search', e.target.value)} /><button disabled={loading || busy} onClick={load}>Làm mới</button><button disabled={busy || !list.total} onClick={exportExcel}>Xuất Excel</button></div><div className="orders-table-scroll"><table><thead><tr><th>Mã hóa đơn</th><th>Thời gian</th><th>Phòng/Bàn · Khách</th><th>Trạng thái</th><th>Tiền hàng</th><th>Sau giảm giá</th><th>Đã trả / Hoàn</th></tr></thead><tbody>{list.rows.map(b => <tr key={b.id} onClick={() => open(b.id)}><td><button disabled={busy} className="orders-code" onClick={e => { e.stopPropagation(); open(b.id) }}>{code(b.id)}</button><small>{b.orderType === 'Takeaway' ? 'Mang về' : 'Tại quán'}</small></td><td>{date(b.dateCheckOut ?? b.dateCheckIn)}<small>{b.dateCheckOut ? 'Giờ đóng / thanh toán' : 'Giờ vào'}</small></td><td>{b.tableName}<small>{b.customerName}</small></td><td><span className={`orders-status status-${b.status}`}>{statuses[b.status]}</span></td><td>{money(b.gross)}</td><td><b>{money(b.total)}</b><small>{b.estimated ? "Ước tính · chưa lưu thực thu" : `Giảm ${b.discount}%`}</small></td><td>{money([1,3].includes(b.status) && !b.estimated ? b.total : 0)}<small>{b.status === 3 ? `Hoàn ${money(b.refundAmount)} đ` : method(b.paymentMethod)}</small></td></tr>)}</tbody></table></div>{loading && <p className="orders-empty">Đang tải hóa đơn...</p>}{!loading && !list.rows.length && <p className="orders-empty">Không có hóa đơn phù hợp. Thử chọn Toàn thời gian hoặc đổi bộ lọc.</p>}<footer className="orders-pagination"><span>{list.total} hóa đơn · Trang {page}/{Math.max(1, Math.ceil(list.total / 15))}</span><button disabled={page === 1 || loading} onClick={() => setPage(p => p - 1)}>Trước</button><button disabled={page * 15 >= list.total || loading} onClick={() => setPage(p => p + 1)}>Sau</button></footer></section></div>
    </>}
    {takeaway && <Modal title="Tạo đơn mang về" onClose={() => { if (!busy) setTakeaway(null) }} footer={<><button disabled={busy} onClick={() => setTakeaway(null)}>Đóng</button><button hidden={!can('POS_ORDER')} className="primary" disabled={busy} onClick={create}>Tạo và chọn món</button></>}><div className="orders-form"><label>Tên khách / nhãn đơn<input placeholder="VD: Anh Nam · lấy lúc 10:30" maxLength={100} value={label} onChange={e => setLabel(e.target.value)} /></label><label>Ghi chú<textarea maxLength={500} value={note} onChange={e => setNote(e.target.value)} /></label><p>Đơn riêng không chiếm bàn. Chọn món, báo bếp và thanh toán như đơn tại quán.</p>{error && <p className="orders-error">{error}</p>}</div></Modal>}
    {detail && <Modal width={850} title={`${code(detail.idBill)} · ${statuses[detail.status]}`} onClose={() => { if (!busy) { setDetail(null); setRefund(false) } }} footer={<><button disabled={busy} onClick={() => printBill(detail)}>In {detail.status === 0 ? 'tạm tính' : 'hóa đơn'}</button>{detail.status === 0 && <button hidden={!can('POS_VIEW')} className="primary" onClick={() => navigate(detail.orderType === 'Takeaway' ? `/pos?bill=${detail.idBill}` : `/pos?table=${detail.idTable}`)}>Tiếp tục phục vụ</button>}{can('ORDERS_REFUND') && detail.status === 1 && !detail.estimated && <button hidden={!can('ORDERS_REFUND')} className="danger" disabled={busy} onClick={() => { setRefund(true); setRefundMethod(['Cash','Transfer'].includes(detail.paymentMethod) ? detail.paymentMethod : 'Cash') }}>Hủy và hoàn tiền</button>}<button disabled={busy} onClick={() => setDetail(null)}>Đóng</button></>}>
      {detail.estimated && <p className="orders-error">Hóa đơn cũ chưa ghi số tiền thực thu. Tổng dưới đây là ước tính; chưa thể tự động hoàn tiền.</p>}
      <div className="invoice-meta"><div><small>Hình thức · Phòng/Bàn</small><b>{detail.orderType === 'Takeaway' ? 'Mang về' : 'Tại quán'} · {detail.tableName}</b></div><div><small>Khách hàng</small><b>{detail.customerName}</b></div><div><small>Giờ vào</small><b>{date(detail.dateCheckIn)}</b></div><div><small>Giờ thanh toán</small><b>{date(detail.dateCheckOut)}</b></div><div><small>Thu ngân</small><b>{detail.paidBy || detail.createdBy || 'Chưa ghi nhận'}</b></div><div><small>Phương thức</small><b>{method(detail.paymentMethod)}</b></div></div>
      <table className="invoice-lines"><thead><tr><th>Món / tùy chọn</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>{detail.items.map(i => <tr key={i.idBillInfo}><td>{i.foodName}<small>{i.optionLabel}</small><small>Đã báo bếp {i.sentCount}/{i.count}</small></td><td>{i.count}</td><td>{money(i.price)}</td><td>{money(i.count * i.price)}</td></tr>)}</tbody></table>
      <div className="invoice-totals"><p>Tiền hàng <b>{money(detail.gross)} đ</b></p><p>Giảm giá {detail.discount}% <b>{money(detail.gross * detail.discount / 100)} đ</b></p><p>Đổi {detail.pointsRedeemed || 0} điểm <b>{money(detail.pointDiscount)} đ</b></p><p>Điểm tích <b>+{detail.pointsEarned || 0}</b></p><p>Tổng cộng <b>{money(detail.totalAmount)} đ</b></p></div>{detail.note && <p>Ghi chú: {detail.note}</p>}
      {detail.status === 3 && <div className="invoice-refund"><b>Đã hoàn {money(detail.refundAmount)} đ · {method(detail.refundMethod)}</b><p>{detail.cancellationReason}</p><small>{detail.cancelledBy} · {date(detail.cancelledAt)}</small></div>}
      {!!detail.payments.length && <details className="invoice-history"><summary>Lịch sử thu / hoàn tiền ({detail.payments.length})</summary>{detail.payments.map(p => <p key={p.id}>{p.direction === 'In' ? 'Thu' : 'Hoàn'} {money(p.amount)} đ · {method(p.paymentMethod)} · {date(p.createdAt)} · {p.createdBy}</p>)}</details>}
      {!!detail.transfers.length && <details className="invoice-history"><summary>Lịch sử chuyển / gộp / tách ({detail.transfers.length})</summary>{detail.transfers.map(t => <p key={t.id}>{t.kind === 'Split' ? 'Tách' : t.kind === 'Merge' ? 'Gộp' : 'Chuyển'} {t.sourceName} → {t.targetName} · {date(t.createdAt)}</p>)}</details>}
      {refund && <div className="orders-form invoice-refund"><h3>Hủy toàn bộ và hoàn tiền</h3><p>Hoàn {money(detail.totalAmount)} đ, cập nhật Sổ quỹ và doanh thu. Giữ nguyên món/giá; nguyên liệu đã dùng không hoàn kho.</p><label>Lý do hủy<textarea maxLength={500} placeholder="Nhập lý do..." value={reason} onChange={e => setReason(e.target.value)} /></label><label>Hoàn bằng<select value={refundMethod} onChange={e => setRefundMethod(e.target.value)}><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select></label><button hidden={!can('ORDERS_REFUND')} className="danger" disabled={busy || !reason.trim()} onClick={cancelPaid}>Xác nhận hủy và ghi hoàn tiền</button></div>}{error && <p className="orders-error">{error}</p>}
    </Modal>}
  </main>
}
export { default as KitchenBoard } from './KitchenBoard'
