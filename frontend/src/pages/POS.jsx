import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import axiosClient from '../api/axiosClient'
import './POS.css'

const money = (n) => Number(n || 0).toLocaleString('vi-VN')

function POS() {
  const navigate = useNavigate()
  const user = JSON.parse(localStorage.getItem('user') || '{}')

  const [tables, setTables] = useState([])
  const [foods, setFoods] = useState([])
  const [categories, setCategories] = useState([])
  const [activeCat, setActiveCat] = useState(null)
  const [keyword, setKeyword] = useState('')
  const [tableId, setTableId] = useState(null)
  const [bill, setBill] = useState(null)
  const [discount, setDiscount] = useState(0)
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState('')

  const loadTables = useCallback(
    () => axiosClient.get('/TableFood').then((r) => setTables(r.data)),
    [],
  )

  const loadBill = useCallback((id) => {
    if (!id) return setBill(null)
    return axiosClient
      .get(`/Bill/table/${id}`)
      .then((r) => {
        setBill(r.data)
        setDiscount(r.data.discount || 0)
      })
      .catch(() => setBill(null))
  }, [])

  useEffect(() => {
    Promise.all([axiosClient.get('/Food'), axiosClient.get('/FoodCategory'), loadTables()])
      .then(([f, c]) => {
        setFoods(f.data)
        setCategories(c.data)
      })
      .catch(() => setMsg('Không kết nối được máy chủ.'))
  }, [loadTables])

  const pickTable = (id) => {
    setTableId(id)
    setMsg('')
    loadBill(id)
  }

  const run = async (fn, okMsg) => {
    setBusy(true)
    try {
      await fn()
      if (okMsg) setMsg(okMsg)
    } catch (e) {
      setMsg(typeof e.response?.data === 'string' ? e.response.data : 'Thao tác thất bại.')
    } finally {
      setBusy(false)
    }
  }

  const changeItem = (idFood, count) =>
    run(async () => {
      await axiosClient.post('/Bill/add-item', { idTable: tableId, idFood, count })
      await Promise.all([loadBill(tableId), loadTables()])
    })

  const sendKitchen = () =>
    run(
      () =>
        axiosClient.post('/Kitchen/send-order', {
          idBill: bill.idBill,
          items: bill.items.map((i) => ({ idFood: i.idFood, count: i.count })),
        }),
      'Đã báo bếp.',
    )

  const checkout = () =>
    run(async () => {
      await axiosClient.post(`/Bill/checkout/${bill.idBill}`, { discount, idCustomer: null })
      setBill(null)
      await loadTables()
    }, 'Thanh toán thành công.')

  const shownFoods = useMemo(() => {
    const kw = keyword.trim().toLowerCase()
    return foods.filter(
      (f) =>
        (activeCat === null || f.idCategory === activeCat) &&
        (!kw || f.name.toLowerCase().includes(kw)),
    )
  }, [foods, activeCat, keyword])

  const subtotal = bill ? bill.items.reduce((s, i) => s + i.price * i.count, 0) : 0
  const total = (subtotal * (100 - discount)) / 100
  const table = tables.find((t) => t.id === tableId)

  return (
    <div className="pos">
      <header className="pos__bar">
        <input
          className="pos__search"
          placeholder="Tìm món (F3)"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
        />
        <div className="pos__bar-right">
          <span>{user.displayName || 'Thu ngân'}</span>
          <button onClick={() => navigate('/dashboard')}>Quản lý</button>
        </div>
      </header>

      <div className="pos__body">
        <section className="pos__left">
          <div className="pos__tables">
            {tables.map((t) => (
              <button
                key={t.id}
                className={`pos__table ${t.id === tableId ? 'is-active' : ''} ${t.status !== 'Trống' ? 'is-busy' : ''}`}
                onClick={() => pickTable(t.id)}
              >
                {t.name}
              </button>
            ))}
          </div>

          <div className="pos__cats">
            <button className={activeCat === null ? 'is-active' : ''} onClick={() => setActiveCat(null)}>
              Tất cả
            </button>
            {categories.map((c) => (
              <button key={c.id} className={activeCat === c.id ? 'is-active' : ''} onClick={() => setActiveCat(c.id)}>
                {c.name}
              </button>
            ))}
          </div>

          <div className="pos__foods">
            {shownFoods.map((f) => (
              <button key={f.id} className="pos__food" disabled={!tableId || busy} onClick={() => changeItem(f.id, 1)}>
                <span className="pos__food-name">{f.name}</span>
                <span className="pos__food-price">{money(f.price)}</span>
              </button>
            ))}
          </div>
        </section>

        <aside className="pos__bill">
          <div className="pos__bill-head">{table ? table.name : 'Chọn bàn để bắt đầu'}</div>

          <ul className="pos__items">
            {bill?.items.map((i) => (
              <li key={i.idFood}>
                <span className="pos__item-name">{i.foodName}</span>
                <span className="pos__qty">
                  <button disabled={busy} onClick={() => changeItem(i.idFood, -1)}>−</button>
                  <b>{i.count}</b>
                  <button disabled={busy} onClick={() => changeItem(i.idFood, 1)}>+</button>
                </span>
                <span className="pos__item-total">{money(i.price * i.count)}</span>
              </li>
            ))}
            {tableId && !bill?.items.length && <li className="pos__empty">Chưa có món. Bấm món bên trái để thêm.</li>}
          </ul>

          <div className="pos__sum">
            <div><span>Tạm tính</span><span>{money(subtotal)}</span></div>
            <div>
              <span>Giảm giá (%)</span>
              <input
                type="number"
                min="0"
                max="100"
                value={discount}
                onChange={(e) => setDiscount(Math.min(100, Math.max(0, Number(e.target.value) || 0)))}
              />
            </div>
            <div className="pos__total"><span>Khách cần trả</span><span>{money(total)}</span></div>
          </div>

          {msg && <div className="pos__msg">{msg}</div>}

          <div className="pos__actions">
            <button disabled={!bill || busy} onClick={sendKitchen}>Báo bếp</button>
            <button className="is-pay" disabled={!bill?.items.length || busy} onClick={checkout}>Thanh toán</button>
          </div>
        </aside>
      </div>
    </div>
  )
}

export default POS
