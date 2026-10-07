import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, Tooltip, CartesianGrid } from 'recharts'
import { BarChart3, Users } from 'lucide-react'
import './ChartCard.css'
import { money } from '../utils/format'

export function PeriodSelect({ value, onChange, label }) {
  return <select className="chart-card__select" aria-label={label} value={value} onChange={(e) => onChange(Number(e.target.value))}>
    <option value={1}>Hôm nay</option><option value={7}>7 ngày qua</option><option value={30}>30 ngày qua</option><option value={90}>90 ngày qua</option>
  </select>
}
export function Tabs({ options, value, onChange, label }) {
  return <div className="chart-card__tabs" role="tablist" aria-label={label}>{options.map(([key, text]) =>
    <button key={key} role="tab" aria-selected={key === value} className={`chart-card__tab ${key === value ? 'is-active' : ''}`} onClick={() => onChange(key)}>{text}</button>
  )}</div>
}
export function Empty({ children, customers = false }) {
  const Icon = customers ? Users : BarChart3
  return <div className="empty-chart"><Icon size={44} strokeWidth={1.5} aria-hidden="true" /><span>{children}</span></div>
}
export default function ChartCard({ title, data, days, onDaysChange, group, onGroupChange, customers = false }) {
  const key = customers ? 'guestCount' : 'revenue'
  const hasData = data.points.some((p) => p[key] !== 0)
  return <section className="chart-card" aria-label={title}>
    <div className="chart-card__head"><h3>{title}</h3><PeriodSelect label={`Thời gian ${title}`} value={days} onChange={onDaysChange} /></div>
    <div className="chart-card__value">{customers ? `${data.guestCount.toLocaleString('vi-VN')} lượt khách` : money(data.revenue)}</div>
    {!customers && <span className="chart-card__value-suffix">({data.orderCount} hóa đơn)</span>}
    <Tabs label={`Nhóm ${title}`} options={[[ 'hour', 'Theo giờ' ], [ 'day', 'Theo ngày' ], [ 'weekday', 'Theo thứ' ]]} value={group} onChange={onGroupChange} />
    <div className="dashboard-chart">
      {!hasData ? <Empty customers={customers}>{customers ? 'Chưa có lượt khách được ghi nhận' : 'Chưa phát sinh doanh thu trong thời gian này'}</Empty> :
        <ResponsiveContainer width="100%" height="100%"><BarChart data={data.points} margin={{ top: 16, right: 12, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--line)" />
          <XAxis dataKey="date" tickLine={false} tick={{ fontSize: 11 }} minTickGap={16} />
          <YAxis allowDecimals={!customers} tickLine={false} axisLine={false} width={50} tick={{ fontSize: 11 }} tickFormatter={(n) => n >= 1000000 ? `${n / 1000000}tr` : n >= 1000 ? `${n / 1000}k` : n} />
          <Tooltip formatter={(n) => [customers ? `${n} lượt khách` : money(n), customers ? 'Lượt khách' : 'Doanh thu thuần']} />
          <Bar dataKey={key} fill={customers ? '#36a6e8' : '#0070e0'} radius={[4, 4, 0, 0]} maxBarSize={36} />
        </BarChart></ResponsiveContainer>}
    </div>
    {customers && data.unknownGuestOrders > 0 && <p className="dashboard-note">{data.unknownGuestOrders} hóa đơn cũ chưa ghi số khách, chưa tính vào lượt khách.</p>}
    {!customers && data.estimatedRevenueOrders > 0 && <p className="dashboard-note">{data.estimatedRevenueOrders} hóa đơn cũ chưa lưu tổng tiền; doanh thu được ước tính theo giá món hiện tại.</p>}
  </section>
}
