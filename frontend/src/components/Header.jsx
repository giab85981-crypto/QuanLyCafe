import { useEffect, useRef, useState } from 'react'
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

// Dữ liệu thông báo mẫu — sau này thay bằng gọi API
const INITIAL_NOTIFICATIONS = [
  { id: 1, icon: '📦', title: 'Sắp hết hàng', text: 'Cà phê hạt Robusta còn dưới mức tối thiểu.', time: '5 phút trước', path: '/inventory', read: false },
  { id: 2, icon: '🧾', title: 'Đơn hàng mới', text: 'Bàn 01 vừa gọi thêm 2 món.', time: '12 phút trước', path: '/pos', read: false },
  { id: 3, icon: '💰', title: 'Thanh toán thành công', text: 'Hóa đơn #1024 đã thanh toán 50,000đ.', time: '1 giờ trước', path: '/orders', read: false },
  { id: 4, icon: '👤', title: 'Ca làm việc', text: 'Nhân viên ca chiều đã vào ca.', time: 'Hôm qua', path: '/staff', read: true },
]

const HELP_ITEMS = [
  {  label: 'Hướng dẫn sử dụng', action: 'guide' },
  {  label: 'Phím tắt', action: 'shortcuts' },
  { label: 'Mở màn hình bán hàng', action: 'pos' },
  { label: 'Liên hệ hỗ trợ', action: 'contact' },
]

function Header() {
  const navigate = useNavigate()
  const user = JSON.parse(localStorage.getItem('user') || '{}')

  const [openPanel, setOpenPanel] = useState(null) // 'bell' | 'help' | null
  const [notifications, setNotifications] = useState(INITIAL_NOTIFICATIONS)
  const [helpView, setHelpView] = useState('menu') // 'menu' | 'guide' | 'shortcuts' | 'contact'
  const bellRef = useRef(null)
  const helpRef = useRef(null)

  const unread = notifications.filter((n) => !n.read).length

  // Đóng khi bấm ra ngoài hoặc nhấn Esc
  useEffect(() => {
    const onClick = (e) => {
      if (bellRef.current?.contains(e.target) || helpRef.current?.contains(e.target)) return
      setOpenPanel(null)
    }
    const onKey = (e) => e.key === 'Escape' && setOpenPanel(null)
    document.addEventListener('mousedown', onClick)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onClick)
      document.removeEventListener('keydown', onKey)
    }
  }, [])

  const logout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    navigate('/login')
  }

  const toggle = (name) => {
    setOpenPanel((cur) => (cur === name ? null : name))
    if (name === 'help') setHelpView('menu')
  }

  const markAllRead = () => setNotifications((list) => list.map((n) => ({ ...n, read: true })))
  const clearAll = () => setNotifications([])

  const openNotification = (n) => {
    setNotifications((list) => list.map((x) => (x.id === n.id ? { ...x, read: true } : x)))
    setOpenPanel(null)
    navigate(n.path)
  }

  const onHelpAction = (action) => {
    if (action === 'pos') {
      setOpenPanel(null)
      navigate('/pos')
    } else {
      setHelpView(action)
    }
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

        {/* Chuông thông báo */}
        <div className="header-pop" ref={bellRef}>
          <button
            className={`icon-btn ${openPanel === 'bell' ? 'is-open' : ''}`}
            aria-label="Thông báo"
            onClick={() => toggle('bell')}
          >
            🔔
            {unread > 0 && <span className="icon-btn__count">{unread > 9 ? '9+' : unread}</span>}
          </button>

          {openPanel === 'bell' && (
            <div className="popover popover--wide">
              <div className="popover__head">
                <h3>Thông báo {unread > 0 && <span className="popover__badge">{unread} mới</span>}</h3>
                <div className="popover__head-actions">
                  <button onClick={markAllRead} disabled={unread === 0}>Đánh dấu đã đọc</button>
                  <button onClick={clearAll} disabled={notifications.length === 0}>Xóa hết</button>
                </div>
              </div>

              <div className="popover__body">
                {notifications.length === 0 ? (
                  <div className="popover__empty">
                    <span>🔕</span>
                    Không có thông báo nào
                  </div>
                ) : (
                  notifications.map((n) => (
                    <button
                      key={n.id}
                      className={`notif ${n.read ? '' : 'is-unread'}`}
                      onClick={() => openNotification(n)}
                    >
                      <span className="notif__icon">{n.icon}</span>
                      <span className="notif__content">
                        <span className="notif__title">{n.title}</span>
                        <span className="notif__text">{n.text}</span>
                        <span className="notif__time">{n.time}</span>
                      </span>
                      {!n.read && <span className="notif__dot" />}
                    </button>
                  ))
                )}
              </div>
            </div>
          )}
        </div>

        {/* Dấu ? trợ giúp */}
        <div className="header-pop" ref={helpRef}>
          <button
            className={`icon-btn ${openPanel === 'help' ? 'is-open' : ''}`}
            aria-label="Trợ giúp"
            onClick={() => toggle('help')}
          >
            ?
          </button>

          {openPanel === 'help' && (
            <div className="popover">
              <div className="popover__head">
                {helpView !== 'menu' ? (
                  <button className="popover__back" onClick={() => setHelpView('menu')}>← Quay lại</button>
                ) : (
                  <h3>Trợ giúp</h3>
                )}
              </div>

              <div className="popover__body popover__body--pad">
                {helpView === 'menu' &&
                  HELP_ITEMS.map((h) => (
                    <button key={h.action} className="help-item" onClick={() => onHelpAction(h.action)}>
                      <span>{h.icon}</span>
                      {h.label}
                    </button>
                  ))}

                {helpView === 'guide' && (
                  <ol className="help-list">
                    <li>Vào <b>Thực đơn</b> để thêm/sửa món và giá.</li>
                    <li>Bấm <b>Bán hàng</b>, chọn bàn rồi chọn món để tạo đơn.</li>
                    <li>Bấm thanh toán để đóng hóa đơn.</li>
                    <li>Xem doanh thu ở <b>Tổng quan</b> và <b>Báo cáo</b>.</li>
                  </ol>
                )}

                {helpView === 'shortcuts' && (
                  <ul className="help-list help-list--keys">
                    <li><kbd>Esc</kbd> Đóng hộp thoại / bảng này</li>
                    <li><kbd>Ctrl</kbd> + <kbd>F5</kbd> Tải lại trang, xóa cache</li>
                  </ul>
                )}

                {helpView === 'contact' && (
                  <div className="help-list">
                    <p>Cần hỗ trợ? Liên hệ quản trị hệ thống của quán.</p>
                    <p>📞 Hotline: <b>0900 000 000</b></p>
                    <p>✉️ Email: <b>support@quancafe.vn</b></p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        <button className="pill-btn" onClick={logout} title="Đăng xuất">
          <span className="pill-btn__icon">👤</span>
          <span>{user.displayName || 'Tài khoản'}</span>
        </button>
      </div>
    </header>
  )
}

export default Header
