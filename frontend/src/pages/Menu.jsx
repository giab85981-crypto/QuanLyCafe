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
  Plus,
  Pencil,
} from 'lucide-react'
import axiosClient from '../api/axiosClient'
import './Menu.css'

const money = (n) => Number(n || 0).toLocaleString('vi-VN')

// Khớp CreateFoodDto/UpdateFoodDto bên backend: { Name, Price, CostPrice, IdCategory, ItemType }
const initialForm = { name: '', idCategory: '', price: '', costPrice: '', itemType: '' }

// Danh sách "Loại món" cố định — có thể chỉnh lại cho đúng nhu cầu quán
const ITEM_TYPES = ['Món chế biến', 'Hàng hóa', 'Dịch vụ']

function Menu() {
  const [foods, setFoods] = useState([])
  const [categories, setCategories] = useState([])
  const [activeCat, setActiveCat] = useState(null)
  const [keyword, setKeyword] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  // UI-only: chọn dòng / đánh dấu món hay bán (chưa có field tương ứng ở backend)
  const [selectedIds, setSelectedIds] = useState(new Set())
  const [favoriteIds, setFavoriteIds] = useState(new Set())

  // Modal "Món mới" / "Sửa món" — dùng chung, phân biệt bằng editingId
  const [showAddModal, setShowAddModal] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(initialForm)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState('')

  // "+ Tạo mới" nhóm món (category) ở sidebar
  const [showNewCat, setShowNewCat] = useState(false)
  const [newCatName, setNewCatName] = useState('')
  const [savingCat, setSavingCat] = useState(false)
  const [catError, setCatError] = useState('')

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

  // ---- "+ Tạo mới" nhóm món: POST /api/FoodCategory ----
  const openNewCat = () => {
    setNewCatName('')
    setCatError('')
    setShowNewCat(true)
  }

  const cancelNewCat = () => {
    if (savingCat) return
    setShowNewCat(false)
  }

  const handleCreateCategory = async (e) => {
    e.preventDefault()
    if (!newCatName.trim()) {
      setCatError('Vui lòng nhập tên nhóm món.')
      return
    }

    setSavingCat(true)
    setCatError('')
    try {
      const res = await axiosClient.post('/FoodCategory', { name: newCatName.trim() })
      const created = res.data
      setCategories((prev) => [...prev, created])
      setActiveCat(created.id)
      setShowNewCat(false)
    } catch (err) {
      setCatError('Tạo nhóm món thất bại. Kiểm tra lại kết nối backend.')
    } finally {
      setSavingCat(false)
    }
  }

  // ---- "Món mới": mở modal ở chế độ thêm ----
  const openAddModal = () => {
    setEditingId(null)
    setForm({ ...initialForm, idCategory: categories[0]?.id ?? '', itemType: ITEM_TYPES[0] })
    setFormError('')
    setShowAddModal(true)
  }

  // ---- "Sửa": mở modal ở chế độ sửa, điền sẵn dữ liệu của món đang chọn ----
  const openEditModal = (food) => {
    setEditingId(food.id)
    setForm({
      name: food.name,
      idCategory: food.idCategory,
      price: food.price,
      costPrice: food.costPrice,
      itemType: food.itemType || ITEM_TYPES[0],
    })
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

    const payload = {
      name: form.name.trim(),
      idCategory: Number(form.idCategory),
      price: Number(form.price) || 0,
      costPrice: Number(form.costPrice) || 0,
      itemType: form.itemType || '',
    }

    setSaving(true)
    setFormError('')
    try {
      if (editingId) {
        await axiosClient.put(`/Food/${editingId}`, payload)
      } else {
        await axiosClient.post('/Food', payload)
      }
      setShowAddModal(false)
      loadFoods()
    } catch (err) {
      setFormError(
        editingId
          ? 'Cập nhật món thất bại. Kiểm tra lại dữ liệu hoặc kết nối backend.'
          : 'Thêm món thất bại. Kiểm tra lại dữ liệu hoặc kết nối backend.',
      )
    } finally {
      setSaving(false)
    }
  }

  // ---- "Xuất file": xuất Excel (.xlsx) bằng thư viện xlsx (SheetJS) ----
  // Cần cài: npm install xlsx
  const handleExport = () => {
    const data = rows.map((f) => ({
      'Mã món': `MN${String(f.id).padStart(4, '0')}`,
      'Tên món': f.name,
      'Nhóm món': f.categoryName,
      'Loại món': f.itemType || '',
      'Giá bán': f.price,
      'Giá vốn': f.costPrice,
    }))

    const worksheet = XLSX.utils.json_to_sheet(data)
    worksheet['!cols'] = [{ wch: 10 }, { wch: 28 }, { wch: 16 }, { wch: 16 }, { wch: 12 }, { wch: 12 }]

    const workbook = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Thực đơn')

    XLSX.writeFile(workbook, `thuc-don-${new Date().toISOString().slice(0, 10)}.xlsx`)
  }

  // ---- "Import": đọc CSV (Tên món,Nhóm món,Giá bán,Giá vốn) và POST từng dòng ----
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
            itemType: '', // CSV import chưa có cột Loại món, để trống
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
        <div className="menu-side-head">
          <h3>Nhóm hàng</h3>
          <button className="link-btn" type="button" onClick={openNewCat}>
            <Plus size={14} /> Tạo mới
          </button>
        </div>

        {showNewCat && (
          <form className="new-cat-form" onSubmit={handleCreateCategory}>
            {catError && <div className="menu-error menu-error-sm">{catError}</div>}
            <input
              autoFocus
              placeholder="Tên nhóm món mới"
              value={newCatName}
              onChange={(e) => setNewCatName(e.target.value)}
            />
            <div className="new-cat-actions">
              <button type="button" className="btn-outline btn-sm" onClick={cancelNewCat} disabled={savingCat}>
                Hủy
              </button>
              <button type="submit" className="btn-new btn-sm" disabled={savingCat}>
                {savingCat ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </form>
        )}

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
              <th>Loại món</th>
              <th className="num">Giá bán</th>
              <th className="col-actions"></th>
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
                <td>{f.itemType || '-'}</td>
                <td className="num">{money(f.price)}</td>
                <td className="col-actions">
                  <button
                    type="button"
                    className="edit-btn"
                    onClick={() => openEditModal(f)}
                    title="Sửa món"
                  >
                    <Pencil size={16} />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      {showAddModal && (
        <div className="modal-overlay" onClick={closeAddModal}>
          <div className="modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editingId ? 'Sửa món' : 'Thêm món mới'}</h2>
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

              <div className="form-row">
                <label className="form-field">
                  <span>Nhóm món</span>
                  <select name="idCategory" value={form.idCategory} onChange={handleFormChange}>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                  </select>
                </label>
                <label className="form-field">
                  <span>Loại món</span>
                  <select name="itemType" value={form.itemType} onChange={handleFormChange}>
                    {ITEM_TYPES.map((t) => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </select>
                </label>
              </div>

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
                  {saving ? 'Đang lưu...' : editingId ? 'Lưu thay đổi' : 'Lưu'}
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