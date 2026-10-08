import QrOrderInbox from '../components/QrOrderInbox'
import { errMsg } from '../api/errMsg'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import axiosClient from '../api/axiosClient'
import './POS.css'
import './Shifts.css'
import Modal from '../components/Modal'
import CustomerPicker from '../components/CustomerPicker'
import TableTransferModal from '../components/TableTransferModal'
import { Coffee, Search, ShoppingBag, Utensils, Bell, CreditCard, Menu, LogOut, ChevronDown, Armchair } from 'lucide-react'
import { canOpen, can, useAccess, managementLanding } from '../utils/staffAccess'

const money = (n) => Number(n || 0).toLocaleString('vi-VN')

function POS() {
  const navigate = useNavigate()
  const user = useAccess()

  const [view, setView] = useState(() => window.location.search ? 'menu' : 'tables')
  const [tableSearch, setTableSearch] = useState(''), [tableStatus, setTableStatus] = useState('all'), [area, setArea] = useState('')
  const [paymentOpen, setPaymentOpen] = useState(false), [menuOpen, setMenuOpen] = useState(false)
  const searchRef = useRef(null)
  const [tables, setTables] = useState([])
  const [foods, setFoods] = useState([])
  const [categories, setCategories] = useState([])
  const [activeCat, setActiveCat] = useState(null)
  const [keyword, setKeyword] = useState('')
  const [initialTableId] = useState(() => Number(new URLSearchParams(window.location.search).get("table")) || null)
  const [initialBillId] = useState(() => Number(new URLSearchParams(window.location.search).get("bill")) || null)
  const [takeawayId, setTakeawayId] = useState(initialBillId)
  const [tableId, setTableId] = useState(initialTableId)
  const [bill, setBill] = useState(null)
  const [discount, setDiscount] = useState(0)
  const [redeemPoints, setRedeemPoints] = useState(0)
  useEffect(() => { setRedeemPoints(0) }, [bill?.idBill, bill?.customer?.id])
  const [paymentMethod, setPaymentMethod] = useState('Cash')
  const [guestCount, setGuestCount] = useState(1)
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState('')
  const [selection, setSelection] = useState(null)
  const [selectedVariant, setSelectedVariant] = useState(null)
  const [toppings, setToppings] = useState({})
  const [cancelItem, setCancelItem] = useState(null)
  const [cancelReason, setCancelReason] = useState('')
  const [kitchen, setKitchen] = useState(null)
  const [transfer, setTransfer] = useState(false)
  const [shift, setShift] = useState(undefined)
  useEffect(() => {
    let active = true
    const read = () => { if (document.visibilityState === 'visible') axiosClient.get('/Shift/current').then(r => { if(active) setShift(r.data.shift) }).catch(() => {}) }
    read(); const timer = setInterval(read, 10000); window.addEventListener('focus', read)
    return () => { active = false; clearInterval(timer); window.removeEventListener('focus', read) }
  }, [])

  useEffect(() => {
    if (!can('POS_CHECKOUT')) setPaymentOpen(false)
    if (!can('POS_ORDER')) setSelection(null)
    if (!can('POS_CANCEL')) setCancelItem(null)
    if (!can('POS_TRANSFER')) setTransfer(false)
    if (!can('KITCHEN_VIEW')) setKitchen(null)
    if (!can('POS_DISCOUNT')) { setDiscount(0); setRedeemPoints(0) }
  }, [user.roleName, JSON.stringify(user.permissions)])

  const loadTables = useCallback(
    () => axiosClient.get('/TableFood').then((r) => setTables(r.data)),
    [],
  )

  const loadFoods = useCallback(() => axiosClient.get('/Food').then(r => setFoods(r.data)), [])

  const loadBill = useCallback((id, orderId = null) => {
    if (!id && !orderId) return setBill(null)
    return axiosClient
      .get(orderId ? `/Orders/${orderId}` : `/Bill/table/${id}`)
      .then((r) => {
        if (r.data.status !== 0) throw new Error("Đơn đã thanh toán hoặc đóng. Vào Đơn hàng để xem lịch sử.")
        setBill(r.data)
        setDiscount(can('POS_DISCOUNT') ? r.data.discount || 0 : 0)
        setGuestCount(r.data.guestCount ?? 1)
        return r.data
      })
      .catch(e => { if (e.response?.status === 404) { setBill(null); setDiscount(0); setGuestCount(1) } else throw e })
  }, [])

  useEffect(() => {
    Promise.all([axiosClient.get('/Food'), axiosClient.get('/FoodCategory'), loadTables()])
      .then(([f, c]) => {
        setFoods(f.data)
        setCategories(c.data)
        if (initialBillId) return loadBill(null, initialBillId)
        if (initialTableId) return loadBill(initialTableId)
      })
      .catch(() => setMsg('Không kết nối được máy chủ.'))
  }, [loadTables, loadBill, initialTableId, initialBillId])

  useEffect(() => {
    const timer = setInterval(() => {
      if (!busy && document.visibilityState === 'visible') void Promise.all([loadFoods(), loadTables()]).catch(() => {})
    }, 15000)
    return () => clearInterval(timer)
  }, [busy, loadFoods, loadTables])

  const pickTable = (id) => {
    setTakeawayId(null)
    setTableId(id)
    navigate(`/pos?table=${id}`, { replace: true })
    setMsg(''); setView('menu')
    void run(() => loadBill(id))
  }

  const run = async (fn, okMsg) => {
    setBusy(true)
    try {
      await fn()
      if (okMsg) setMsg(okMsg)
    } catch (e) {
      setMsg(errMsg(e))
    } finally {
      setBusy(false)
    }
  }

  const pickFood = food => { if (!can('POS_ORDER')) return;
    if (!food || !food.isActive) return
    setSelection(food); setSelectedVariant(food.variants?.find(v => v.isActive && v.availableQuantity !== 0)?.id ?? null); setToppings({})
  }
  const addSelection = () => run(async () => {
    await axiosClient.post('/Bill/add-item', { idTable: tableId, idBill: takeawayId, idFood: selection.id, count: 1, idVariant: selectedVariant,
      toppings: Object.entries(toppings).filter(([, count]) => count > 0).map(([idFood, count]) => ({ idFood: Number(idFood), count })) })
    setSelection(null); await Promise.all([loadBill(tableId, takeawayId), loadTables(), loadFoods()])
  }, 'Đã thêm món và giữ nguyên liệu theo công thức.')
  const decrease = item => {
    if (item.count <= item.sentCount) { setCancelItem(item); setCancelReason(''); return }
    run(async () => { await axiosClient.post(`/Bill/items/${item.idBillInfo}/cancel`, { count: 1 }); await Promise.all([loadBill(tableId, takeawayId), loadFoods()]) })
  }
  const cancel = () => run(async () => {
    await axiosClient.post(`/Bill/items/${cancelItem.idBillInfo}/cancel`, { count: 1, reason: cancelReason }); setCancelItem(null); await Promise.all([loadBill(tableId, takeawayId), loadFoods()])
  }, 'Đã hủy 1 phần. Món chờ làm được hoàn kho; món đã làm ghi nhận hao hụt.')
  const sendKitchen = () => run(async () => {
    const { data } = await axiosClient.post('/Kitchen/send-order', { idBill: bill.idBill })
    setMsg(data.message); await Promise.all([loadBill(tableId, takeawayId), loadFoods()])
  })
  const openKitchen = () => run(async () => { const { data } = await axiosClient.get('/Kitchen/pending-orders'); setKitchen(data) })
  const changeKitchen = order => run(async () => {
    await axiosClient.put(`/Kitchen/${order.id}/status`, { status: order.status === 'Pending' ? 'Cooking' : 'Completed' });
    const { data } = await axiosClient.get('/Kitchen/pending-orders'); setKitchen(data)
  })
  const checkout = () =>
    run(async () => {
      try {
        await axiosClient.post(`/Bill/checkout/${bill.idBill}`, { discount, guestCount, paymentMethod, idCustomer: bill.customer?.id ?? null, redeemPoints: Math.min(redeemPoints, maxRedeem), expectedTotal: total })
      } catch (e) {
        if (e.response?.data?.code === 'BILL_CHANGED') { setPaymentOpen(false); await loadBill(tableId, takeawayId) }
        throw e
      }
      setBill(null); setPaymentOpen(false); setView('tables')
      if (takeawayId) { setTakeawayId(null); navigate("/pos", { replace: true }) }
      await Promise.all([loadTables(), loadFoods()])
    }, 'Thanh toán thành công.')

  const shownFoods = useMemo(() => {
    const kw = keyword.trim().toLowerCase()
    return foods.filter(
      (f) =>
        f.isActive && !f.isTopping && (activeCat === null || f.idCategory === activeCat) &&
        (!kw || f.name.toLowerCase().includes(kw)),
    )
  }, [foods, activeCat, keyword])

  const subtotal = bill ? bill.items.reduce((s, i) => s + i.price * i.count, 0) : 0
  const afterDiscount = Math.round(subtotal * (100 - discount)) / 100
  const maxRedeem = Math.max(0, Math.min(bill?.customer?.points || 0, Math.floor(afterDiscount / 100)))
  const total = afterDiscount - Math.min(redeemPoints, maxRedeem) * 100
  const earnedPoints = bill?.customer ? Math.floor(total / 10000) : 0
  const table = tables.find((t) => t.id === tableId)

  const availableTables = tables.filter(t => t.isActive && t.areaActive !== false)
  const shownTables = availableTables.filter(t => (!area || t.areaName === area) && (!tableSearch || t.name.toLowerCase().includes(tableSearch.toLowerCase())) && (tableStatus === 'all' || (tableStatus === 'busy' ? t.status !== 'Trống' : t.status === 'Trống')))
  const newTakeaway = () => run(async () => {
    const r = await axiosClient.post('/Orders/takeaway', { requestKey: crypto.randomUUID(), label: '', note: '' })
    setTableId(null); setTakeawayId(r.data.idBill); setView('menu'); setMsg('')
    navigate(`/pos?bill=${r.data.idBill}`, { replace: true }); await loadBill(null, r.data.idBill)
  })
  const openPayment = async () => {
    if (!can('POS_CHECKOUT') || !bill?.items.length || busy) return
    try {
      const [r, latest] = await Promise.all([axiosClient.get('/Shift/current'), loadBill(tableId, takeawayId)]); setShift(r.data.shift)
      if (!latest?.items.length || !can('POS_CHECKOUT')) return
      if (!r.data.shift) { setMsg(can('SHIFT_SELF') ? 'Bạn chưa mở ca. Bấm Ca làm việc ở thanh trên để mở ca trước khi thanh toán.' : 'Bạn chưa có ca mở. Quản trị viên cần cấp quyền mở/chốt ca cá nhân để bạn bắt đầu ca.'); return }
      setMsg(''); setPaymentOpen(true)
    } catch { setMsg('Không kiểm tra được ca hiện tại. Vui lòng thử lại.') }
  }
  useEffect(() => {
    const handler = e => {
      if (e.key === 'F3') { e.preventDefault(); setView('menu'); searchRef.current?.focus() }
      if (e.key === 'F9' && can('POS_CHECKOUT')) { e.preventDefault(); if (!selection && !cancelItem && !transfer && kitchen === null) openPayment() }
      if (e.key === 'F10' && can('POS_SEND')) { e.preventDefault(); if (bill?.items.some(i => i.count > i.sentCount) && !busy && !selection && !cancelItem && !transfer && !paymentOpen && kitchen === null) void sendKitchen() }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  })

  return (
    <div className="pos">
      <header className="pos__bar">
        <div className="pos__nav" role="tablist" aria-label="Khu vực bán hàng">
          <button role="tab" aria-selected={view === 'tables'} className={view === 'tables' ? 'is-active' : ''} onClick={() => setView('tables')}><Armchair size={17}/> Phòng / Bàn</button>
          <button role="tab" aria-selected={view === 'menu'} className={view === 'menu' ? 'is-active' : ''} onClick={() => setView('menu')}><Utensils size={17}/> Thực đơn</button>
        </div>
        <label className="pos__search-wrap"><Search size={18}/><input ref={searchRef} className="pos__search" aria-label="Tìm món" placeholder="Tìm món (F3)" value={keyword} onChange={e => { setKeyword(e.target.value); setView('menu') }}/></label>
        <div className="pos__bar-right"><QrOrderInbox onProcessed={async id => { await Promise.all([loadTables(), loadFoods(), id === tableId ? loadBill(tableId, takeawayId) : Promise.resolve()]) }} /><span className="pos__user">{user.displayName || 'Thu ngân'}<small>{user.roleName === 'Admin' ? 'Quản trị viên' : user.roleName === 'Cashier' ? 'Thu ngân' : user.roleName}</small></span>
          {canOpen('/shifts', user) ? <button className="pos__shift-status" onClick={() => navigate('/shifts')}>{shift ? `CA${String(shift.id).padStart(6,'0')}` : 'Mở ca'} · Ca làm việc</button> : can('POS_CHECKOUT') && <span className="pos__shift-status">{shift ? `CA${String(shift.id).padStart(6,'0')}` : 'Chưa mở ca'}</span>}
          <button className="pos__menu-toggle" aria-label="Menu thu ngân" aria-expanded={menuOpen} onClick={() => setMenuOpen(v => !v)}><Menu size={21}/></button>
          {menuOpen && <><button className="pos__menu-dismiss" aria-label="Đóng menu thu ngân" onClick={() => setMenuOpen(false)}/><div className="pos__user-menu">
            {canOpen('/orders', user) && <button onClick={() => navigate('/orders')}>Đơn hàng</button>}
            {canOpen('/assistant', user) && <button onClick={() => navigate('/assistant')}>Trợ lý AI</button>}
            {canOpen('/shifts', user) && <button onClick={() => navigate('/shifts')}>Ca làm việc</button>}
            {canOpen('/kitchen', user) && <button onClick={() => navigate('/kitchen')}>Bếp / Bar</button>}
            {managementLanding(user) && <button onClick={() => navigate(managementLanding(user))}>Quản lý</button>}
            <button onClick={() => { localStorage.removeItem('token'); localStorage.removeItem('user'); navigate('/login') }}><LogOut size={16}/> Đăng xuất</button>
          </div></>}
        </div>
      </header>
      <div className="pos__body">
        <section className="pos__left">
          {view === 'tables' ? <>
            <div className="pos__room-tools"><select aria-label="Lọc khu vực bàn" value={area} onChange={e => setArea(e.target.value)}><option value="">Tất cả khu vực</option>{[...new Set(availableTables.map(t => t.areaName))].map(name => <option key={name}>{name}</option>)}</select><label><Search size={17}/><input aria-label="Tìm phòng bàn" placeholder="Tìm phòng / bàn..." value={tableSearch} onChange={e => setTableSearch(e.target.value)}/></label></div>
            <div className="pos__status-filters">{[['all',`Tất cả (${availableTables.length})`],['busy',`Đang dùng (${availableTables.filter(t => t.status !== 'Trống').length})`],['free',`Còn trống (${availableTables.filter(t => t.status === 'Trống').length})`]].map(([value,label]) => <button key={value} aria-pressed={tableStatus === value} className={tableStatus === value ? 'is-active' : ''} onClick={() => setTableStatus(value)}>{label}</button>)}</div>
            <div className="pos__room-grid"><button hidden={!can('POS_ORDER')} className="pos__room pos__room--takeaway" disabled={busy} onClick={newTakeaway}><ShoppingBag size={36}/><b>Mang về</b><small>+ Tạo đơn mới</small></button>{shownTables.map(t => <button key={t.id} className={`pos__room ${t.status !== 'Trống' ? 'is-busy' : ''} ${t.id === tableId ? 'is-selected' : ''}`} disabled={busy} onClick={() => pickTable(t.id)}><Armchair size={31}/><b>{t.name}</b><small>{t.areaName} · {t.status === 'Trống' ? `${t.seats || 0} chỗ` : `${t.guestCount || 1} khách`}</small><span>{t.status !== 'Trống' ? `${money(t.amount)} đ` : 'Còn trống'}</span></button>)}</div>
            {!shownTables.length && <p className="pos__catalog-empty">Không có bàn phù hợp bộ lọc.</p>}
          </> : <>
            <div className="pos__menu-heading"><span><Coffee size={18}/> Chọn món</span><button disabled={busy} onClick={() => setView('tables')}>{table?.name || (takeawayId ? 'Mang về' : 'Chọn bàn trước')}<ChevronDown size={15}/></button><button hidden={!can('POS_ORDER')} disabled={busy} onClick={newTakeaway}><ShoppingBag size={15}/> + Mang về</button></div>
            <div className="pos__cats"><button className={activeCat === null ? 'is-active' : ''} onClick={() => setActiveCat(null)}>Tất cả</button>{categories.map(c => <button key={c.id} className={activeCat === c.id ? 'is-active' : ''} onClick={() => setActiveCat(c.id)}>{c.name}</button>)}</div>
            {!tableId && !takeawayId && <p className="pos__choose-hint">Chọn Phòng / Bàn hoặc tạo đơn Mang về để thêm món.</p>}
            <div className="pos__foods">{shownFoods.map(f => <button key={f.id} className="pos__food" disabled={!can('POS_ORDER') || (!tableId && !takeawayId) || busy || f.availableQuantity === 0} onClick={() => pickFood(f)}>{f.imageUrl ? <img className="pos__food-image" src={f.imageUrl} alt={f.name}/> : <span className="pos__food-placeholder"><Coffee size={33}/></span>}<span className="pos__food-name">{f.name}</span><span className="pos__food-price">{money(f.variants?.some(v => v.isActive) ? Math.min(...f.variants.filter(v => v.isActive).map(v => v.price)) : f.price)} đ</span><small className={f.availableQuantity === 0 ? 'pos__stock-empty' : 'pos__stock'}>{f.availableQuantity == null ? 'Chưa giới hạn theo công thức' : f.availableQuantity === 0 ? 'Hết nguyên liệu' : `Còn nhận ${f.availableQuantity} phần`}</small></button>)}</div>
            {!shownFoods.length && <p className="pos__catalog-empty">Không có món phù hợp tìm kiếm.</p>}
          </>}
        </section>
        <aside className="pos__bill">
          <div className="pos__bill-head"><span>{takeawayId ? <ShoppingBag size={19}/> : <Armchair size={19}/>} {takeawayId ? bill?.tableName || 'Mang về' : table ? table.name : 'Chưa chọn bàn'}<small>{bill ? `HD${String(bill.idBill).padStart(6, '0')}` : 'Chọn bàn hoặc tạo đơn mang về'}</small></span><button hidden={!can('POS_ORDER')} disabled={busy} onClick={newTakeaway} title="Tạo đơn mang về mới">+ Đơn mới</button></div>
          <CustomerPicker customer={bill?.customer} disabled={!can('POS_ORDER') || !bill || busy} onSelect={async c => { await axiosClient.put(`/Bill/${bill.idBill}/customer`, { idCustomer: c?.id ?? null }); await loadBill(tableId, takeawayId); setRedeemPoints(0) }} />
          <ul className="pos__items">
            {bill?.items.map((i) => (
              <li key={i.idBillInfo}>
                <span className="pos__item-name">{i.foodName}<small>{i.optionLabel}</small><small>Đã báo: {i.sentCount} · Chưa báo: {i.count - i.sentCount}</small></span>
                <span className="pos__qty">
                  <button hidden={!can('POS_CANCEL')} disabled={busy} onClick={() => decrease(i)}>−</button>
                  <b>{i.count}</b>
                  <button hidden={!can('POS_ORDER')} disabled={busy} onClick={() => pickFood(foods.find(f => f.id === i.idFood))}>+</button>
                </span>
                <span className="pos__item-total">{money(i.price * i.count)}</span>
              </li>
            ))}
            {(tableId || takeawayId) && !bill?.items.length && <li className="pos__empty">Chưa có món. Bấm món bên trái để thêm.</li>}
          </ul>

          <div className="pos__bill-tools"><span>{bill?.items.reduce((sum,i) => sum + i.count, 0) || 0} món · {guestCount} khách</span><button hidden={!can('POS_TRANSFER')} disabled={!bill?.items.length || busy || !!takeawayId} onClick={() => setTransfer(true)}>Chuyển / gộp / tách</button>{bill && !bill.items.length && <button hidden={!can('POS_CANCEL')} disabled={busy} onClick={() => run(async () => { await axiosClient.post(`/Bill/${bill.idBill}/close-empty`); setBill(null); if (takeawayId) { setTakeawayId(null); navigate('/pos', {replace:true}) } await loadTables() }, 'Đã đóng đơn trống.')}>Đóng đơn trống</button>}</div>
          <div className="pos__bottom-total"><span>Tổng cần thanh toán<small>Giảm giá và đổi điểm tại bước thanh toán</small></span><b>{money(total)}<small>đ</small></b></div>
          {msg && <div className="pos__msg">{msg}</div>}
          <div className="pos__actions">
            <button hidden={!can('POS_SEND')} disabled={!bill?.items.some(i => i.count > i.sentCount) || busy} onClick={sendKitchen}><Bell size={19}/> Báo bếp <small>F10</small></button>
            <button hidden={!can('POS_CHECKOUT')} className="is-pay" disabled={!bill?.items.length || busy} onClick={openPayment}><CreditCard size={20}/> Thanh toán <small>F9</small></button>
          </div>
        </aside>
      </div>
      {paymentOpen && bill && <Modal width={550} title={`Thanh toán · ${table?.name || bill.tableName || 'Mang về'}`} onClose={() => { if (!busy) setPaymentOpen(false) }} footer={<><button disabled={busy} onClick={() => setPaymentOpen(false)}>Quay lại đơn</button><button hidden={!can('POS_CHECKOUT')} className="pos__confirm-pay" disabled={busy || !bill.items.length} onClick={checkout}>{busy ? 'Đang thanh toán...' : `Xác nhận thu ${money(total)} đ`}</button></>}>
        <p className="pos__payment-hint">Món chưa báo bếp sẽ tự gửi khi xác nhận thanh toán.</p>
        <p className="pos__payment-caption">{bill.customer?.name || 'Khách lẻ'} · HD{String(bill.idBill).padStart(6, '0')}</p>
          <div className="pos__sum">
            <div><span>Thanh toán bằng</span><select value={paymentMethod} onChange={e => setPaymentMethod(e.target.value)}><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select></div>
            <div><label htmlFor="guest-count">Số khách</label><input id="guest-count" type="number" min="1" max="1000" disabled={!can('POS_ORDER') || !bill || busy} value={guestCount} onChange={(e) => setGuestCount(Math.min(1000, Math.max(1, Math.trunc(Number(e.target.value)) || 1)))} onBlur={() => { if (bill && guestCount !== bill.guestCount) axiosClient.put(`/Bill/${bill.idBill}/guests`, { guestCount }).catch(() => setMsg("Không lưu được số khách. Vui lòng thử lại.")) }} /></div>
            <div><span>Tạm tính</span><span>{money(subtotal)}</span></div>
            <div>
              <span>Giảm giá (%)</span>
              <input
                type="number"
                min="0"
                max="100"
                disabled={!can('POS_DISCOUNT')}
                value={discount}
                onChange={(e) => setDiscount(Math.min(100, Math.max(0, Math.trunc(Number(e.target.value)) || 0)))}
              />
            </div>
            {bill?.customer && <><div><label htmlFor="redeem-points">Đổi điểm (tối đa {maxRedeem})</label><input id="redeem-points" type="number" min="0" max={maxRedeem} disabled={!can('POS_DISCOUNT') || busy} value={Math.min(redeemPoints, maxRedeem)} onChange={e => setRedeemPoints(Math.max(0, Math.min(maxRedeem, Math.trunc(Number(e.target.value)) || 0)))} /></div><div><span>Giảm từ điểm</span><span>{money(Math.min(redeemPoints, maxRedeem) * 100)} đ</span></div><p className="pos__loyalty-note">Tích thêm {earnedPoints} điểm sau thanh toán · 1 điểm = 100đ.{bill.customer.points < 0 ? ' Điểm âm được bù bằng lần mua này.' : ''}</p></>}
            <div className="pos__total"><span>Khách cần trả</span><span>{money(total)}</span></div>
          </div>

        {msg && <div className="pos__msg" role="alert">{msg}</div>}
      </Modal>}
      {transfer && bill && <TableTransferModal bill={bill} tables={tables} onClose={() => setTransfer(false)} onDone={async id => { setTransfer(false); setTableId(id); navigate(`/pos?table=${id}`, { replace: true }); await Promise.all([loadBill(id), loadTables()]); setMsg("Đã chuyển món và đồng bộ bàn/bếp.") }} />}
      {selection && <Modal width={520} title={selection.name} onClose={() => { if (!busy) setSelection(null) }} footer={<><button disabled={busy} onClick={() => setSelection(null)}>Đóng</button><button hidden={!can('POS_ORDER')} disabled={busy || selection.variants?.find(v => v.id === selectedVariant)?.availableQuantity === 0} onClick={addSelection}>Thêm vào đơn · {money((selection.variants?.find(v => v.id === selectedVariant)?.price ?? selection.price) + Object.entries(toppings).reduce((sum, [id, count]) => sum + (foods.find(f => f.id === Number(id))?.price || 0) * count, 0))} đ</button></>}>
        {msg && <p className="pos__msg">{msg}</p>}
        {selection.variants?.some(v => v.isActive) && <div className="pos__options"><b>Chọn size</b>{selection.variants.filter(v => v.isActive).map(v => <label key={v.id}><input type="radio" name="size" disabled={v.availableQuantity === 0} checked={selectedVariant === v.id} onChange={() => setSelectedVariant(v.id)} /><span>{v.name}<small>{v.availableQuantity == null ? '' : v.availableQuantity === 0 ? ' · Hết nguyên liệu' : ` · Còn nhận ${v.availableQuantity}`}</small></span><b>{money(v.price)} đ</b></label>)}</div>}
        <div className="pos__options">{foods.filter(f => selection.toppingIds?.includes(f.id) && f.isActive && f.isTopping).map(f => <label key={f.id}><span>{f.name} (+{money(f.price)} đ)</span><input aria-label={`Số phần ${f.name}`} type="number" min="0" max="20" value={toppings[f.id] || 0} onChange={e => setToppings(t => ({ ...t, [f.id]: Math.min(20, Math.max(0, Math.trunc(Number(e.target.value)) || 0)) }))} /></label>)}</div>
        {!selection.variants?.some(v => v.isActive) && !selection.toppingIds?.length && <p>Giá bán: {money(selection.price)} đ</p>}
      </Modal>}
      {cancelItem && <Modal title={`Hủy 1 phần ${cancelItem.foodName}`} onClose={() => setCancelItem(null)} footer={<><button onClick={() => setCancelItem(null)}>Đóng</button><button hidden={!can('POS_CANCEL')} disabled={busy || !cancelReason.trim()} onClick={cancel}>Xác nhận hủy</button></>}><p>Chờ chế biến: hoàn nguyên liệu. Đang làm hoặc đã hoàn thành: ghi nhận hao hụt.</p><textarea className="pos__reason" placeholder="Lý do hủy món..." maxLength={300} value={cancelReason} onChange={e => setCancelReason(e.target.value)} />{msg && <p className="pos__msg">{msg}</p>}</Modal>}
      {kitchen !== null && <Modal width={800} title="Bếp / Bar" onClose={() => setKitchen(null)} footer={<button disabled={busy} onClick={openKitchen}>Làm mới</button>}>
        {msg && <p className="pos__msg">{msg}</p>}{!kitchen.length && <p>Không có phiếu chờ chế biến.</p>}
        <div className="pos__kitchen-grid">{kitchen.map(order => <article key={order.id}><div><b>{order.tableName} · Phiếu #{order.id}</b><small>{new Date(order.createdAt).toLocaleTimeString('vi-VN')} · {order.status === 'Pending' ? 'Chờ làm' : 'Đang làm'}</small></div>{order.details.map(d => <p key={d.id}><b>{d.count} × {d.foodName}</b><small>{d.optionLabel}</small></p>)}<button hidden={!can('KITCHEN_UPDATE')} disabled={busy} onClick={() => changeKitchen(order)}>{order.status === 'Pending' ? 'Bắt đầu chế biến' : 'Hoàn thành'}</button></article>)}</div>
      </Modal>}    </div>
  )
}

export default POS
