import { NavLink, useNavigate } from 'react-router-dom'
import './Header.css'

const NAV_ITEMS = [
  { label: 'Tổng quan', path: '/dashboard' },
  { label: 'Thực đơn', path: '/menu' },
  { label: 'Kho hàng', path: '/inventory' },
  { label: 'Phòng/Bàn', path: '/tables' },
  { label: 'Đơn hàng', path: '/orders' },
  { label: 'Khách hàng', path: '/customers' },
  { label: 'Nhân viên', path: '/staff' },
  { label: 'Sổ quỹ', path: '/cashbook' },
  { label: 'Báo cáo', path: '/reports' },
]

function Header() {
  const navigate = useNavigate()
  const user = JSON.parse(localStorage.getItem('user') || '{}')

  const logout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    navigate('/login')
  }

  return (
    <header className="app-header">
      <div className="app-header__brand">
        <span className="app-header__logo">☕</span>
        <span className="app-header__name">Quán Cà Phê</span>
      </div>

      <nav className="app-header__nav">
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            className={({ isActive }) => `app-header__tab ${isActive ? 'is-active' : ''}`}
            style={{ textDecoration: 'none' }}
          >
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="app-header__actions">
        <button className="pill-btn">
          <span className="pill-btn__icon">🏬</span>
          Chi nhánh trung tâm
        </button>
        <button className="pill-btn pill-btn--primary" onClick={() => navigate('/pos')}>
          <span className="pill-btn__icon">🧾</span>
          <span>Bán hàng</span>
        </button>
        <button className="icon-btn" aria-label="Thông báo">
          🔔<span className="icon-btn__dot" />
        </button>
        <button className="icon-btn" aria-label="Trợ giúp">
          ?
        </button>
        <button className="pill-btn" onClick={logout} title="Đăng xuất">
          <span className="pill-btn__icon">👤</span>
          <span>{user.displayName || 'Tài khoản'}</span>
        </button>
      </div>
    </header>
  )
}

export default Header
