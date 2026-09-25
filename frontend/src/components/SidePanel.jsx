import './SidePanel.css'

const QUICK_LINKS = [
  { icon: '🛵', title: 'Giao món siêu tốc', subtitle: 'Grab, Ahamove, XanhSM' },
  { icon: '💳', title: 'Thanh toán', subtitle: 'Cài đặt QR nhận tiền miễn phí' },
  { icon: '💰', title: 'Vay vốn', subtitle: 'Giải ngân tới 1 tỷ đồng chỉ trong 24H' },
]

const ACTIVITIES = [
  {
    icon: '🛒',
    text: 'Bảo Gia vừa bán hàng với giá trị 50,000 tại Chi nhánh trung tâm',
    time: '1 ngày trước',
  },
]

function QuickLinks() {
  return (
    <div className="panel-card quick-links">
      {QUICK_LINKS.map((item) => (
        <button className="quick-links__item" key={item.title}>
          <span className="quick-links__icon">{item.icon}</span>
          <span className="quick-links__text">
            <span className="quick-links__title">{item.title}</span>
            <span className="quick-links__subtitle">{item.subtitle}</span>
          </span>
          <span className="quick-links__chevron">›</span>
        </button>
      ))}
    </div>
  )
}

function PromoCard() {
  return (
    <div className="panel-card promo-card">
      <span className="promo-card__badge">Mới</span>
      <h4>Khuyến mại</h4>
      <p>Dễ dàng tạo các loại khuyến mại phổ biến nhất</p>
      <a href="#" className="promo-card__link">
        Xem ngay →
      </a>
    </div>
  )
}

function ActivityFeed() {
  return (
    <div className="panel-card activity-card">
      <h4>Hoạt động gần đây</h4>
      <ul className="activity-card__list">
        {ACTIVITIES.map((a, i) => (
          <li key={i}>
            <span className="activity-card__icon">{a.icon}</span>
            <span className="activity-card__text">
              {a.text}
              <span className="activity-card__time">{a.time}</span>
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}

export { QuickLinks, PromoCard, ActivityFeed }