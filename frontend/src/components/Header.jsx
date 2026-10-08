import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import HeaderNavigation from './HeaderNavigation'
import './Header.css'
import { canOpen, useAccess } from '../utils/staffAccess'
import { ReceiptText, ChevronDown, Bell, CircleHelp, UserRound, LogOut, ChefHat, Armchair, Coffee } from 'lucide-react'

const NAV_ITEMS = [
  { label: 'Tổng quan', path: '/dashboard' },
  { label: 'Trợ lý AI', path: '/assistant' },
  { label: 'Thực đơn', path: '/menu' },
  { label: 'Kho hàng', path: '/inventory' },
  { label: 'Phòng/Bàn', path: '/tables' },
  { label: 'Đơn hàng', path: '/orders' },
  { label: 'Khách hàng', path: '/customers' },
  { label: 'Phân quyền', path: '/permissions' },
  { label: 'Nhân viên', path: '/staff' },
  { label: 'Bếp / Bar', path: '/kitchen' },
  { label: 'Sổ quỹ', path: '/cashbook' },
  { label: 'Báo cáo', path: '/reports' },
  { label: 'Ca làm việc', path: '/shifts' },
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
  const user = useAccess()
  const availableItems = useMemo(() => NAV_ITEMS.filter(item => canOpen(item.path, user)), [user])

  const [openPanel, setOpenPanel] = useState(null) // 'bell' | 'help' | null
  const closePanel = useCallback(() => setOpenPanel(null), [])
  const [notifications, setNotifications] = useState(INITIAL_NOTIFICATIONS)
  const [helpView, setHelpView] = useState('menu') // 'menu' | 'guide' | 'shortcuts' | 'contact'
  const bellRef = useRef(null)
  const helpRef = useRef(null)
  const screensRef = useRef(null)
  const accountRef = useRef(null)
  const screens = [
    { label: 'Phòng / Bàn', path: '/tables', icon: Armchair, description: 'Xem khu vực và quản lý bàn' },
    { label: 'Bếp / Bar', path: '/kitchen', icon: ChefHat, description: 'Nhận phiếu và cập nhật chế biến' },
  ].filter(item => canOpen(item.path, user))
  const roleLabel = user.roleName === 'Admin' ? 'Quản trị viên' : user.roleName === 'Kitchen' ? 'Bếp / Bar' : user.roleName === 'Cashier' ? 'Thu ngân' : user.roleName

  const unread = notifications.filter((n) => !n.read).length

  // Đóng khi bấm ra ngoài hoặc nhấn Esc
  useEffect(() => {
    const onClick = (e) => {
      if (e.target.closest('.app-header__nav')) return
      if ([bellRef, helpRef, screensRef, accountRef].some(ref => ref.current?.contains(e.target))) return
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
        <span className="app-header__logo"><Coffee size={24} strokeWidth={2}/></span>
        <span className="app-header__name">Quán Cà Phê</span>
      </div>

      <HeaderNavigation items={availableItems} open={openPanel === 'more'}
        onToggle={() => toggle('more')} onClose={closePanel}/>

      <div className="app-header__actions">
        {canOpen('/pos', user) && <div className="header-pop" ref={screensRef}>
          <div className="header-sales">
            <button className="header-sales__main" onClick={() => { setOpenPanel(null); navigate('/pos') }} title="Mở màn hình bán hàng"><ReceiptText size={17}/><span>Thu ngân</span></button>
            {screens.length > 0 && <button className={`header-sales__toggle ${openPanel === 'screens' ? 'is-open' : ''}`} aria-label="Chuyển màn hình làm việc" aria-haspopup="menu" aria-expanded={openPanel === 'screens'} aria-controls="header-screens-menu" onClick={() => toggle('screens')}><ChevronDown size={16}/></button>}
          </div>
          {openPanel === 'screens' && <div className="popover header-screen-menu" id="header-screens-menu" role="menu" aria-label="Màn hình làm việc">
            {screens.map(item => <button role="menuitem" className="header-screen-item" key={item.path} onClick={() => { setOpenPanel(null); navigate(item.path) }}><item.icon size={19}/><span><b>{item.label}</b><small>{item.description}</small></span></button>)}
          </div>}
        </div>}

        {/* Chuông thông báo */}
        <div className="header-pop" ref={bellRef}>
          <button
            className={`icon-btn ${openPanel === 'bell' ? 'is-open' : ''}`}
            aria-label="Thông báo"
            aria-expanded={openPanel === 'bell'}
            onClick={() => toggle('bell')}
          >
            <Bell size={18}/>
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
            aria-expanded={openPanel === 'help'}
            onClick={() => toggle('help')}
          >
            <CircleHelp size={18}/>
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
                  HELP_ITEMS.filter(h => h.action !== 'pos' || canOpen('/pos', user)).map((h) => (
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
                    <li>Xem doanh thu ở <b>Tổng quan</b> và lịch sử tại <b>Đơn hàng</b>.</li>
                  </ol>
                )}

                {helpView === 'shortcuts' && (
                  <ul className="help-list help-list--keys">
                    <li><kbd>F3</kbd> Tìm món ở Thu ngân</li><li><kbd>F9</kbd> Mở thanh toán</li><li><kbd>F10</kbd> Báo bếp</li><li><kbd>Esc</kbd> Đóng hộp thoại / bảng này</li>
                    <li><kbd>Ctrl</kbd> + <kbd>F5</kbd> Tải lại trang, xóa cache</li>
                  </ul>
                )}

                {helpView === 'contact' && (
                  <div className="help-list">
                    <p>Cần hỗ trợ? Liên hệ quản trị hệ thống của quán.</p>

                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        <div className="header-pop" ref={accountRef}>
          <button className={`icon-btn ${openPanel === 'account' ? 'is-open' : ''}`} aria-label="Tài khoản" title={user.displayName || 'Tài khoản'} aria-haspopup="menu" aria-expanded={openPanel === 'account'} aria-controls="header-account-menu" onClick={() => toggle('account')}><UserRound size={18}/></button>
          {openPanel === 'account' && <div className="popover header-account-menu" id="header-account-menu" role="menu" aria-label="Tài khoản đăng nhập">
            <div className="header-account-info"><span className="header-account-avatar"><UserRound size={22}/></span><span><b>{user.displayName || 'Tài khoản'}</b><small>{roleLabel}</small></span></div>
            <button role="menuitem" className="header-screen-item header-account-logout" onClick={logout}><LogOut size={18}/><span>Đăng xuất</span></button>
          </div>}
        </div>
      </div>
    </header>
  )
}

export default Header
