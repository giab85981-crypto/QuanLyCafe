import { useEffect, useRef, useState } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import { ShoppingBag, Plus, Minus, Coffee, Send } from 'lucide-react'
import Modal from '../components/Modal'
import PageState from '../components/PageState'
import { errMsg } from '../api/errMsg'
import { money } from '../utils/warehouse'
import './TableManagement.css'
import './QrOrdering.css'
const api = axios.create({baseURL: import.meta.env.VITE_API_URL || '/api', timeout: 15000})
const labels = {Pending:'Đang chờ thu ngân xác nhận',Accepted:'Thu ngân đã xác nhận',Rejected:'Yêu cầu chưa được nhận',Expired:'Yêu cầu đã hết hạn'}
export default function PublicMenu() {
  const {tableId} = useParams()
  // Remount the session when changing the table so a basket never moves silently.
  return <GuestMenu key={tableId} tableId={tableId}/>
}
function GuestMenu({tableId}) {
  const storage = `cafe.qr.request.${tableId}`
  const [data,setData]=useState(null), [error,setError]=useState(''), [reload,setReload]=useState(0), [search,setSearch]=useState(''), [category,setCategory]=useState('')
  const [cart,setCart]=useState([]), [note,setNote]=useState(''), [choice,setChoice]=useState(null), [variant,setVariant]=useState(''), [toppings,setToppings]=useState({}), [count,setCount]=useState(1)
  const [attempt,setAttempt]=useState(()=>{try{return JSON.parse(localStorage.getItem(storage))}catch{return null}}), [request,setRequest]=useState(null), [sending,setSending]=useState(false), [sendError,setSendError]=useState(''), [trackError,setTrackError]=useState('')
  const inFlight=useRef(false)
  const requestStatus=request?.status
  useEffect(()=>{let active=true; api.get(`/PublicMenu/${tableId}`).then(r=>{if(active){setData(r.data);setError('')}}).catch(e=>{if(active)setError(errMsg(e))}); return()=>{active=false}},[tableId,reload])
  useEffect(()=>{
    if(!attempt || (requestStatus && requestStatus !== 'Pending')) return
    let active=true
    const read=()=>api.get(`/PublicMenu/${tableId}/requests/${attempt.requestKey}`).then(r=>{if(active){setRequest(r.data);setCart([]);setTrackError('')}}).catch(e=>{if(active && e.response?.status!==404)setTrackError(errMsg(e))})
    read(); const timer=setInterval(()=>{if(document.visibilityState==='visible')read()},5000)
    return()=>{active=false;clearInterval(timer)}
  },[tableId,attempt,requestStatus])
  const pick=food=>{setChoice(food);setVariant(food.variants[0]?.id || '');setToppings({});setCount(1)}
  const unit=choice ? (choice.variants.find(v=>v.id===Number(variant))?.price ?? choice.price)+choice.toppings.reduce((s,t)=>s+t.price*(toppings[t.id]||0),0) : 0
  const add=()=>{
    const selected=choice.toppings.filter(t=>toppings[t.id]>0).map(t=>({idFood:t.id,count:toppings[t.id]}))
    const options=[choice.variants.find(v=>v.id===Number(variant))?.name,...choice.toppings.filter(t=>toppings[t.id]>0).map(t=>`${t.name} ×${toppings[t.id]}`)].filter(Boolean).join(' · ')
    setCart(rows=>[...rows,{key:crypto.randomUUID(),idFood:choice.id,idVariant:Number(variant)||null,toppings:selected,count,name:choice.name,options,price:unit}]);setChoice(null)
  }
  const total=cart.reduce((s,i)=>s+i.price*i.count,0)
  const submit=async()=>{
    if(inFlight.current || request || (!attempt && !cart.length))return
    inFlight.current=true;setSending(true);setSendError('')
    const body=attempt || {requestKey:crypto.randomUUID(),items:cart.map(({idFood,idVariant,toppings,count})=>({idFood,idVariant,toppings,count})),note,expectedTotal:total}
    try {
      localStorage.setItem(storage,JSON.stringify(body));setAttempt(body)
      const r=await api.post(`/PublicMenu/${tableId}/requests`,body);setRequest(r.data);setCart([]);setNote('')
    }catch(e){setSendError(errMsg(e));if(e.response && e.response.status<500 && e.response.status!==429){localStorage.removeItem(storage);setAttempt(null)}}
    finally{inFlight.current=false;setSending(false)}
  }
  const next=()=>{localStorage.removeItem(storage);setAttempt(null);setRequest(null);setTrackError('');setSendError('');setCart([])}
  const foods=(data?.foods||[]).filter(f=>(!category||f.category===category)&&f.name.toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi')))
  return <main className="public-menu qr-guest"><header><span>☕ GỌI MÓN TẠI BÀN</span><h1>{data?.tableName || 'Mời bạn chọn món'}</h1><p>{data?.areaName}</p><small>Chọn món và gửi yêu cầu. Thu ngân xác nhận trước khi quán chuẩn bị món.</small></header>
    {error && <PageState error={error} onRetry={()=>{setError('');setData(null);setReload(v=>v+1)}}/>}{!data&&!error&&<PageState loading title="Đang tải thực đơn…"/>}
    {request && <div className={`qr-tracking status-${request.status}`} role="status"><b>Yêu cầu #{request.id} · {labels[request.status]}</b><p>{request.status==='Accepted'?'Món đã được thêm vào đơn của bàn. Nhân viên sẽ báo bếp; vui lòng gọi nhân viên nếu cần thay đổi.':request.status==='Pending'?'Chưa giữ nguyên liệu cho đến khi thu ngân xác nhận. Vui lòng chờ, không gửi lại yêu cầu này.':request.reason||'Vui lòng chọn món và gửi yêu cầu mới hoặc gọi nhân viên.'}</p><ul>{request.items.map((i,n)=><li key={n}>{i.count} × {i.name} {i.options&&`· ${i.options}`}</li>)}</ul><span>Tạm tính: {money(request.total)}</span>{request.status!=='Pending'&&<button onClick={next}>Chọn thêm / gửi yêu cầu mới</button>}</div>}
    {trackError&&<p className="qr-error" role="alert">Chưa cập nhật trạng thái: {trackError}</p>}
    {data && <div className={`qr-layout${request ? " has-request" : ""}`}><div><input className="qr-search" aria-label="Tìm món" placeholder="Bạn muốn uống gì?" value={search} onChange={e=>setSearch(e.target.value)}/><nav><button className={!category?'active':''} onClick={()=>setCategory('')}>Tất cả</button>{[...new Set(data.foods.map(f=>f.category))].map(c=><button className={category===c?'active':''} key={c} onClick={()=>setCategory(c)}>{c}</button>)}</nav>
      {!foods.length&&<PageState title="Chưa có món phù hợp"/>}<section>{foods.map(f=><article key={f.id}>{f.imageUrl?<img src={f.imageUrl} alt={f.name}/>:<div className="qr-food-icon"><Coffee size={36}/></div>}<div><small>{f.category}</small><h2>{f.name}</h2><p>{f.description}</p><b>{f.variants.length?'Từ ':''}{money(f.variants.length?Math.min(...f.variants.map(v=>v.price)):f.price)}</b><button className="qr-primary" disabled={!!attempt||sending} onClick={()=>pick(f)}><Plus size={16}/>Chọn món</button></div></article>)}</section></div>
      {!request&&<aside className="qr-cart"><h2><ShoppingBag size={20}/> Món đã chọn</h2>{!cart.length?<p>Chọn món ở thực đơn để thêm vào giỏ.</p>:cart.map(i=><div className="qr-cart-line" key={i.key}><b>{i.name}</b><small>{i.options}</small><span>{money(i.price)}</span><div><button aria-label={`Giảm ${i.name}`} disabled={!!attempt} onClick={()=>setCart(rows=>rows.flatMap(r=>r.key!==i.key?[r]:r.count>1?[{...r,count:r.count-1}]:[]))}><Minus size={14}/></button><span>{i.count}</span><button aria-label={`Tăng ${i.name}`} disabled={!!attempt||i.count>=20} onClick={()=>setCart(rows=>rows.map(r=>r.key===i.key?{...r,count:r.count+1}:r))}><Plus size={14}/></button></div></div>)}
      <label>Ghi chú cho quán<textarea disabled={!!attempt} maxLength={300} placeholder="Ví dụ: ít đường, ít đá…" value={note} onChange={e=>setNote(e.target.value)}/></label><div className="qr-total">Tạm tính <b>{money(attempt?.expectedTotal ?? total)}</b></div><p>Giá tạm tính chưa gồm giảm giá. Quán có thể từ chối nếu hết nguyên liệu.</p>{sendError&&<p className="qr-error" role="alert">{sendError}</p>}<button className="qr-primary" disabled={sending||!!request||(!attempt&&!cart.length)||cart.length>30} onClick={submit}><Send size={17}/>{sending?'Đang gửi…':attempt&&!request?'Gửi lại yêu cầu đang chờ':'Gửi yêu cầu gọi món'}</button>{attempt&&!request&&<p>Đang kiểm tra yêu cầu đã gửi. Gửi lại dùng cùng mã, không tạo đơn trùng.</p>}</aside>}
    </div>}
    {choice&&<Modal title={`Chọn ${choice.name}`} onClose={()=>setChoice(null)} footer={<button className="qr-primary" onClick={add} disabled={cart.length>=30}>Thêm {count} phần · {money(unit*count)}</button>}><div className="qr-choice">{choice.variants.length>0&&<label>Size<select aria-label="Size" value={variant} onChange={e=>setVariant(e.target.value)}>{choice.variants.map(v=><option value={v.id} key={v.id}>{v.name} · {money(v.price)}</option>)}</select></label>}{choice.toppings.map(t=><label key={t.id}>{t.name} · +{money(t.price)}<input aria-label={`Topping ${t.name}`} type="number" min="0" max="20" value={toppings[t.id]||0} onChange={e=>setToppings(old=>({...old,[t.id]:Math.min(20,Math.max(0,Math.trunc(Number(e.target.value))))}))}/></label>)}<label>Số lượng<input aria-label="Số lượng" type="number" min="1" max="20" value={count} onChange={e=>setCount(Math.min(20,Math.max(1,Math.trunc(Number(e.target.value)))))}/></label></div></Modal>}
  </main>
}
