import {useCallback,useEffect,useRef,useState} from 'react'
import {Bell, Check, X} from 'lucide-react'
import api from '../api/axiosClient'
import {errMsg} from '../api/errMsg'
import {can,useAccess} from '../utils/staffAccess'
import {money} from '../utils/warehouse'
import Modal from './Modal'
import PageState from './PageState'
import '../pages/QrOrdering.css'
export default function QrOrderInbox({onProcessed}) {
 const user=useAccess(), allowed=can('POS_ORDER')
 const [rows,setRows]=useState([]),[open,setOpen]=useState(false),[loading,setLoading]=useState(false),[error,setError]=useState(''),[notice,setNotice]=useState(''),[busy,setBusy]=useState(null),[reject,setReject]=useState(null),[reason,setReason]=useState('')
 const version=useRef(0),mutating=useRef(false)
 const load=useCallback(async()=>{if(!can('POS_ORDER'))return;const id=++version.current;setLoading(true);try{const r=await api.get('/QrOrders');if(id===version.current){setRows(r.data);setError('')}}catch(e){if(id===version.current)setError(errMsg(e))}finally{if(id===version.current)setLoading(false)}},[])
 useEffect(()=>{if(!allowed){setRows([]);setOpen(false);setReject(null);return}void load();const timer=setInterval(()=>{if(document.visibilityState==='visible'&&!mutating.current)void load()},5000);return()=>{version.current++;clearInterval(timer)}},[allowed,load,user.userName])
 const decide=async(row,accept)=>{
  if(mutating.current||!can('POS_ORDER'))return
  mutating.current=true;setBusy(row.id);setError('');setNotice('')
  try {const r=await api.post(`/QrOrders/${row.id}/decision`,{accept,reason:accept?'':reason});setReject(null);setReason('');setNotice(`${row.tableName} · Yêu cầu #${row.id}: ${accept?'đã thêm món vào đơn, chờ báo bếp':'đã từ chối và thông báo cho khách'}.`);await load();await onProcessed(r.data.idTable)}catch(e){setError(errMsg(e))}finally{mutating.current=false;setBusy(null)}
 }
 if(!allowed)return null
 return <><button className={`pos__qr-button${rows.length?' has-pending':''}`} aria-label={`Yêu cầu QR: ${rows.length} đang chờ`} onClick={()=>{setNotice('');setOpen(true);void load()}}><Bell size={17}/>QR <b>{rows.length}</b>{error&&<span title="Chưa cập nhật được yêu cầu QR">!</span>}</button>
 {open&&<Modal title={`Yêu cầu gọi món QR · ${rows.length} đang chờ`} width={780} onClose={()=>{if(!mutating.current)setOpen(false)}} footer={<><button disabled={loading||busy!==null} onClick={load}>Làm mới</button><button disabled={busy!==null} onClick={()=>setOpen(false)}>Đóng</button></>}>
 <p className="qr-inbox-help">Đối chiếu đúng bàn trước khi xác nhận. Món được giữ nguyên liệu và thêm vào đơn; nhân viên báo bếp như thường lệ.</p>
 {error&&<p className="qr-error" role="alert">{error}</p>}{notice&&<p className="qr-success" role="status">{notice}</p>}
 {loading&&!rows.length?<PageState loading/>:!rows.length&&!error?<PageState title="Chưa có yêu cầu chờ duyệt" description="Yêu cầu khách gửi qua QR sẽ tự xuất hiện tại đây."/>:rows.map(row=><article className="qr-inbox-card" key={row.id}><div><h3>{row.tableName} <small>#{row.id}</small></h3><small>Gửi lúc {new Date(row.createdAt.endsWith('Z') ? row.createdAt : row.createdAt+'Z').toLocaleTimeString('vi-VN')} · Hết hạn sau 30 phút</small></div><ul>{row.items.map((i,n)=><li key={n}><span><b>{i.count} × {i.name}</b><small>{i.options}</small></span><b>{money(i.price*i.count)}</b></li>)}</ul>{row.note&&<p>Ghi chú: {row.note}</p>}<div className="qr-inbox-actions"><b>Tạm tính {money(row.total)}</b><button disabled={busy!==null||loading} onClick={()=>{setReject(row);setReason('');setError('')}}><X size={15}/>Từ chối</button><button className="qr-primary" disabled={busy!==null||loading} onClick={()=>decide(row,true)}><Check size={16}/>{busy===row.id?'Đang xử lý…':'Xác nhận & thêm món'}</button></div></article>)}
 </Modal>}
 {reject&&<Modal title={`Từ chối yêu cầu #${reject.id} · ${reject.tableName}`} onClose={()=>{if(!mutating.current)setReject(null)}} footer={<button className="qr-primary" disabled={busy!==null||!reason.trim()} onClick={()=>decide(reject,false)}>Xác nhận từ chối</button>}><label className="qr-choice">Lý do cho khách<textarea aria-label="Lý do từ chối" maxLength={300} placeholder="Ví dụ: Món hết nguyên liệu, vui lòng chọn món khác." value={reason} onChange={e=>setReason(e.target.value)}/></label>{error&&<p className="qr-error" role="alert">{error}</p>}</Modal>}
 </>
}
