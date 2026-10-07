import { useEffect } from 'react'
import api from '../api/axiosClient'
import { publishUser } from '../utils/staffAccess'
export default function AccessSync() {
  useEffect(() => {
    let busy = false, stopped = false
    const sync = async () => { if (busy || !localStorage.getItem('token') || document.hidden) return; busy = true; const token = localStorage.getItem('token'); try { const { data } = await api.get('/Auth/me'); if (!stopped && token === localStorage.getItem('token')) publishUser(data) } catch { /* Authentication interceptor handles revoked sessions; keep access during outages. */ } finally { busy = false } }
    void sync(); const timer = setInterval(sync, 3000)
    window.addEventListener('focus', sync); window.addEventListener('access-refresh', sync); document.addEventListener('visibilitychange', sync)
    return () => { stopped = true; clearInterval(timer); window.removeEventListener('focus', sync); window.removeEventListener('access-refresh', sync); document.removeEventListener('visibilitychange', sync) }
  }, [])
  return null
}
