import { useEffect, useMemo, useState } from 'react'
import axiosClient from '../api/axiosClient'
import { errMsg } from '../api/errMsg'
import DateRange, { presets } from './DateRange'
import './Page.css'

const money = (n) => Number(n || 0).toLocaleString('vi-VN')

function Reports() {
  const [[from, to], setRange] = useState(presets.week())
  const [top, setTop] = useState(10)
  const [revenue, setRevenue] = useState([])
  const [topFoods, setTopFoods] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    const params = { fromDate: `${from}T00:00:00`, toDate: `${to}T23:59:59` }
    Promise.all([
      axiosClient.get('/Report/revenue', { params }),
      axiosClient.get('/Report/top-selling', { params: { ...params, top } }),
    ])
      .then(([rev, tp]) => {
        setRevenue([...rev.data].sort((a, b) => new Date(a.date) - new Date(b.date)))
        setTopFoods(tp.data)
        setError('')
      })
      .catch((e) => setError(errMsg(e, 'Không tải được báo cáo.')))
      .finally(() => setLoading(false))
  }, [from, to, top])

  const sum = useMemo(
    () =>
      revenue.reduce(
        (s, r) => ({
          revenue: s.revenue + r.totalRevenue,
          profit: s.profit + r.totalProfit,
          bills: s.bills + r.totalBills,
        }),
        { revenue: 0, profit: 0, bills: 0 },
      ),
    [revenue],
  )

  const maxRevenue = Math.max(1, ...revenue.map((r) => r.totalRevenue))
  const maxQty = Math.max(1, ...topFoods.map((f) => f.totalQuantity))

  return (
    <div className="pg">
      <div className="pg-head">
        <h1>Báo cáo</h1>
        <DateRange from={from} to={to} onChange={(f, t) => setRange([f, t])} />
      </div>

      {error && <div className="pg-error">{error}</div>}

      <div className="pg-stats">
        <div className="pg-card pg-stat pg-stat--green">
          <div className="pg-stat__label">Doanh thu</div>
          <div className="pg-stat__value">{money(sum.revenue)}</div>
        </div>
        <div className="pg-card pg-stat">
          <div className="pg-stat__label">Lợi nhuận</div>
          <div className="pg-stat__value">{money(sum.profit)}</div>
        </div>
        <div className="pg-card pg-stat pg-stat--amber">
          <div className="pg-stat__label">Số hóa đơn</div>
          <div className="pg-stat__value">{sum.bills}</div>
        </div>
        <div className="pg-card pg-stat">
          <div className="pg-stat__label">Trung bình mỗi hóa đơn</div>
          <div className="pg-stat__value">{money(sum.bills ? sum.revenue / sum.bills : 0)}</div>
        </div>
      </div>

      <div className="pg-two-col">
        <div className="pg-card">
          <h3 className="pg-section-title">Doanh thu theo ngày</h3>
          {loading && <div className="pg-empty">Đang tải...</div>}
          {!loading && revenue.length === 0 && !error && <div className="pg-empty">Không có dữ liệu trong khoảng ngày này.</div>}
          {revenue.length > 0 && (
            <div className="chart-bars">
              {revenue.map((r) => {
                const d = new Date(r.date)
                return (
                  <div key={r.date} className="chart-bars__col">
                    <div
                      className="chart-bars__bar"
                      style={{ height: `${(r.totalRevenue / maxRevenue) * 100}%` }}
                      title={`${d.toLocaleDateString('vi-VN')}: ${money(r.totalRevenue)} (${r.totalBills} hóa đơn)`}
                    />
                    <span className="chart-bars__label">{`${d.getDate()}/${d.getMonth() + 1}`}</span>
                  </div>
                )
              })}
            </div>
          )}
        </div>

        <div className="pg-card">
          <div className="pg-head" style={{ padding: '14px 16px 0', marginBottom: 0 }}>
            <h3 style={{ fontSize: 15 }}>Món bán chạy</h3>
            <select className="pg-select" value={top} onChange={(e) => setTop(Number(e.target.value))}>
              <option value={5}>Top 5</option>
              <option value={10}>Top 10</option>
              <option value={20}>Top 20</option>
            </select>
          </div>
          <div className="pg-table-wrap">
            <table className="pg-table">
              <thead>
                <tr>
                  <th>Món</th>
                  <th className="num">SL</th>
                  <th className="num">Doanh thu</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {!loading && topFoods.length === 0 && !error && (
                  <tr><td colSpan="4" className="pg-empty">Chưa có món nào được bán.</td></tr>
                )}
                {topFoods.map((f) => (
                  <tr key={f.foodName}>
                    <td>{f.foodName}</td>
                    <td className="num">{f.totalQuantity}</td>
                    <td className="num">{money(f.totalAmount)}</td>
                    <td><span className="hbar"><span style={{ width: `${(f.totalQuantity / maxQty) * 100}%` }} /></span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}

export default Reports
