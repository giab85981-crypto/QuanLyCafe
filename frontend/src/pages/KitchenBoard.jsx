import { errMsg as failure } from '../api/errMsg'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChefHat, Clock3, Flame, CircleCheck, RefreshCw, Search, LayoutGrid, List, ShoppingBag, Armchair, LogOut, ArrowLeft, StickyNote, ArrowRight, Coffee } from 'lucide-react'
import api from '../api/axiosClient'
import { can, useAccess, managementLanding } from '../utils/staffAccess'
import './KitchenBoard.css'

const stages = [
  { id: 'Pending', label: 'Chờ làm', icon: Clock3, caption: 'Phiếu mới · ưu tiên phiếu chờ lâu' },
  { id: 'Cooking', label: 'Đang làm', icon: Flame, caption: 'Món đang được pha chế / chế biến' },
  { id: 'Completed', label: 'Hoàn thành', icon: CircleCheck, caption: 'Các phiếu báo trong hôm nay đã làm xong' },
]
const code = id => `HD${String(id).padStart(6, '0')}`
const normalize = value => String(value).normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase()
const portions = orders => orders.reduce((sum, order) => sum + order.details.reduce((n, d) => n + d.count, 0), 0)
const elapsed = (created, now) => {
  const minutes = Math.max(0, Math.floor((now - new Date(created).getTime()) / 60000))
  return minutes < 60 ? `${minutes} phút` : `${Math.floor(minutes / 60)} giờ ${minutes % 60} phút`
}

export default function KitchenBoard({ embedded = false }) {
  const navigate = useNavigate(), user = useAccess()
  const [orders, setOrders] = useState([]), [error, setError] = useState(''), [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(null), [loading, setLoading] = useState(true), [updated, setUpdated] = useState(null)
  const [search, setSearch] = useState(''), [type, setType] = useState('all'), [mode, setMode] = useState('orders'), [now, setNow] = useState(Date.now())
  const request = useRef(0), saving = useRef(false), fetching = useRef(false)
  const load = useCallback(async () => {
    const version = ++request.current; fetching.current = true; setLoading(true)
    try {
      const [active, completed] = await Promise.all([api.get('/Kitchen/pending-orders'), api.get('/Kitchen/pending-orders', { params: { completed: true } })])
      if (version === request.current) { setOrders([...active.data, ...completed.data]); setUpdated(new Date()); setError('') }
    } catch (e) { if (version === request.current) setError(failure(e)) }
    finally { if (version === request.current) { fetching.current = false; setLoading(false) } }
  }, [])
  useEffect(() => {
    void load()
    const poll = setInterval(() => { if (!saving.current && !fetching.current && document.visibilityState === 'visible') void load() }, 15000)
    const clock = setInterval(() => setNow(Date.now()), 30000)
    return () => { request.current++; clearInterval(poll); clearInterval(clock) }
  }, [load])
  const change = async order => {
    if (saving.current) return
    saving.current = true; request.current++; setBusy(order.id); setError(''); setNotice('')
    try {
      await api.put(`/Kitchen/${order.id}/status`, { status: order.status === 'Pending' ? 'Cooking' : 'Completed' })
      await load(); setNotice(`Phiếu #${order.id} · ${order.tableName}: ${order.status === 'Pending' ? 'đã bắt đầu làm' : 'đã hoàn thành'}.`)
    } catch (e) { await load(); setError(failure(e)) }
    finally { saving.current = false; setBusy(null) }
  }
  const shown = orders.filter(o => (type === 'all' || o.orderType === type) && (!search.trim() || normalize(`${o.tableName} ${code(o.idBill)} ${o.id} ${o.note || ''} ${o.details.map(d => `${d.foodName} ${d.optionLabel}`).join(' ')}`).includes(normalize(search.trim()))))
  const logout = () => { localStorage.removeItem('token'); localStorage.removeItem('user'); navigate('/login') }
  return <section className={`kitchen-display ${embedded ? 'is-embedded' : ''}`}>
    <header className="kb-top">
      <div className="kb-brand"><span><ChefHat size={25}/></span><div><h1>Bếp / Bar</h1><small>Chi nhánh trung tâm</small></div></div>
      <div className="kb-user"><span>{user.displayName || 'Nhân viên bếp'}<small>{user.roleName === 'Admin' ? 'Quản trị viên' : user.roleName === 'Kitchen' ? 'Pha chế / chế biến' : user.roleName}</small></span>{!embedded && <>{managementLanding(user) && <button onClick={() => navigate(managementLanding(user))}><ArrowLeft size={16}/> Quản lý</button>}<button aria-label="Đăng xuất Bếp / Bar" onClick={logout}><LogOut size={17}/></button></>}</div>
    </header>
    <div className="kb-workspace">
      <div className="kb-stats">{stages.map(stage => { const rows = orders.filter(o => o.status === stage.id); return <article key={stage.id} className={`kb-stat ${stage.id}`}><stage.icon size={22}/><div><small>{stage.label}</small><b>{portions(rows)} <span>phần</span></b><p>{rows.length} phiếu{stage.id === 'Completed' ? ' báo hôm nay' : ''}</p></div></article> })}</div>
      <div className="kb-toolbar"><label className="kb-search"><Search size={18}/><input aria-label="Tìm phiếu bếp" placeholder="Tìm bàn, mã hóa đơn hoặc tên món..." value={search} onChange={e => setSearch(e.target.value)}/></label><select aria-label="Hình thức phục vụ" value={type} onChange={e => setType(e.target.value)}><option value="all">Tất cả đơn</option><option value="DineIn">Tại quán</option><option value="Takeaway">Mang về</option></select><div className="kb-view"><button aria-pressed={mode === 'orders'} onClick={() => setMode('orders')}><LayoutGrid size={16}/> Theo phiếu</button><button aria-pressed={mode === 'foods'} onClick={() => setMode('foods')}><List size={16}/> Theo món</button></div><button className="kb-refresh" disabled={loading || busy !== null} onClick={load}><RefreshCw size={16} className={loading ? 'kb-spin' : ''}/><span>Làm mới</span></button></div>
      <div className="kb-sync"><span className={error ? 'offline' : ''}/>{error ? 'Chưa cập nhật được dữ liệu' : updated ? `Cập nhật ${updated.toLocaleTimeString('vi-VN')} · Tự làm mới 15 giây` : 'Đang tải phiếu bếp...'}<small>Phiếu chờ lâu nhất nằm trên cùng</small></div>
      {error && <div className="kb-alert" role="alert">{error}</div>}{notice && <div className="kb-notice" role="status">{notice}<button aria-label="Đóng thông báo" onClick={() => setNotice('')}>×</button></div>}
      <div className="kb-columns">{stages.map(stage => {
        const rows = shown.filter(o => o.status === stage.id).sort((a, b) => stage.id === 'Completed' ? new Date(b.createdAt) - new Date(a.createdAt) : new Date(a.createdAt) - new Date(b.createdAt))
        const grouped = new Map()
        rows.forEach(o => o.details.forEach(d => { const key = `${d.idFood}|${d.optionLabel}`; const item = grouped.get(key) || { name: d.foodName, option: d.optionLabel, count: 0, orders: new Set() }; item.count += d.count; item.orders.add(o.tableName); grouped.set(key, item) }))
        return <section key={stage.id} className={`kb-column ${stage.id}`}><header className="kb-column-head"><span><stage.icon size={18}/><b>{stage.label}</b><em>{rows.length}</em></span><small>{stage.caption}</small></header><div className="kb-column-body">
          {!rows.length && <div className="kb-empty"><stage.icon size={42}/><h3>{loading && !updated ? 'Đang tải...' : search || type !== 'all' ? 'Không có phiếu phù hợp' : stage.id === 'Pending' ? 'Chưa có món chờ làm' : stage.id === 'Cooking' ? 'Chưa có phiếu đang làm' : 'Chưa có phiếu hoàn thành'}</h3><p>{search || type !== 'all' ? 'Thử đổi từ khóa hoặc bộ lọc hình thức phục vụ.' : stage.id === 'Pending' ? 'Món sẽ hiện khi thu ngân báo bếp hoặc thanh toán.' : stage.id === 'Cooking' ? 'Bắt đầu một phiếu bên cột Chờ làm.' : 'Phiếu đã hoàn thành trong hôm nay sẽ hiện tại đây.'}</p></div>}
          {mode === 'foods' ? [...grouped.entries()].map(([key, item]) => <article className="kb-food" key={key}><div><Coffee size={19}/><b>{item.name}</b><strong>{item.count}</strong></div>{item.option && <p>{item.option}</p>}<small>{[...item.orders].join(' · ')}</small><button onClick={() => { setSearch(item.name); setMode('orders') }}>Xem phiếu <ArrowRight size={14}/></button></article>) : rows.map(o => {
            const late = stage.id !== 'Completed' && now - new Date(o.createdAt).getTime() >= 15 * 60000
            return <article className="kb-ticket" key={o.id}><div className="kb-ticket-head"><b>{o.orderType === 'Takeaway' ? <ShoppingBag size={17}/> : <Armchair size={17}/>} {o.tableName}</b><span>{o.orderType === 'Takeaway' ? 'Mang về' : 'Tại quán'}</span></div><div className="kb-ticket-meta"><span>{code(o.idBill)} · Phiếu #{o.id}</span><time title={new Date(o.createdAt).toLocaleString('vi-VN')}>{new Date(o.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</time></div>{stage.id !== 'Completed' && <div className={`kb-wait ${late ? 'is-late' : ''}`}><Clock3 size={13}/> Đã báo {elapsed(o.createdAt, now)} trước{late && <b>Chờ lâu</b>}</div>}<ul>{o.details.map(d => <li key={d.id}><strong>{d.count}</strong><span><b>{d.foodName}</b>{d.optionLabel && <small>{d.optionLabel}</small>}</span></li>)}</ul>{o.note && <p className="kb-note"><StickyNote size={14}/>{o.note}</p>}<footer><small>{o.details.reduce((sum, d) => sum + d.count, 0)} phần · Toàn phiếu</small>{stage.id !== 'Completed' ? <button hidden={!can('KITCHEN_UPDATE')} disabled={busy !== null} onClick={() => change(o)}>{busy === o.id ? 'Đang lưu...' : stage.id === 'Pending' ? 'Bắt đầu làm' : 'Hoàn thành'}{stage.id === 'Pending' ? <ArrowRight size={16}/> : <CircleCheck size={16}/>}</button> : <span className="kb-done"><CircleCheck size={15}/> Đã làm xong</span>}</footer></article>
          })}
        </div></section>
      })}</div>
    </div>
  </section>
}
