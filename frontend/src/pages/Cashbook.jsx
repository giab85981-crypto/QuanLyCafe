import { useEffect, useMemo, useState } from 'react'
import axiosClient from '../api/axiosClient'
import { errMsg } from '../api/errMsg'
import DateRange, { presets } from './DateRange'
import './Page.css'

const money = (n) => Number(n || 0).toLocaleString('vi-VN')
const day = (iso) => new Date(iso).toLocaleDateString('vi-VN')

function Cashbook() {
  const [[from, to], setRange] = useState(presets.month())
  const [rows, setRows] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    axiosClient
      .get('/Report/revenue', { params: { fromDate: `${from}T00:00:00`, toDate: `${to}T23:59:59` } })
      .then((r) => {
        setRows([...r.data].sort((a, b) => new Date(a.date) - new Date(b.date)))
        setError('')
      })
      .catch((e) => setError(errMsg(e, 'Không tải được sổ quỹ.')))
      .finally(() => setLoading(false))
  }, [from, to])

  const sum = useMemo(
    () =>
      rows.reduce(
        (s, r) => ({
          revenue: s.revenue + r.totalRevenue,
          cost: s.cost + r.totalCost,
          profit: s.profit + r.totalProfit,
          bills: s.bills + r.totalBills,
        }),
        { revenue: 0, cost: 0, profit: 0, bills: 0 },
      ),
    [rows],
  )

  // Xuất CSV (thêm BOM để Excel đọc đúng tiếng Việt)
  const exportCsv = () => {
    const lines = [['Ngày', 'Số hóa đơn', 'Tiền thu', 'Giá vốn', 'Chênh lệch']]
    rows.forEach((r) => lines.push([day(r.date), r.totalBills, r.totalRevenue, r.totalCost, r.totalProfit]))
    lines.push(['Tổng', sum.bills, sum.revenue, sum.cost, sum.profit])
    const csv = '\uFEFF' + lines.map((l) => l.join(',')).join('\n')
    const a = document.createElement('a')
    a.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }))
    a.download = `so-quy_${from}_${to}.csv`
    a.click()
    URL.revokeObjectURL(a.href)
  }

  return (
    <div className="pg">
      <div className="pg-head">
        <h1>Sổ quỹ</h1>
        <div className="pg-tools">
          <DateRange from={from} to={to} onChange={(f, t) => setRange([f, t])} />
          <button className="pg-btn" onClick={exportCsv} disabled={rows.length === 0}>Xuất CSV</button>
        </div>
      </div>

      {error && <div className="pg-error">{error}</div>}

      <div className="pg-stats">
        <div className="pg-card pg-stat pg-stat--green">
          <div className="pg-stat__label">Tổng thu</div>
          <div className="pg-stat__value">{money(sum.revenue)}</div>
        </div>
        <div className="pg-card pg-stat pg-stat--amber">
          <div className="pg-stat__label">Giá vốn</div>
          <div className="pg-stat__value">{money(sum.cost)}</div>
        </div>
        <div className="pg-card pg-stat">
          <div className="pg-stat__label">Chênh lệch (thu − vốn)</div>
          <div className="pg-stat__value">{money(sum.profit)}</div>
        </div>
      </div>

      <div className="pg-card pg-table-wrap">
        <table className="pg-table">
          <thead>
            <tr>
              <th>Ngày</th>
              <th className="num">Số hóa đơn</th>
              <th className="num">Tiền thu</th>
              <th className="num">Giá vốn</th>
              <th className="num">Chênh lệch</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan="5" className="pg-empty">Đang tải...</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan="5" className="pg-empty">Không có giao dịch nào trong khoảng ngày này.</td></tr>
            )}
            {rows.map((r) => (
              <tr key={r.date}>
                <td>{day(r.date)}</td>
                <td className="num">{r.totalBills}</td>
                <td className="num">{money(r.totalRevenue)}</td>
                <td className="num">{money(r.totalCost)}</td>
                <td className="num">{money(r.totalProfit)}</td>
              </tr>
            ))}
          </tbody>
          {rows.length > 0 && (
            <tfoot>
              <tr>
                <td>Tổng</td>
                <td className="num">{sum.bills}</td>
                <td className="num">{money(sum.revenue)}</td>
                <td className="num">{money(sum.cost)}</td>
                <td className="num">{money(sum.profit)}</td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>

      <p className="pg-note">
        Số liệu tính từ các hóa đơn đã thanh toán. Backend chưa có API phiếu thu/phiếu chi riêng nên chưa ghi được khoản thu chi ngoài bán hàng.
      </p>
    </div>
  )
}

export default Cashbook
