import { useState } from 'react'
import './Header.css'

const NAV_ITEMS = [
  'Tổng quan',
  'Thực đơn',
  'Kho hàng',
  'Phòng/Bàn',
  'Đơn hàng',
  'Khách hàng',
  'Nhân viên',
  'Sổ quỹ',
  'Báo cáo',
]

function Header() {
  const [active, setActive] = useState('Tổng quan')

  return (
    <header className="app-header">
      <div className="app-header__brand">
        <span className="app-header__logo">☕</span>
        <span className="app-header__name">Quán Cà Phê</span>
      </div>

      <nav className="app-header__nav">
        {NAV_ITEMS.map((item) => (
          <button
            key={item}
            className={`app-header__tab ${active === item ? 'is-active' : ''}`}
            onClick={() => setActive(item)}
          >
            {item}
          </button>
        ))}
        <button className="app-header__tab app-header__tab--more">
          Khác <span className="chevron">▾</span>
        </button>
      </nav>

      <div className="app-header__actions">
        <button className="pill-btn">
          <span className="pill-btn__icon">🏬</span>
          Chi nhánh trung tâm
        </button>
        <button className="pill-btn pill-btn--primary">
          <span className="pill-btn__icon">🧾</span>
          Thu ngân
          <span className="chevron">▾</span>
        </button>
        <button className="icon-btn" aria-label="Thông báo">
          🔔<span className="icon-btn__dot" />
        </button>
        <button className="icon-btn" aria-label="Trợ giúp">
          ?
        </button>
        <button className="icon-btn" aria-label="Cài đặt">
          ⚙️
        </button>
        <button className="avatar-btn" aria-label="Tài khoản">
          👤
        </button>
      </div>
    </header>
  )
}

export default Header