import React, { useState, useEffect } from 'react';
import './TableManagement.css';

function TableManagement() {
  const token = localStorage.getItem('token');
  
  // State Dữ liệu
  const [areas, setAreas] = useState([]);
  const [tables, setTables] = useState([]);
  const [loading, setLoading] = useState(false);

  // State Bộ lọc
  const [selectedArea, setSelectedArea] = useState(null); // null = Tất cả
  const [statusFilter, setStatusFilter] = useState('Tất cả');
  const [search, setSearch] = useState('');

  // State Modals
  const [showTableModal, setShowTableModal] = useState(false);
  const [showAreaModal, setShowAreaModal] = useState(false);

  // Form States
  const [newAreaName, setNewAreaName] = useState('');
  const [newTable, setNewTable] = useState({
    name: '',
    seats: 4,
    note: '',
    sortOrder: 0,
    idArea: ''
  });

  // 1. Gọi API lấy danh sách Khu vực (/api/TableFood/areas)
  const fetchAreas = async () => {
    try {
      const res = await fetch('https://localhost:7053/api/TableFood/areas', {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setAreas(data);
      }
    } catch (err) {
      console.error('Lỗi fetch areas:', err);
    }
  };

  // 2. Gọi API lấy danh sách Phòng/Bàn (/api/TableFood)
  const fetchTables = async () => {
    setLoading(true);
    try {
      let url = `https://localhost:7053/api/TableFood?search=${search}`;
      if (selectedArea) url += `&areaId=${selectedArea}`;
      if (statusFilter !== 'Tất cả') url += `&status=${statusFilter}`;

      const res = await fetch(url, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setTables(data);
      }
    } catch (err) {
      console.error('Lỗi fetch tables:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAreas();
  }, []);

  useEffect(() => {
    fetchTables();
  }, [selectedArea, statusFilter, search]);

  // 3. Tạo Khu vực mới (POST /api/TableFood/areas)
  const handleCreateArea = async (e) => {
    e.preventDefault();
    if (!newAreaName.trim()) return;

    const res = await fetch('https://localhost:7053/api/TableFood/areas', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify({ name: newAreaName })
    });

    if (res.ok) {
      setNewAreaName('');
      setShowAreaModal(false);
      fetchAreas();
    }
  };

  // 4. Tạo Bàn mới (POST /api/TableFood)
  const handleCreateTable = async (e) => {
    e.preventDefault();
    if (!newTable.name.trim()) return;

    const res = await fetch('https://localhost:7053/api/TableFood', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify({
        ...newTable,
        idArea: newTable.idArea ? parseInt(newTable.idArea) : null
      })
    });

    if (res.ok) {
      setNewTable({ name: '', seats: 4, note: '', sortOrder: 0, idArea: '' });
      setShowTableModal(false);
      fetchTables();
      fetchAreas();
    }
  };

  // 5. Xóa Bàn (DELETE /api/TableFood/{id})
  const handleDeleteTable = async (id) => {
    if (!window.confirm('Bạn có chắc muốn xóa bàn này?')) return;
    const res = await fetch(`https://localhost:7053/api/TableFood/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
    if (res.ok) {
      fetchTables();
      fetchAreas();
    }
  };

  return (
    <div className="table-page">
      {/* CỘT BÊN TRÁI: KHU VỰC & TRẠNG THÁI */}
      <div className="table-page__sidebar">
        <div className="sidebar-group">
          <div className="sidebar-group__header">
            <h3>Khu vực</h3>
            <button className="btn-link" onClick={() => setShowAreaModal(true)}>+ Tạo mới</button>
          </div>
          <ul className="filter-list">
            <li 
              className={selectedArea === null ? 'active' : ''} 
              onClick={() => setSelectedArea(null)}
            >
              <input type="radio" checked={selectedArea === null} readOnly /> Tất cả
            </li>
            {areas.map(a => (
              <li 
                key={a.id} 
                className={selectedArea === a.id ? 'active' : ''} 
                onClick={() => setSelectedArea(a.id)}
              >
                <input type="radio" checked={selectedArea === a.id} readOnly /> 
                {a.name} <span className="badge">{a.tableCount}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="sidebar-group" style={{ marginTop: '24px' }}>
          <h3>Trạng thái</h3>
          <ul className="filter-list">
            {['Tất cả', 'Trống', 'Có người', 'Ngừng hoạt động'].map(st => (
              <li 
                key={st} 
                className={statusFilter === st ? 'active' : ''} 
                onClick={() => setStatusFilter(st)}
              >
                <input type="radio" checked={statusFilter === st} readOnly /> {st}
              </li>
            ))}
          </ul>
        </div>
      </div>

      {/* NỘI DUNG CHÍNH BÊN PHẢI */}
      <div className="table-page__content">
        {/* HEADER TOOLBAR */}
        <div className="table-page__toolbar">
          <div className="search-box">
            <span className="search-icon">🔍</span>
            <input 
              type="text" 
              placeholder="Theo tên phòng bàn, số ghế..." 
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          <div className="action-buttons">
            <button className="btn-primary" onClick={() => setShowTableModal(true)}>
              + Thêm phòng/bàn
            </button>
            <button className="btn-secondary">📥 Import</button>
            <button className="btn-secondary">📤 Xuất file</button>
          </div>
        </div>

        {/* BẢNG DỮ LIỆU HOẶC EMPTY STATE */}
        <div className="table-page__main-card">
          {loading ? (
            <div className="loading-state" style={{ textAlign: 'center', padding: '40px' }}>Đang tải dữ liệu...</div>
          ) : tables.length > 0 ? (
            <table className="data-table">
              <thead>
                <tr>
                  <th>Tên phòng/bàn</th>
                  <th>Ghi chú</th>
                  <th>Khu vực</th>
                  <th>Số ghế</th>
                  <th>Trạng thái</th>
                  <th>Thứ tự</th>
                  <th style={{ textAlign: 'right' }}>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {tables.map((t) => (
                  <tr key={t.id}>
                    <td className="fw-bold" style={{ fontWeight: '600' }}>{t.name}</td>
                    <td>{t.note || '---'}</td>
                    <td>{t.areaName}</td>
                    <td>{t.seats}</td>
                    <td>
                      <span className={`status-badge status-${t.status.toLowerCase().replace(/\s+/g, '-')}`}>
                        {t.status}
                      </span>
                    </td>
                    <td>{t.sortOrder}</td>
                    <td style={{ textAlign: 'right' }}>
                      <button className="btn-danger-sm" onClick={() => handleDeleteTable(t.id)}>Xóa</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="empty-state">
              <div className="empty-icon">📂</div>
              <h3>Bắt đầu quản lý không gian cửa hàng với khu vực & phòng bàn</h3>
              <p>Để thiết lập hồ sơ cửa hàng, trước tiên hãy tạo khu vực hoặc bạn có thể thêm phòng bàn ngay.</p>
              <div className="empty-actions">
                <button className="btn-outline" onClick={() => setShowTableModal(true)}>Tạo phòng bàn</button>
                <button className="btn-primary" onClick={() => setShowAreaModal(true)}>Tạo khu vực</button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* MODAL TẠO KHU VỰC */}
      {showAreaModal && (
        <div className="modal-overlay">
          <div className="modal-card">
            <h3>Tạo Khu vực mới</h3>
            <form onSubmit={handleCreateArea}>
              <div className="form-group">
                <label>Tên khu vực *</label>
                <input 
                  type="text" 
                  required 
                  placeholder="VD: Tầng 1, Sân thượng..." 
                  value={newAreaName}
                  onChange={(e) => setNewAreaName(e.target.value)}
                />
              </div>
              <div className="modal-footer">
                <button type="button" className="btn-secondary" onClick={() => setShowAreaModal(false)}>Hủy</button>
                <button type="submit" className="btn-primary">Lưu</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL TẠO PHÒNG BÀN */}
      {showTableModal && (
        <div className="modal-overlay">
          <div className="modal-card">
            <h3>Thêm Phòng / Bàn mới</h3>
            <form onSubmit={handleCreateTable}>
              <div className="form-group">
                <label>Tên phòng/bàn *</label>
                <input 
                  type="text" 
                  required 
                  placeholder="VD: Bàn 01, VIP 2..." 
                  value={newTable.name}
                  onChange={(e) => setNewTable({ ...newTable, name: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>Khu vực</label>
                <select 
                  value={newTable.idArea} 
                  onChange={(e) => setNewTable({ ...newTable, idArea: e.target.value })}
                >
                  <option value="">-- Chọn khu vực --</option>
                  {areas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                </select>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Số ghế</label>
                  <input 
                    type="number" 
                    min="1" 
                    value={newTable.seats}
                    onChange={(e) => setNewTable({ ...newTable, seats: parseInt(e.target.value) || 1 })}
                  />
                </div>
                <div className="form-group">
                  <label>Số thứ tự</label>
                  <input 
                    type="number" 
                    value={newTable.sortOrder}
                    onChange={(e) => setNewTable({ ...newTable, sortOrder: parseInt(e.target.value) || 0 })}
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Ghi chú</label>
                <input 
                  type="text" 
                  placeholder="Ghi chú thêm..." 
                  value={newTable.note}
                  onChange={(e) => setNewTable({ ...newTable, note: e.target.value })}
                />
              </div>
              <div className="modal-footer">
                <button type="button" className="btn-secondary" onClick={() => setShowTableModal(false)}>Hủy</button>
                <button type="submit" className="btn-primary">Lưu</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default TableManagement;