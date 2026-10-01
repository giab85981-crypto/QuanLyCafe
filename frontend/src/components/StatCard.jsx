import './StatCard.css'

function StatCard({ tone, title, badge, value, subtitle, rows }) {
  return (
    <div className={`stat-card stat-card--${tone}`}>
      <div className="stat-card__head">
        <span className="stat-card__title">{title}</span>
        {badge && <span className="stat-card__badge">{badge}</span>}
      </div>
      <div className="stat-card__value">{value}</div>
      <div className="stat-card__subtitle">{subtitle}</div>

      {rows?.length > 0 && (
        <div className="stat-card__rows">
          {rows.map((row) => (
            <div className="stat-card__row" key={row.label}>
              <span>{row.label}</span>
              <span className="stat-card__row-value">{row.value}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default StatCard