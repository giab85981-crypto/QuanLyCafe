import { useEffect, useMemo, useRef, useState } from 'react'
import * as XLSX from 'xlsx'
import {
  Search,
  ChevronDown,
  Upload,
  Download,
  List,
  HelpCircle,
  Settings,
  Star,
  X,
} from 'lucide-react'
import axiosClient from '../api/axiosClient'
import './Menu.css'

const money = (n) => Number(n || 0).toLocaleString('vi-VN')

// Khớp đúng CreateFoodDto bên backend: { Name, Price, CostPrice, IdCategory }
// (JSON trả về/nhận vào ở dạng camelCase: name, price, costPrice, idCategory)
const initialForm = { name: '', idCategory: '', price: '', costPrice: '' }

function Menu() {
  const [foods, setFoods] = useState([])
  const [categories, setCategories] = useState([])
  const [activeCat, setActiveCat] = useState(null)
  const [keyword, setKeyword] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // UI-only: chọn dòng / đánh dấu món hay bán (chưa gắn API vì backend chưa có field này)
  const [selectedIds, setSelectedIds] = useState(new Set())
  const [favoriteIds, setFavoriteIds] = useState(new Set())

  // Modal "Món mới"
  const [showAddModal, setShowAddModal] = useState(false)
  const [form, setForm] = useState(initialForm)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState('')

  // Import CSV
  const fileInputRef = useRef(null)
  const [importing, setImporting] = useState(false)

  const loadFoods = () => {
    setLoading(true)
    Promise.all([axiosClient.get('/Food'), axiosClient.get('/FoodCategory')])
      .then(([f, c]) => {
        setFoods(f.data)
        setCategories(c.data)
      })
      .catch(() => setError('Không tải được thực đơn. Kiểm tra backend đã chạy chưa.'))
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    loadFoods()
  }, [])

  const rows = useMemo(() => {
    const kw = keyword.trim().toLowerCase()
    return foods.filter(
      (f) =>
        (activeCat === null || f.idCategory === activeCat) &&
        (!kw || f.name.toLowerCase().includes(kw)),
    )
  }, [foods, activeCat, keyword])

  const allSelected = rows.length > 0 && rows.every((f) => selectedIds.has(f.id))

  const toggleSelectAll = () => {
    setSelectedIds(allSelected ? new Set() : new Set(rows.map((f) => f.id)))
  }

  const toggleSelectOne = (id) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  const toggleFavorite = (id) => {
    setFavoriteIds((prev) => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  // ---- "Món mới": modal + POST /api/Food ----
  const openAddModal = () => {
    setForm({ ...initialForm, idCategory: categories[0]?.id ?? '' })
    setFormError('')
    setShowAddModal(true)
  }

  const closeAddModal = () => {
    if (saving) return
    setShowAddModal(false)
  }

  const handleFormChange = (e) => {
    const { name, value } = e.target
    setForm((prev) => ({ ...prev, [name]: value }))
  }

  const handleAddSubmit = async (e) => {
    e.preventDefault()
    if (!form.name.trim() || !form.price || !form.idCategory) {
      setFormError('Vui lòng nhập tên món, giá bán và chọn nhóm món.')
      return
    }

    setSaving(true)
    setFormError('')
    try {
      await axiosClient.post('/Food', {
        name: form.name.trim(),
        idCategory: Number(form.idCategory),
        price: Number(form.price) || 0,
        costPrice: Number(form.costPrice) || 0,
      })
      setShowAddModal(false)
      loadFoods()
    } catch (err) {
      setFormError('Thêm món thất bại. Kiểm tra lại dữ liệu hoặc kết nối backend.')
    } finally {
      setSaving(false)
    }
  }

  // ---- "Xuất file": xuất Excel (.xlsx) thật sự bằng thư viện xlsx (SheetJS) ----
  // Cần cài: npm install xlsx
  const handleExport = () => {
    const data = rows.map((f) => ({
      'Mã món': `MN${String(f.id).padStart(4, '0')}`,
      'Tên món': f.name,
      'Nhóm món': f.categoryName,
      'Giá bán': f.price,
      'Giá vốn': f.costPrice,
    }))

    const worksheet = XLSX.utils.json_to_sheet(data)
    worksheet['!cols'] = [{ wch: 10 }, { wch: 28 }, { wch: 16 }, { wch: 12 }, { wch: 12 }]

    const workbook = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Thực đơn')

    XLSX.writeFile(workbook, `thuc-don-${new Date().toISOString().slice(0, 10)}.xlsx`)
  }

  // ---- "Import": đọc file CSV (Tên món,Nhóm món,Giá bán,Giá vốn) và POST từng dòng lên /api/Food ----
  const handleImportClick = () => {
    fileInputRef.current?.click()
  }

  const handleImportFile = async (e) => {
    const file = e.target.files?.[0]
    if (!file) return

    setImporting(true)
    setError('')
    try {
      const text = await file.text()
      const lines = text.split(/\r?\n/).filter((l) => l.trim())
      // Bỏ dòng tiêu đề nếu dòng đầu không phải dữ liệu số ở cột giá
      const dataLines = /^[^,]+,[^,]+,\s*\d/.test(lines[0]) ? lines : lines.slice(1)

      const items = dataLines
        .map((line) => {
          const [name, categoryName, price, costPrice] = line.split(',').map((s) => s.trim())
          const match = categories.find((c) => c.name === categoryName)
          return {
            name,
            idCategory: match ? match.id : null,
            price: Number(price) || 0,
            costPrice: Number(costPrice) || 0,
          }
        })
        .filter((item) => item.name && item.idCategory)

      if (items.length === 0) {
        setError(
          'File import không có dòng hợp lệ. Định dạng cần: Tên món,Nhóm món,Giá bán,Giá vốn — và "Nhóm món" phải trùng tên nhóm đã có sẵn.',
        )
        return
      }

      await Promise.all(items.map((item) => axiosClient.post('/Food', item)))
      loadFoods()
    } catch (err) {
      setError('Import thất bại. Kiểm tra định dạng file hoặc kết nối backend.')
    } finally {
      setImporting(false)
      e.target.value = ''
    }
  }

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
        <div className="menu-head">
          <h1>Thực đơn</h1>
        </div>

        <div className="menu-toolbar">
          <div className="menu-search-wrap">
            <Search size={16} className="menu-search-icon" />
            <input
              className="menu-search"
              placeholder="Theo mã hoặc tên món"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
            />
          </div>

          <div className="menu-toolbar-actions">
            <button className="btn-new" type="button" onClick={openAddModal}>
              Món mới <ChevronDown size={14} />
            </button>
            <button className="btn-outline" type="button" onClick={handleImportClick} disabled={importing}>
              <Upload size={16} /> {importing ? 'Đang import...' : 'Import'}
            </button>
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv"
              style={{ display: 'none' }}
              onChange={handleImportFile}
            />
            <button className="btn-outline" type="button" onClick={handleExport}>
              <Download size={16} /> Xuất file
            </button>
            <button className="icon-btn" type="button" title="Danh sách">
              <List size={18} />
            </button>
            <button className="icon-btn" type="button" title="Trợ giúp">
              <HelpCircle size={18} />
            </button>
            <button className="icon-btn" type="button" title="Cài đặt">
              <Settings size={18} />
            </button>
          </div>
        </div>

        {error && <div className="menu-error">{error}</div>}

        <table className="menu-table">
          <thead>
            <tr>
              <th className="col-check">
                <input type="checkbox" checked={allSelected} onChange={toggleSelectAll} />
              </th>
              <th className="col-star"></th>
              <th>Mã món</th>
              <th>Tên món</th>
              <th>Nhóm món</th>
              <th>Loại thực đơn</th>
              <th>Loại món</th>
              <th className="num">Giá bán</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan="8" className="menu-empty">Đang tải...</td></tr>
            )}
            {!loading && rows.length === 0 && !error && (
              <tr><td colSpan="8" className="menu-empty">Không có món nào phù hợp</td></tr>
            )}
            {rows.map((f) => (
              <tr key={f.id} className={selectedIds.has(f.id) ? 'is-selected' : ''}>
                <td className="col-check">
                  <input
                    type="checkbox"
                    checked={selectedIds.has(f.id)}
                    onChange={() => toggleSelectOne(f.id)}
                  />
                </td>
                <td className="col-star">
                  <button
                    type="button"
                    className={`star-btn ${favoriteIds.has(f.id) ? 'is-fav' : ''}`}
                    onClick={() => toggleFavorite(f.id)}
                    title="Đánh dấu món hay bán"
                  >
                    <Star size={16} />
                  </button>
                </td>
                <td>MN{String(f.id).padStart(4, '0')}</td>
                <td>{f.name}</td>
                <td>{f.categoryName}</td>
                <td>Đồ uống</td>
                <td>Món chế biến</td>
                <td className="num">{money(f.price)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      {showAddModal && (
        <div className="modal-overlay" onClick={closeAddModal}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Thêm món mới</h2>
              <button className="icon-btn" type="button" onClick={closeAddModal}>
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleAddSubmit}>
              {formError && <div className="menu-error">{formError}</div>}

              <label className="form-field">
                <span>Tên món</span>
                <input name="name" value={form.name} onChange={handleFormChange} placeholder="VD: Cà phê sữa" />
              </label>

              <label className="form-field">
                <span>Nhóm món</span>
                <select name="idCategory" value={form.idCategory} onChange={handleFormChange}>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </label>

              <div className="form-row">
                <label className="form-field">
                  <span>Giá bán</span>
                  <input name="price" type="number" min="0" value={form.price} onChange={handleFormChange} />
                </label>
                <label className="form-field">
                  <span>Giá vốn</span>
                  <input name="costPrice" type="number" min="0" value={form.costPrice} onChange={handleFormChange} />
                </label>
              </div>

              <div className="modal-actions">
                <button type="button" className="btn-outline" onClick={closeAddModal} disabled={saving}>
                  Hủy
                </button>
                <button type="submit" className="btn-new" disabled={saving}>
                  {saving ? 'Đang lưu...' : 'Lưu'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}

export default Menu