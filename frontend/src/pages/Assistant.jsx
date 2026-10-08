import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { Bot, Send, Sparkles, RefreshCw, ShieldCheck, TrendingUp, Package, ReceiptText, Coffee } from 'lucide-react'
import api from '../api/axiosClient'
import { errMsg } from '../api/errMsg'
import { canOpen, useAccess } from '../utils/staffAccess'
import PageState from '../components/PageState'
import './Assistant.css'
const suggestions=[{topic:'overview',icon:Sparkles,text:'Tình hình quán hôm nay thế nào?'},{topic:'sales',icon:TrendingUp,text:'So sánh doanh thu 7 ngày qua với kỳ liền trước'},{topic:'orders',icon:ReceiptText,text:'Hôm nay có bao nhiêu đơn đã thanh toán?'},{topic:'menu',icon:Coffee,text:'Món nào bán chạy trong tháng này?'},{topic:'stock',icon:Package,text:'Nguyên liệu nào sắp hết?'}]
export default function Assistant() {
  const user=useAccess()
  // Conversation data must not survive an account or permission change.
  return <AssistantChat key={JSON.stringify([user.userName,user.roleName,user.permissions])} user={user}/>
}
function AssistantChat({user}) {
  const [caps,setCaps]=useState(null),[loadError,setLoadError]=useState(''),[reload,setReload]=useState(0),[question,setQuestion]=useState(''),[messages,setMessages]=useState([]),[busy,setBusy]=useState(false),[sendError,setSendError]=useState('')
  const pending=useRef(null), feed=useRef(null), input=useRef(null)
  useEffect(()=>{const controller=new AbortController(); api.get('/Assistant/capabilities',{signal:controller.signal}).then(r=>setCaps(r.data)).catch(e=>{if(!controller.signal.aborted)setLoadError(errMsg(e))});return()=>controller.abort()},[reload])
  useEffect(()=>()=>{pending.current?.abort()},[])
  useEffect(()=>{feed.current?.scrollIntoView({behavior:'smooth',block:'end'})},[messages,busy])
  async function ask(text=question) {
    if(pending.current || text.trim().length<3 || !caps)return
    const controller=new AbortController();pending.current=controller;setBusy(true);setSendError('')
    const value=text.trim();setQuestion('')
    try {
      const {data}=await api.post('/Assistant/ask',{question:value},{signal:controller.signal,timeout:55000})
      if(!controller.signal.aborted)setMessages(rows=>[...rows.slice(-19),{question:value,result:data}])
    } catch(e){if(!controller.signal.aborted){setSendError(errMsg(e));setQuestion(value)}}
    finally{if(!controller.signal.aborted){pending.current=null;setBusy(false);input.current?.focus()}}
  }
  return <main className="ai-page"><header className="ai-heading"><span className="ai-brand"><Bot size={26}/></span><div><span className="ai-eyebrow">HIỂU QUÁN TỪ DỮ LIỆU</span><h1>Trợ lý quán</h1><p>Hỏi doanh thu, đơn hàng, món bán chạy và nguyên liệu sắp hết.</p></div><button className="ai-reset" disabled={busy||!messages.length} onClick={()=>{setMessages([]);setSendError('');input.current?.focus()}}><RefreshCw size={16}/>Cuộc trò chuyện mới</button></header>
    {!caps&&<PageState loading={!loadError} error={loadError} onRetry={()=>{setLoadError('');setReload(n=>n+1)}}/>}
    {caps&&<div className="ai-layout"><aside className="ai-sidebar"><h2><Sparkles size={17}/>Bạn có thể hỏi</h2>{suggestions.filter(s=>caps.topics.includes(s.topic)).map(s=><button key={s.topic} disabled={busy} onClick={()=>ask(s.text)}><s.icon size={18}/><span>{s.text}</span></button>)}{!caps.topics.length&&<p>Bạn có quyền mở trợ lý nhưng chưa có quyền xem doanh thu, đơn hàng hoặc kho. Liên hệ quản trị viên.</p>}<div className="ai-side-note"><ShieldCheck size={18}/><p>Chỉ xem dữ liệu theo quyền của bạn. Trợ lý không sửa đơn hàng hoặc kho.</p></div><small>Thời gian: hôm nay, hôm qua, 7/30 ngày qua, tuần này, tháng này. Mỗi câu hỏi nêu rõ khoảng thời gian.</small></aside><section className="ai-chat" aria-label="Trò chuyện với trợ lý quán"><div className={`ai-connection ${caps.configured?'is-connected':''}`}><span/><b>{caps.configured?'Đã cấu hình Gemini':'Tra cứu cơ bản'}</b><small>{caps.configured?'Nhận xét bằng AI · số liệu từ quán':'Chưa có mã Gemini · câu hỏi gợi ý vẫn hoạt động'}</small></div><div className="ai-feed" role="log" aria-live="polite" aria-relevant="additions text">{!messages.length&&!busy&&<div className="ai-welcome"><Bot size={42}/><h2>Hôm nay bạn muốn biết gì?</h2><p>Thử chọn câu hỏi bên trái hoặc nhập câu hỏi của bạn.</p><small>Số liệu hiển thị được tính trực tiếp từ phần mềm.</small></div>}{messages.map((message,index)=><article className="ai-exchange" key={index}><div className="ai-question">{message.question}</div><div className="ai-answer"><div className="ai-answer-heading"><Bot size={18}/><b>Trợ lý quán</b><span>{message.result.mode==='gemini'?'Gemini':'Tra cứu cơ bản'}</span></div><p>{message.result.answer}</p>{message.result.warning&&<div className="ai-warning">{message.result.warning}</div>}{message.result.sections.map((s,n)=><section className="ai-section" key={n}><h3>{s.title}</h3><div className="ai-cards">{s.cards.map(c=><div key={c.label}><small>{c.label}</small><strong>{c.value}</strong></div>)}</div>{s.rows.length>0&&<ul className="ai-rows">{s.rows.map((row,i)=><li key={i}><div><b>{row.name}</b><small>{row.detail}</small></div><strong>{row.value}</strong></li>)}</ul>}<p className="ai-method">{s.note}</p><div className="ai-source">Nguồn: {s.source}{canOpen(s.path,user)&&<Link to={s.path}>Mở trang dữ liệu →</Link>}</div></section>)}{message.result.insight&&<div className="ai-insight"><b><Sparkles size={15}/>Nhận xét của AI</b><p>{message.result.insight}</p><small>Gợi ý tham khảo; đối chiếu số liệu ở trên khi quyết định.</small></div>}<small className="ai-timestamp">Cập nhật {new Date(message.result.snapshotAt).toLocaleString('vi-VN',{timeZone:'Asia/Ho_Chi_Minh'})}</small></div></article>)}{busy&&<div className="ai-thinking" role="status"><Bot size={20}/>Đang kiểm tra dữ liệu…</div>}<div ref={feed}/></div>{sendError&&<div className="ai-send-error" role="alert">{sendError}</div>}<form className="ai-compose" onSubmit={e=>{e.preventDefault();ask()}}><label htmlFor="ai-question" className="ai-sr">Câu hỏi cho trợ lý quán</label><textarea ref={input} id="ai-question" placeholder="Ví dụ: Doanh thu hôm nay thế nào?" maxLength={500} disabled={busy||!caps.topics.length} value={question} onChange={e=>setQuestion(e.target.value)} onKeyDown={e=>{if(e.key==='Enter'&&!e.shiftKey&&!e.nativeEvent.isComposing){e.preventDefault();ask()}}}/><button type="submit" disabled={busy||question.trim().length<3||!caps.topics.length} aria-label="Gửi câu hỏi"><Send size={19}/></button><div className="ai-compose-hint"><span>Enter để gửi · Shift + Enter xuống dòng</span><span>{question.length}/500</span></div></form></section></div>}
  </main>
}
