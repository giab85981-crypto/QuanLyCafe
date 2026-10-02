import { useEffect, useMemo, useState } from 'react'
import axiosClient from '../api/axiosClient'
import { errMsg } from '../api/errMsg'
import Modal from '../components/Modal'
import './Page.css'

function Customers() {
  const [list, setList] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [keyword, setKeyword] = useState('')

  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ name: '', phone: '' })
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)

  const load = () =>
    axiosClient
      .get('/Customer')
      .then((r) => {
        setList(r.data)
        setError('')
      })
      .catch((e) => setError(errMsg(e, 'Không tải được danh sách khách hàng.')))
      .finally(() => setLoading(false))

  useEffect(() => {
    load()
  }, [])

  const rows = useMemo(() => {
    const kw = keyword.trim().toLowerCase()
    return list.filter((c) => !kw || c.name.toLowerCase().includes(kw) || c.phone.includes(kw))
  }, [list, keyword])

  const submit = async () => {
    const name = form.name.trim()
    const phone = form.phone.trim()
    if (!name) return setFormError('Nhập tên khách hàng.')
    if (phone && !/^\d{9,11}$/.test(phone)) return setFormError('Số điện thoại phải gồm 9–11 chữ số.')
    if (phone && list.some((c) => c.phone === phone)) return setFormError('Số điện thoại này đã có trong danh sách.')

    setSaving(true)
    try {
      await axiosClient.post('/Customer', { name, phone })
      setShowAdd(false)
      load()
    } catch (e) {
      setFormError(errMsg(e, 'Không thêm được khách hàng.'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="pg">
      <div className="pg-head">
        <h1>Khách hàng</h1>
        <div className="pg-tools">
          <input
            className="pg-input pg-input--search"
            placeholder="Tìm theo tên hoặc số điện thoại"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
          />
          <button
            className="pg-btn pg-btn--primary"
            onClick={() => {
              setForm({ name: '', phone: '' })
              setFormError('')
              setShowAdd(true)
            }}
          >
            + Thêm khách hàng
          </button>
        </div>
      </div>

      {error && <div className="pg-error">{error}</div>}

      <div className="pg-card pg-table-wrap">
        <table className="pg-table">
          <thead>
            <tr>
              <th>Mã</th>
              <th>Tên khách hàng</th>
              <th>Điện thoại</th>
              <th className="num">Điểm tích lũy</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan="4" className="pg-empty">Đang tải...</td></tr>}
            {!loading && rows.length === 0 && !error && (
              <tr>
                <td colSpan="4" className="pg-empty">
                  {list.length === 0 ? 'Chưa có khách hàng nào.' : 'Không tìm thấy khách hàng phù hợp.'}
                </td>
              </tr>
            )}
            {rows.map((c) => (
              <tr key={c.id}>
                <td>KH{String(c.id).padStart(4, '0')}</td>
                <td>{c.name}</td>
                <td>{c.phone || <span className="pg-mute">—</span>}</td>
                <td className="num">{Number(c.points).toLocaleString('vi-VN')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showAdd && (
        <Modal
          title="Thêm khách hàng"
          onClose={() => setShowAdd(false)}
          footer={
            <>
              <button className="pg-btn" onClick={() => setShowAdd(false)}>Hủy</button>
              <button className="pg-btn pg-btn--primary" onClick={submit} disabled={saving}>
                {saving ? 'Đang lưu...' : 'Thêm khách hàng'}
              </button>
            </>
          }
        >
          {formError && <div className="pg-error">{formError}</div>}
          <div className="field">
            <label htmlFor="cname">Tên khách hàng</label>
            <input id="cname" autoFocus value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
          <div className="field">
            <label htmlFor="cphone">Số điện thoại</label>
            <input id="cphone" inputMode="numeric" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          </div>
        </Modal>
      )}
    </div>
  )
}

export default Customers
