import { errMsg as message } from '../api/errMsg'
import { can, useAccess } from '../utils/staffAccess'
import { useEffect, useState } from 'react'
import axios from '../api/axiosClient'
import Modal from './Modal'
import './CustomerPicker.css'
export default function CustomerPicker({ customer, disabled, onSelect }) {
  useAccess()
  const [show, setShow] = useState(false), [search, setSearch] = useState(''), [rows, setRows] = useState([]), [loading, setLoading] = useState(false), [error, setError] = useState(''), [busy, setBusy] = useState(false)
  const [adding, setAdding] = useState(false), [name, setName] = useState(''), [phone, setPhone] = useState('')
  useEffect(() => { if (disabled) setShow(false); if (!can('CUSTOMERS_CREATE')) setAdding(false) }, [disabled, can('CUSTOMERS_CREATE')])
  useEffect(() => {
    if (!show) return
    const controller = new AbortController()
    const timer = setTimeout(async () => { setLoading(true); try { const r = await axios.get('/Customer', { params: { search }, signal: controller.signal }); if (!controller.signal.aborted) { setRows(r.data); setError('') } } catch (e) { if (!controller.signal.aborted) setError(message(e)) } finally { if (!controller.signal.aborted) setLoading(false) } }, 200)
    return () => { clearTimeout(timer); controller.abort() }
  }, [show, search])
  const choose = async c => { if (disabled || busy) return; setBusy(true); setError(''); try { await onSelect(c); setShow(false) } catch (e) { setError(message(e)) } finally { setBusy(false) } }
  const create = async () => {
    if (disabled || busy || !can('CUSTOMERS_CREATE')) return
    setBusy(true); setError('')
    try { const r = await axios.post('/Customer', { name, phone }); setAdding(false); setSearch(r.data.phone); setRows([r.data]); await onSelect(r.data); setShow(false) }
    catch (e) { setError(message(e)) } finally { setBusy(false) }
  }
  return <div className="pos-customer"><button disabled={disabled} className="pos-customer-trigger" onClick={() => { setShow(true); setAdding(false); setSearch(''); setError('') }}><span>{customer ? `${customer.name} · ${customer.phone}` : 'Khách lẻ · Chọn khách hàng'}</span><small>{customer ? `${customer.points.toLocaleString('vi-VN')} điểm · Đổi khách` : 'Tích điểm cho khách quen'}</small></button>{show && <Modal width={540} title="Chọn khách hàng" onClose={() => { if (!busy) setShow(false) }} footer={<><button disabled={busy} onClick={() => choose(null)}>Khách lẻ</button><button hidden={!can('CUSTOMERS_CREATE')} disabled={busy} onClick={() => setAdding(a => !a)}>{adding ? 'Quay lại tìm khách' : '+ Thêm nhanh khách'}</button></>}>
    {!adding ? <><input className="pos-customer-search" aria-label="Tìm khách tại bán hàng" autoFocus placeholder="Tên, điện thoại hoặc mã khách..." value={search} onChange={e => setSearch(e.target.value)} /><div className="pos-customer-results">{rows.map(c => <button key={c.id} disabled={busy} onClick={() => choose(c)}><span><b>{c.name}</b><small>{c.phone} · {c.code}</small></span><strong>{c.points} điểm</strong></button>)}</div>{loading && <p>Đang tìm khách...</p>}{!loading && !rows.length && <p>Không có khách phù hợp.{can('CUSTOMERS_CREATE') && ' Bạn có thể thêm nhanh.'}</p>}</> : <div className="pos-customer-form"><label>Tên khách hàng<input maxLength={150} autoFocus value={name} onChange={e => setName(e.target.value)} /></label><label>Điện thoại<input type="tel" maxLength={30} value={phone} onChange={e => setPhone(e.target.value)} /></label><button disabled={busy || !name.trim() || !phone.trim()} onClick={create}>Lưu và chọn khách</button><p>Có thể bổ sung thông tin tại trang Khách hàng.</p></div>}{error && <p role="alert" className="pos-customer-error">{error}</p>}</Modal>}</div>
}
