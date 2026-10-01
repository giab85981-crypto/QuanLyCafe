import { useEffect, useMemo, useState } from 'react'
import axiosClient from '../api/axiosClient'
import './Menu.css'

const money = (n) => Number(n || 0).toLocaleString('vi-VN')

function Menu() {
  const [foods, setFoods] = useState([])
  const [categories, setCategories] = useState([])
  const [activeCat, setActiveCat] = useState(null)
  const [keyword, setKeyword] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([axiosClient.get('/Food'), axiosClient.get('/FoodCategory')])
      .then(([f, c]) => {
        setFoods(f.data)
        setCategories(c.data)
      })
      .catch(() => setError('Không tải được thực đơn. Kiểm tra backend đã chạy chưa.'))
      .finally(() => setLoading(false))
  }, [])

  const rows = useMemo(() => {
    const kw = keyword.trim().toLowerCase()
    return foods.filter(
      (f) =>
        (activeCat === null || f.idCategory === activeCat) &&
        (!kw || f.name.toLowerCase().includes(kw)),
    )
  }, [foods, activeCat, keyword])

  return (
    <div className="menu-page">
      <aside className="menu-side">
        <h3>Nhóm hàng</h3>
        <ul>
          <li>
            <button className={activeCat === null ? 'is-active' : ''} onClick={() => setActiveCat(null)}>
              Tất cả
            </button>
          </li>
          {categories.map((c) => (
            <li key={c.id}>
              <button className={activeCat === c.id ? 'is-active' : ''} onClick={() => setActiveCat(c.id)}>
                {c.name}
              </button>
            </li>
          ))}
        </ul>
      </aside>

      <section className="menu-main">
        <div className="menu-bar">
          <h1>Thực đơn</h1>
          <input
            className="menu-search"
            placeholder="Theo tên món"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
          />
        </div>

        {error && <div className="menu-error">{error}</div>}

        <table className="menu-table">
          <thead>
            <tr>
              <th>Mã</th>
              <th>Tên món</th>
              <th>Nhóm hàng</th>
              <th className="num">Giá bán</th>
              <th className="num">Giá vốn</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan="5" className="menu-empty">Đang tải...</td></tr>
            )}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan="5" className="menu-empty">Không có món nào phù hợp</td></tr>
            )}
            {rows.map((f) => (
              <tr key={f.id}>
                <td>MN{String(f.id).padStart(4, '0')}</td>
                <td>{f.name}</td>
                <td>{f.categoryName}</td>
                <td className="num">{money(f.price)}</td>
                <td className="num">{money(f.costPrice)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  )
}

export default Menu
