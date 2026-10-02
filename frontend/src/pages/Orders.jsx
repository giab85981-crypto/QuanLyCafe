import { useCallback, useEffect, useState } from 'react'
import axiosClient from '../api/axiosClient'
import { errMsg } from '../api/errMsg'
import './Page.css'

const REFRESH_MS = 15000

const minutesAgo = (iso) => {
  const m = Math.max(0, Math.round((Date.now() - new Date(iso).getTime()) / 60000))
  if (m < 1) return 'Vừa xong'
  if (m < 60) return `${m} phút trước`
  return `${Math.floor(m / 60)} giờ ${m % 60} phút trước`
}

function Orders() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState(null)
  const [, tick] = useState(0)

  const load = useCallback(
    () =>
      axiosClient
        .get('/Kitchen/pending-orders')
        .then((r) => {
          setOrders(r.data)
          setError('')
        })
        .catch((e) => setError(errMsg(e, 'Không tải được danh sách đơn.')))
        .finally(() => setLoading(false)),
    [],
  )

  // Tự làm mới mỗi 15 giây để bếp thấy đơn mới
  useEffect(() => {
    load()
    const t = setInterval(() => {
      load()
      tick((n) => n + 1)
    }, REFRESH_MS)
    return () => clearInterval(t)
  }, [load])

  const complete = async (o) => {
    setBusyId(o.id)
    try {
      await axiosClient.put(`/Kitchen/${o.id}/status`, { status: 'Completed' })
      await load()
    } catch (e) {
      setError(errMsg(e, 'Không cập nhật được đơn.'))
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="pg">
      <div className="pg-head">
        <h1>Đơn hàng đang chờ pha chế</h1>
        <div className="pg-tools">
          <span className="pg-mute">Tự làm mới mỗi {REFRESH_MS / 1000} giây</span>
          <button className="pg-btn" onClick={load}>Làm mới</button>
        </div>
      </div>

      {error && <div className="pg-error">{error}</div>}
      {loading && <div className="pg-empty">Đang tải...</div>}
      {!loading && orders.length === 0 && !error && (
        <div className="pg-card pg-empty">Không có đơn nào đang chờ. Đơn mới từ màn hình bán hàng sẽ hiện ở đây.</div>
      )}

      <div className="ord-grid">
        {orders.map((o) => (
          <div key={o.id} className="pg-card ord-card">
            <div className="ord-card__head">
              <strong>{o.tableName}</strong>
              <span className="pg-mute">
                #{o.id} · {minutesAgo(o.createdAt)}
              </span>
            </div>
            <ul className="ord-card__items">
              {o.details.map((d) => (
                <li key={d.id}>
                  <span>{d.foodName}</span>
                  <strong>x{d.count}</strong>
                </li>
              ))}
            </ul>
            <div className="ord-card__foot">
              <button
                className="pg-btn pg-btn--primary"
                style={{ width: '100%' }}
                disabled={busyId === o.id}
                onClick={() => complete(o)}
              >
                {busyId === o.id ? 'Đang cập nhật...' : 'Hoàn thành'}
              </button>
            </div>
          </div>
        ))}
      </div>

      <p className="pg-note">
        Trang này hiển thị các đơn đã gửi bếp và chưa hoàn thành. Backend hiện chưa có API xem lịch sử hóa đơn.
      </p>
    </div>
  )
}

export default Orders
