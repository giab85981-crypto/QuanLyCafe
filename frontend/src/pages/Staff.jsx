import { useEffect, useState } from 'react'
import axiosClient from '../api/axiosClient'
import { errMsg } from '../api/errMsg'
import Modal from '../components/Modal'
import './Page.css'

// Backend chưa có API lấy danh sách vai trò. Id theo thứ tự DbSeeder tạo: Admin=1, Cashier=2, Kitchen=3
const ROLES = [
  { id: 1, label: 'Quản trị viên' },
  { id: 2, label: 'Thu ngân' },
  { id: 3, label: 'Bếp / Pha chế' },
]

const EMPTY_FORM = { userName: '', passWord: '', displayName: '', idRole: 2 }

function Staff() {
  const me = JSON.parse(localStorage.getItem('user') || '{}')
  const [list, setList] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [modal, setModal] = useState(null) // null | { mode: 'create' | 'edit', form }
  const [formError, setFormError] = useState('')
  const [saving, setSaving] = useState(false)

  const load = () =>
    axiosClient
      .get('/Account')
      .then((r) => {
        setList(r.data)
        setError('')
      })
      .catch((e) => setError(errMsg(e, 'Không tải được danh sách nhân viên.')))
      .finally(() => setLoading(false))

  useEffect(() => {
    load()
  }, [])

  const openCreate = () => {
    setFormError('')
    setModal({ mode: 'create', form: { ...EMPTY_FORM } })
  }

  const openEdit = (a) => {
    setFormError('')
    setModal({ mode: 'edit', form: { userName: a.userName, passWord: '', displayName: a.displayName, idRole: a.idRole } })
  }

  const setField = (patch) => setModal((m) => ({ ...m, form: { ...m.form, ...patch } }))

  const submit = async () => {
    const f = modal.form
    if (!f.displayName.trim()) return setFormError('Nhập tên hiển thị.')
    if (modal.mode === 'create') {
      if (!f.userName.trim()) return setFormError('Nhập tên đăng nhập.')
      if (list.some((a) => a.userName.toLowerCase() === f.userName.trim().toLowerCase()))
        return setFormError('Tên đăng nhập đã tồn tại.')
      if (f.passWord.length < 6) return setFormError('Mật khẩu phải có ít nhất 6 ký tự.')
    } else if (f.passWord && f.passWord.length < 6) {
      return setFormError('Mật khẩu mới phải có ít nhất 6 ký tự.')
    }

    setSaving(true)
    try {
      if (modal.mode === 'create') {
        await axiosClient.post('/Account', {
          userName: f.userName.trim(),
          passWord: f.passWord,
          displayName: f.displayName.trim(),
          idRole: Number(f.idRole),
        })
      } else {
        await axiosClient.put(`/Account/${encodeURIComponent(f.userName)}`, {
          displayName: f.displayName.trim(),
          idRole: Number(f.idRole),
          passWord: f.passWord || null,
        })
      }
      setModal(null)
      load()
    } catch (e) {
      setFormError(errMsg(e, 'Không lưu được nhân viên.'))
    } finally {
      setSaving(false)
    }
  }

  const toggleActive = async (a) => {
    try {
      await axiosClient.put(`/Account/${encodeURIComponent(a.userName)}/status`, { isActive: !a.isActive })
      load()
    } catch (e) {
      setError(errMsg(e, 'Không đổi được trạng thái tài khoản.'))
    }
  }

  const remove = async (a) => {
    if (!window.confirm(`Xóa tài khoản "${a.userName}"? Thao tác này không hoàn tác được.`)) return
    try {
      await axiosClient.delete(`/Account/${encodeURIComponent(a.userName)}`)
      load()
    } catch (e) {
      setError(errMsg(e, 'Không xóa được tài khoản.'))
    }
  }

  return (
    <div className="pg">
      <div className="pg-head">
        <h1>Nhân viên</h1>
        <button className="pg-btn pg-btn--primary" onClick={openCreate}>+ Thêm nhân viên</button>
      </div>

      {error && <div className="pg-error">{error}</div>}

      <div className="pg-card pg-table-wrap">
        <table className="pg-table">
          <thead>
            <tr>
              <th>Tên đăng nhập</th>
              <th>Tên hiển thị</th>
              <th>Vai trò</th>
              <th>Trạng thái</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan="5" className="pg-empty">Đang tải...</td></tr>}
            {!loading && list.length === 0 && !error && <tr><td colSpan="5" className="pg-empty">Chưa có nhân viên nào.</td></tr>}
            {list.map((a) => {
              const isMe = a.userName === me.userName
              return (
                <tr key={a.userName}>
                  <td>{a.userName}{isMe && <span className="pg-mute"> (bạn)</span>}</td>
                  <td>{a.displayName}</td>
                  <td>{a.roleName}</td>
                  <td>
                    <span className={`pg-badge ${a.isActive ? 'pg-badge--ok' : 'pg-badge--mute'}`}>
                      {a.isActive ? 'Đang làm việc' : 'Đã khóa'}
                    </span>
                  </td>
                  <td>
                    <div className="actions">
                      <button className="pg-btn pg-btn--sm" onClick={() => openEdit(a)}>Sửa</button>
                      <button className="pg-btn pg-btn--sm" disabled={isMe} onClick={() => toggleActive(a)}>
                        {a.isActive ? 'Khóa' : 'Mở khóa'}
                      </button>
                      <button className="pg-btn pg-btn--sm pg-btn--danger" disabled={isMe} onClick={() => remove(a)}>Xóa</button>
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {modal && (
        <Modal
          title={modal.mode === 'create' ? 'Thêm nhân viên' : `Sửa nhân viên: ${modal.form.userName}`}
          onClose={() => setModal(null)}
          footer={
            <>
              <button className="pg-btn" onClick={() => setModal(null)}>Hủy</button>
              <button className="pg-btn pg-btn--primary" onClick={submit} disabled={saving}>
                {saving ? 'Đang lưu...' : 'Lưu'}
              </button>
            </>
          }
        >
          {formError && <div className="pg-error">{formError}</div>}
          {modal.mode === 'create' && (
            <div className="field">
              <label htmlFor="un">Tên đăng nhập</label>
              <input id="un" autoFocus value={modal.form.userName} onChange={(e) => setField({ userName: e.target.value })} />
            </div>
          )}
          <div className="field">
            <label htmlFor="dn">Tên hiển thị</label>
            <input id="dn" value={modal.form.displayName} onChange={(e) => setField({ displayName: e.target.value })} />
          </div>
          <div className="field">
            <label htmlFor="rl">Vai trò</label>
            <select id="rl" value={modal.form.idRole} onChange={(e) => setField({ idRole: e.target.value })}>
              {ROLES.map((r) => (
                <option key={r.id} value={r.id}>{r.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label htmlFor="pw">{modal.mode === 'create' ? 'Mật khẩu' : 'Mật khẩu mới'}</label>
            <input id="pw" type="password" autoComplete="new-password" value={modal.form.passWord} onChange={(e) => setField({ passWord: e.target.value })} />
            {modal.mode === 'edit' && <span className="field__hint">Để trống nếu không muốn đổi mật khẩu.</span>}
          </div>
        </Modal>
      )}
    </div>
  )
}

export default Staff
