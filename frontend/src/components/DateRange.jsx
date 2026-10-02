// Chọn khoảng ngày + các nút chọn nhanh
export const toISO = (d) => {
  const p = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
}

export const presets = {
  today: () => {
    const t = toISO(new Date())
    return [t, t]
  },
  week: () => {
    const e = new Date()
    const s = new Date()
    s.setDate(e.getDate() - 6)
    return [toISO(s), toISO(e)]
  },
  month: () => {
    const e = new Date()
    return [toISO(new Date(e.getFullYear(), e.getMonth(), 1)), toISO(e)]
  },
}

function DateRange({ from, to, onChange }) {
  const is = (key) => {
    const [f, t] = presets[key]()
    return f === from && t === to
  }
  return (
    <div className="pg-tools">
      {[
        ['today', 'Hôm nay'],
        ['week', '7 ngày qua'],
        ['month', 'Tháng này'],
      ].map(([key, label]) => (
        <button
          key={key}
          className={`pg-btn ${is(key) ? 'pg-btn--primary' : ''}`}
          onClick={() => onChange(...presets[key]())}
        >
          {label}
        </button>
      ))}
      <input className="pg-input" type="date" value={from} max={to} onChange={(e) => e.target.value && onChange(e.target.value, to)} />
      <span className="pg-mute">đến</span>
      <input className="pg-input" type="date" value={to} min={from} onChange={(e) => e.target.value && onChange(from, e.target.value)} />
    </div>
  )
}

export default DateRange