import { errMsg } from '../api/errMsg'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Coffee, Eye, EyeOff, LayoutDashboard, ShoppingCart, ShieldCheck } from 'lucide-react'
import authApi from '../api/authApi'
import { canOpen, landing } from '../utils/staffAccess'
import './Login.css'
export default function Login() {
  const [form, setForm] = useState({userName:'', passWord:''})
  const [show, setShow] = useState(false), [error, setError] = useState(''), [loading, setLoading] = useState(false)
  const busy = useRef(false)
  const navigate = useNavigate()
  const login = async target => {
    if (busy.current) return
    if (!form.userName.trim() || !form.passWord) { setError('Vui lòng nhập tên đăng nhập và mật khẩu.'); return }
    busy.current = true; setLoading(true); setError('')
    try {
      const {data} = await authApi.login({...form,userName:form.userName.trim()})
      const {token,userName,displayName,roleName,permissions} = data
      const user = {userName,displayName,roleName,permissions}
      localStorage.setItem('token',token); localStorage.setItem('user',JSON.stringify(user))
      navigate(canOpen(target,user) ? target : landing(user), {replace:true})
    } catch (e) { setError(e.response?.status === 401 ? 'Tên đăng nhập hoặc mật khẩu chưa đúng.' : errMsg(e)) }
    finally { busy.current = false; setLoading(false) }
  }
  return <main className="login-page"><div className="login-shell"><section className="login-story"><div className="login-wordmark"><Coffee size={26}/> Quán Cà Phê</div><div className="login-story-body"><span className="login-kicker">MỖI NGÀY, MỘT KHỞI ĐẦU TỐT</span><h1>Quản lý gọn gàng.<br/>Phục vụ tận tâm.</h1><p>Từ ly cà phê đầu tiên đến ca làm cuối ngày — mọi hoạt động của quán trong một nơi.</p><div className="login-story-tags"><span>Thu ngân</span><span>Bếp / Bar</span><span>Quản lý quán</span></div></div><small>Chúc bạn một ca làm việc hiệu quả.</small></section><section className="login-card"><div className="login-logo"><Coffee size={28}/></div><h2>Chào mừng trở lại</h2><p>Đăng nhập để bắt đầu ca làm việc.</p><form onSubmit={e=>{e.preventDefault();login('/dashboard')}} aria-busy={loading} noValidate>{error&&<div className="login-error" role="alert">{error}</div>}<label htmlFor="login-user">Tên đăng nhập</label><input id="login-user" autoComplete="username" autoCapitalize="none" spellCheck={false} value={form.userName} disabled={loading} onChange={e=>setForm({...form,userName:e.target.value})} placeholder="Nhập tên đăng nhập" required/><label htmlFor="login-password">Mật khẩu</label><div className="login-password"><input id="login-password" type={show?'text':'password'} autoComplete="current-password" value={form.passWord} disabled={loading} onChange={e=>setForm({...form,passWord:e.target.value})} placeholder="Nhập mật khẩu" required/><button type="button" onClick={()=>setShow(v=>!v)} aria-label={show?'Ẩn mật khẩu':'Hiện mật khẩu'} aria-pressed={show}>{show?<EyeOff size={19}/>:<Eye size={19}/>}</button></div><div className="login-actions"><button type="submit" disabled={loading}><LayoutDashboard size={18}/>{loading?'Đang đăng nhập…':'Quản lý'}</button><button type="button" disabled={loading} onClick={()=>login('/pos')}><ShoppingCart size={18}/>Bán hàng</button></div><p className="login-account-help">Quên mật khẩu? Liên hệ quản trị viên để đặt lại mật khẩu tài khoản.</p></form><div className="login-security"><ShieldCheck size={16}/><span>Màn hình làm việc được mở theo quyền của bạn.</span></div></section></div></main>
}
