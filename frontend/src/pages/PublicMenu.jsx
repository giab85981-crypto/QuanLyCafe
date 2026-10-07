import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import api from '../api/axiosClient'
import { money, errorText } from '../utils/warehouse'
import './TableManagement.css'
export default function PublicMenu() {
  const { tableId } = useParams(), [data, setData] = useState(null), [error, setError] = useState(''), [category, setCategory] = useState(''), [search, setSearch] = useState('')
  useEffect(() => { let active = true; api.get(`/PublicMenu/${tableId}`).then(r => { if (active) setData(r.data) }).catch(e => { if (active) setError(errorText(e)) }); return () => { active = false } }, [tableId])
  return <main className="public-menu"><header><span>☕ THỰC ĐƠN CỦA QUÁN</span><h1>Mời bạn chọn món</h1><p>{data?.tableName} {data?.areaName && `· ${data.areaName}`}</p><small>Vui lòng gọi nhân viên để đặt món.</small></header>{error && <p role="alert">{error}</p>}{!data && !error && <p>Đang tải thực đơn...</p>}{data && <><input aria-label="Tìm món" placeholder="Bạn muốn uống gì?" value={search} onChange={e => setSearch(e.target.value)}/><nav><button className={!category ? 'active' : ''} onClick={() => setCategory('')}>Tất cả</button>{[...new Set(data.foods.map(f => f.category))].map(c => <button className={category === c ? 'active' : ''} key={c} onClick={() => setCategory(c)}>{c}</button>)}</nav><section>{data.foods.filter(f => (!category || f.category === category) && f.name.toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi'))).map(f => <article key={f.id}>{f.imageUrl && <img src={f.imageUrl} alt={f.name}/>}<div><small>{f.category}</small><h2>{f.name}</h2><p>{f.description}</p>{f.variants.length ? f.variants.map((v, i) => <p key={i}>{v.name} <b>{money(v.price)}</b></p>) : <b>{money(f.price)}</b>}{f.toppings.length > 0 && <details><summary>Topping thêm</summary>{f.toppings.map((t, i) => <p key={i}>{t.name} +{money(t.price)}</p>)}</details>}</div></article>)}</section></>}</main>
}
