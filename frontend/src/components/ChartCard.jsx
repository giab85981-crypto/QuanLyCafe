import { useState } from 'react'
import './ChartCard.css'

const TABS = ['Theo giờ', 'Theo ngày', 'Theo thứ']

// Demo data: revenue by hour of day (in thousands VND)
const HOURLY = [
  0, 0, 0, 0, 0, 0, 0, 0, 22, 28, 35, 42, 50, 41, 30, 20, 18, 12, 8, 4, 0, 0, 0, 0,
]

function RevenueChart() {
  const max = Math.max(...HOURLY, 1)
  return (
    <div className="rev-chart">
      <div className="rev-chart__bars">
        {HOURLY.map((v, i) => (
          <div className="rev-chart__col" key={i}>
            <div
              className="rev-chart__bar"
              style={{ height: `${(v / max) * 100}%` }}
              title={`${i}h: ${v}.000đ`}
            />
          </div>
        ))}
      </div>
      <div className="rev-chart__axis">
        <span>0h</span>
        <span>6h</span>
        <span>12h</span>
        <span>18h</span>
        <span>23h</span>
      </div>
    </div>
  )
}

function ChartCard({ title, tooltip, value, valueSuffix, periodOptions, defaultPeriod }) {
  const [tab, setTab] = useState(TABS[0])
  const [period, setPeriod] = useState(defaultPeriod)

  return (
    <div className="chart-card">
      <div className="chart-card__head">
        <div className="chart-card__title-wrap">
          <h3>{title}</h3>
          {tooltip && <span className="chart-card__info" title={tooltip}>ⓘ</span>}
        </div>
        {periodOptions && (
          <select
            className="chart-card__select"
            value={period}
            onChange={(e) => setPeriod(e.target.value)}
          >
            {periodOptions.map((opt) => (
              <option key={opt}>{opt}</option>
            ))}
          </select>
        )}
      </div>

      {value !== undefined && (
        <div className="chart-card__value">
          {value}
          {valueSuffix && <span className="chart-card__value-suffix"> {valueSuffix}</span>}
        </div>
      )}

      <div className="chart-card__tabs">
        {TABS.map((t) => (
          <button
            key={t}
            className={`chart-card__tab ${tab === t ? 'is-active' : ''}`}
            onClick={() => setTab(t)}
          >
            {t}
          </button>
        ))}
      </div>

      <RevenueChart />
    </div>
  )
}

export default ChartCard