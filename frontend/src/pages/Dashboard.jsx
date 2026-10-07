import { useEffect, useState } from 'react'
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts'
import { Utensils, Sandwich, CupSoda, RefreshCw } from 'lucide-react'
import axiosClient from '../api/axiosClient'
import StatCard from '../components/StatCard'
import ChartCard, { PeriodSelect, Tabs, Empty } from '../components/ChartCard'
import { money } from '../utils/format'
import './Dashboard.css'

const colors = ['#0070e0', '#35b5ac', '#ffb44b', '#8d7bea', '#f27792', '#69b958']
function Dashboard() {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)
  const [filters, setFilters] = useState({ revenueDays: 7, customerDays: 7, menuDays: 7, revenueGroup: 'hour', customerGroup: 'hour', menuGroup: 'category', menuFilter: '', sortBy: 'quantity' })
  const update = (key, value) => {
    setLoading(true)
    setError('')
    setFilters((old) => ({ ...old, [key]: value, ...(key === 'menuGroup' || key === 'menuDays' ? { menuFilter: '' } : {}) }))
  }
  const refresh = () => { setLoading(true); setError(''); setReload((n) => n + 1) }
  useEffect(() => {
    const controller = new AbortController()
    axiosClient.get('/Dashboard/overview', { params: filters, signal: controller.signal })
      .then((r) => { if (!controller.signal.aborted) setData(r.data) })
      .catch((e) => {
        if (controller.signal.aborted) return
        setError(e.response?.status === 401 ? 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.' : 'Không tải được Dashboard. Kiểm tra kết nối API và cập nhật cơ sở dữ liệu rồi thử lại.')
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [filters, reload])
  const summary = data?.summary
  const menu = data?.menu
  return <main className="dashboard">
    <div className="dashboard__title-row"><h1>Bức tranh kinh doanh</h1><div className="dashboard__actions">
      <span className="dashboard__branch-select">Chi nhánh trung tâm</span>
      <button className="dashboard__refresh" disabled={loading} onClick={refresh}><RefreshCw size={16} /> Làm mới</button>
    </div></div>
    {error && <div className="dashboard-error" role="alert">{error}<button onClick={refresh}>Thử lại</button></div>}
    {loading && <p role="status" className="dashboard-loading">Đang tải dữ liệu kinh doanh…</p>}
    {data && !error && <div aria-busy={loading} className={loading ? 'dashboard-content is-loading' : 'dashboard-content'}>
      <div className="dashboard__stats-row">
        <StatCard tone="blue" title="Doanh thu hôm nay" value={money(summary.todayRevenue)} subtitle='Sau giảm giá, đổi điểm và hoàn tiền trong ngày' rows={[
          { label: 'Giảm giá hóa đơn', value: money(summary.todayDiscount) }, { label: 'Hoàn tiền hôm nay', value: money(summary.todayRefund) }
        ]} />
        <StatCard tone="green" title="Số lượng đơn hôm nay" value={summary.todayOrderCount} subtitle={summary.todayOrderCount ? 'Số hóa đơn đã thanh toán' : 'Không phát sinh đơn'} rows={[
          { label: 'Trung bình đơn', value: money(summary.averageOrderValue) }, { label: 'Số khách/đơn', value: summary.averageGuestsPerOrder == null ? 'Chưa ghi nhận' : summary.averageGuestsPerOrder.toLocaleString('vi-VN') }
        ]} />
        <StatCard tone="amber" title="Tỷ lệ phủ bàn" value={`${summary.occupancyRate.toLocaleString('vi-VN')}%`} subtitle={`${summary.occupiedTables}/${summary.totalTables} bàn đang sử dụng`} rows={[
          { label: `Đơn đang phục vụ (${summary.servingOrders})`, value: summary.servingOrders }, { label: 'Khách đang phục vụ', value: `${summary.servingGuests}${summary.unknownServingGuestOrders ? ' + chưa ghi nhận' : ''}` }
        ]} />
      </div>
      <div className="dashboard__charts-row">
        <ChartCard title="Doanh thu thuần" data={data.revenue} days={filters.revenueDays} onDaysChange={(v) => update('revenueDays', v)} group={filters.revenueGroup} onGroupChange={(v) => update('revenueGroup', v)} />
        <ChartCard title="Lượng khách hàng" customers data={data.customers} days={filters.customerDays} onDaysChange={(v) => update('customerDays', v)} group={filters.customerGroup} onGroupChange={(v) => update('customerGroup', v)} />
      </div>
      <section className="chart-card dashboard-menu" aria-label="Hiệu quả thực đơn">
        <div className="chart-card__head"><h3>Hiệu quả thực đơn</h3><div className="dashboard-menu__controls">
          <Tabs label="Phân tích thực đơn" options={[[ 'category', 'Theo nhóm' ], [ 'type', 'Theo loại' ]]} value={filters.menuGroup} onChange={(v) => update('menuGroup', v)} />
          <PeriodSelect label="Thời gian Hiệu quả thực đơn" value={filters.menuDays} onChange={(v) => update('menuDays', v)} />
        </div></div>
        <div className="dashboard-menu__averages">{[
          [Utensils, 'Giá trị trung bình/món', menu.averageItemValue], [Sandwich, 'Giá trị trung bình/đồ ăn', menu.averageFoodValue], [CupSoda, 'Giá trị trung bình/đồ uống', menu.averageDrinkValue]
        ].map(([Icon, label, value]) => <div key={label}><Icon size={32} aria-hidden="true" /><div><span title="Doanh thu thuần chia cho số lượng món bán ra trong khoảng thời gian đã chọn">{label} ⓘ</span><strong>{money(value)}</strong></div></div>)}</div>
        {menu.unclassifiedQuantity > 0 && <p className="dashboard-note">{menu.unclassifiedQuantity} món bán ra chưa phân loại Đồ ăn/Đồ uống. Bạn có thể cập nhật trong Thực đơn.</p>}
        <div className="dashboard-menu__details">
          <div className="dashboard-menu__groups"><h4>{filters.menuGroup === 'category' ? 'Nhóm món' : 'Loại món'}</h4>
            {menu.sales.length === 0 ? <Empty>Chưa có doanh thu theo {filters.menuGroup === 'category' ? 'nhóm' : 'loại'} món</Empty> : <>
              {menu.sales.some((i) => i.totalRevenue > 0) && <div className="dashboard-menu__donut"><ResponsiveContainer width="100%" height="100%"><PieChart><Pie data={menu.sales} dataKey="totalRevenue" nameKey="categoryName" innerRadius={54} outerRadius={80} paddingAngle={2}>{menu.sales.map((i, n) => <Cell key={i.key} fill={colors[n % colors.length]} />)}</Pie><Tooltip formatter={(v) => money(v)} /></PieChart></ResponsiveContainer></div>}
              <ul className="dashboard-menu__legend">{menu.sales.map((item, n) => <li key={item.key}><i style={{ background: colors[n % colors.length] }} /><span>{item.categoryName}</span><strong>{money(item.totalRevenue)}</strong></li>)}</ul>
            </>}
          </div>
          <div className="dashboard-menu__top"><div className="chart-card__head"><h4>Chi tiết từng {filters.menuGroup === 'category' ? 'nhóm' : 'loại'} món</h4>
            <select className="chart-card__select" aria-label="Lọc món bán chạy" value={filters.menuFilter} onChange={(e) => update('menuFilter', e.target.value)}><option value="">Tất cả {filters.menuGroup === 'category' ? 'nhóm' : 'loại'}</option>{menu.filterOptions.map((item) => <option key={item.key} value={item.key}>{item.categoryName}</option>)}</select>
          </div><div className="dashboard-menu__table-wrap"><table><thead><tr><th>STT</th><th>Top 10 món bán chạy</th><th aria-sort={filters.sortBy === 'quantity' ? 'descending' : 'none'}><button className={filters.sortBy === 'quantity' ? 'is-active' : ''} onClick={() => update('sortBy', 'quantity')}>Số lượng bán {filters.sortBy === 'quantity' ? '↓' : ''}</button></th><th aria-sort={filters.sortBy === 'revenue' ? 'descending' : 'none'}><button className={filters.sortBy === 'revenue' ? 'is-active' : ''} onClick={() => update('sortBy', 'revenue')}>Doanh thu trước hoàn {filters.sortBy === 'revenue' ? '↓' : ''}</button></th></tr></thead>
            <tbody>{menu.topSellingFoods.map((item) => <tr key={item.foodId}><td>{item.stt}</td><td>{item.foodName}</td><td>{item.quantitySold.toLocaleString('vi-VN')}</td><td>{money(item.totalRevenue)}</td></tr>)}</tbody></table></div>
            {menu.topSellingFoods.length === 0 && <Empty>Chưa có món nào được bán trong lựa chọn này</Empty>}
          </div>
        </div>
      </section>
    </div>}
  </main>
}
export default Dashboard
