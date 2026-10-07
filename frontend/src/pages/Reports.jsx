import PageState from '../components/PageState'
import { useEffect, useState } from 'react'
import api from '../api/axiosClient'
import DateRange, { presets } from '../components/DateRange'
import { money, number, date, exportSheet, errorText } from '../utils/warehouse'
import './Page.css'
import './Reports.css'
const methods = { Cash: 'Tiền mặt', Transfer: 'Chuyển khoản', Unknown: 'Chưa rõ phương thức' }
const tabs = [['sales', 'Doanh thu & món bán'], ['cash', 'Dòng tiền'], ['stock', 'Tồn kho hiện tại'], ['debt', 'Công nợ hiện tại']]
function Grid({ headers, rows }) { return <div className="pg-table-wrap"><table className="pg-table"><thead><tr>{headers.map(h => <th key={h}>{h}</th>)}</tr></thead><tbody>{rows.length ? rows.map((row, index) => <tr key={index}>{row.map((cell, i) => <td key={i}>{cell}</td>)}</tr>) : <tr><td colSpan={headers.length} className="pg-empty">Chưa có dữ liệu.</td></tr>}</tbody></table></div> }
function Stat({ label, value, note }) { return <div className="report-stat"><span>{label}</span><strong>{value}</strong>{note && <small>{note}</small>}</div> }
export default function Reports() {
  const [[from, to], setRange] = useState(presets.week())
  const [tab, setTab] = useState('sales'), [data, setData] = useState(null), [loading, setLoading] = useState(true), [error, setError] = useState(''), [reload, setReload] = useState(0), [search, setSearch] = useState('')
  useEffect(() => {
    let active = true
    setLoading(true); setError(''); setData(null)
    api.get('/Report/summary', { params: { fromDate: from, toDate: to } }).then(r => { if (active) setData(r.data) }).catch(e => { if (active) setError(errorText(e)) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [from, to, reload])
  const s = data?.sales
  const match = name => name.toLocaleLowerCase('vi').includes(search.trim().toLocaleLowerCase('vi'))
  const foods = (s?.foods || []).filter(f => match(f.foodName)), stock = (data?.stock || []).filter(i => match(`${i.code} ${i.name}`)), suppliers = (data?.suppliers || []).filter(i => match(i.supplierName))
  const headers = tab === 'sales' ? ['Ngày', 'Hóa đơn', 'Bán hàng', 'Hoàn tiền', 'Doanh thu thuần', 'Giá vốn', 'Lãi gộp'] : tab === 'cash' ? ['Phương thức', 'Lũy kế trước kỳ', 'Thu trong kỳ', 'Chi trong kỳ', 'Lũy kế cuối kỳ'] : tab === 'stock' ? ['Mã / nguyên liệu', 'Đơn vị', 'Tồn thực tế', 'Còn hạn', 'Hết hạn', 'Giá trị tồn', 'Cảnh báo'] : ['Nhà cung cấp', 'Tổng nhập', 'Đã trả', 'Còn nợ']
  const raw = !data ? [] : tab === 'sales' ? s.daily.map(d => [new Date(d.date).toLocaleDateString('vi-VN'), d.bills, d.sales, d.refunds, d.netRevenue, d.cost, d.grossProfit]) : tab === 'cash' ? data.cash.map(c => [methods[c.paymentMethod] || c.paymentMethod, c.opening, c.totalIn, c.totalOut, c.closing]) : tab === 'stock' ? stock.map(i => [`${i.code || '—'} · ${i.name}`, i.unit, i.quantity, i.usable, i.expired, i.value, i.expired > 0 ? 'Có lô hết hạn' : i.usable <= i.minQuantity ? 'Cần nhập thêm' : 'Đủ tồn']) : suppliers.map(i => [i.supplierName, i.total, i.paid, i.debt])
  const rows = raw.map(r => r.map((v, i) => typeof v !== 'number' ? v : tab === 'sales' ? i === 1 ? number(v) : money(v) : tab === 'stock' ? i === 5 ? money(v) : number(v) : money(v)))
  const exportCurrent = () => exportSheet(raw.map(r => Object.fromEntries(headers.map((h, i) => [h, r[i]]))), `Bao-cao-${tab}-${tab === 'stock' || tab === 'debt' ? 'hien-tai' : from + '-' + to}`)
  return <main className="pg financial-report">
    <div className="report-heading"><div><span className="report-eyebrow">TỔNG QUAN KINH DOANH</span><h1>Báo cáo</h1><p>Theo dõi bán hàng, dòng tiền và nguồn hàng của quán.</p></div><div className="pg-tools"><button className="pg-btn" disabled={loading} onClick={() => setReload(r => r + 1)}>Làm mới</button><button className="pg-btn pg-btn--primary" disabled={!data || loading} onClick={exportCurrent}>Xuất Excel</button></div></div>
    <div className="report-navigation">{tabs.map(([key, label]) => <button key={key} className={tab === key ? 'selected' : ''} onClick={() => { setTab(key); setSearch('') }}>{label}</button>)}</div>
    <div className="report-controls">{tab === 'sales' || tab === 'cash' ? <DateRange from={from} to={to} onChange={(f, t) => setRange([f, t])} /> : <span>Số liệu hiện tại • {data && date(data.snapshotAt)} • Không phải số liệu cuối kỳ</span>}{tab !== 'cash' && <input className="pg-input" placeholder={tab === 'sales' ? 'Tìm món...' : tab === 'stock' ? 'Tìm nguyên liệu...' : 'Tìm nhà cung cấp...'} value={search} onChange={e => setSearch(e.target.value)} />}</div>
    {error && <PageState error={error} onRetry={() => setReload(r => r + 1)} />}{loading && <PageState loading title="Đang tải báo cáo…" />}
    {data && !loading && <>
      {tab === 'sales' && <><div className="report-stats"><Stat label="Bán hàng sau giảm giá / đổi điểm" value={money(s.sales)} note={`${s.totalBills} hóa đơn thanh toán trong kỳ`} /><Stat label="Hoàn tiền trong kỳ" value={money(s.refunds)} /><Stat label="Doanh thu thuần" value={money(s.netRevenue)} /><Stat label="Lãi gộp" value={money(s.grossProfit)} note={`Giá vốn món đã bán: ${money(s.cost)}`} /></div><p className="report-note">Doanh thu ghi vào ngày thanh toán; hoàn tiền ghi vào ngày hoàn. Giá vốn giữ nguyên vì hoàn tiền không hoàn nguyên liệu đã dùng. Lãi gộp chưa trừ thuê mặt bằng, lương và chi phí vận hành.{s.estimatedBills > 0 && ` Có ${s.estimatedBills} hóa đơn cũ có doanh thu ước tính.`}</p></>}
      {tab === 'cash' && <><div className="report-stats"><Stat label="Thu đã ghi sổ" value={money(data.cash.reduce((a, c) => a + c.totalIn, 0))} /><Stat label="Chi đã ghi sổ" value={money(data.cash.reduce((a, c) => a + c.totalOut, 0))} /><Stat label="Chênh lệch trong kỳ" value={money(data.cash.reduce((a, c) => a + c.totalIn - c.totalOut, 0))} /></div><p className="report-note">Chỉ tính phiếu đã ghi Sổ quỹ: bán hàng, hoàn tiền, trả nhà cung cấp và thu chi thủ công. Lũy kế tính từ giao dịch đầu tiên đã ghi nhận, chưa phải số dư thực tế nếu chưa ghi tiền ban đầu. Tiền nhập hàng là dòng chi, không trừ thêm vào lãi gộp.</p></>}
      {tab === 'stock' && <><div className="report-stats"><Stat label="Giá trị tồn theo giá vốn lô" value={money(data.stock.reduce((a, i) => a + i.value, 0))} /><Stat label="Nguyên liệu chạm mức tối thiểu" value={data.stock.filter(i => i.usable <= i.minQuantity).length} /><Stat label="Nguyên liệu còn lô hết hạn" value={data.stock.filter(i => i.expired > 0).length} /></div><p className="report-note">Tồn còn hạn chưa trừ nguyên liệu đang giữ cho đơn chưa báo bếp. Giá trị tồn gồm cả lô hết hạn chưa xuất hủy.</p></>}
      {tab === 'debt' && <><div className="report-stats"><Stat label="Tổng nhập có theo dõi thanh toán" value={money(data.suppliers.reduce((a, i) => a + i.total, 0))} /><Stat label="Đã trả nhà cung cấp" value={money(data.suppliers.reduce((a, i) => a + i.paid, 0))} /><Stat label="Còn nợ hiện tại" value={money(data.suppliers.reduce((a, i) => a + i.debt, 0))} /></div><p className="report-note">Chỉ tính phiếu nhập có theo dõi thanh toán.{data.untrackedImports > 0 && ` Có ${data.untrackedImports} phiếu nhập cũ chưa đủ dữ liệu thanh toán, không tự coi là còn nợ.`}</p></>}
      <section className="pg-card"><h2>{tabs.find(t => t[0] === tab)[1]}</h2><Grid headers={headers} rows={rows} /></section>
      {tab === 'sales' && <section className="pg-card"><div className="report-section-head"><h2>Món bán theo doanh thu</h2><button className="pg-btn" onClick={() => exportSheet(foods.map(f => ({ 'Món': f.foodName, 'Số lượng': f.totalQuantity, 'Doanh thu phân bổ': f.totalAmount })), `Mon-ban-${from}-${to}`)}>Xuất danh sách món</button></div><p className="report-note">Phân bổ tiền hóa đơn sau giảm giá và đổi điểm theo giá trị từng món. Gồm món của hóa đơn đã hoàn tiền vì món đã phục vụ; khoản hoàn trình bày riêng phía trên.</p><Grid headers={['Món', 'Số lượng đã bán', 'Doanh thu phân bổ trước hoàn']} rows={foods.map(f => [f.foodName, number(f.totalQuantity), money(f.totalAmount)])} /></section>}
    </>}
  </main>
}
