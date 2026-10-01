import { BrowserRouter as Router, Routes, Route, Navigate, Outlet } from 'react-router-dom'
import Header from './components/Header'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import POS from './pages/POS'
import Menu from './pages/Menu'

// Chỉ cho vào khi đã đăng nhập
const ProtectedRoute = ({ children }) => {
  const token = localStorage.getItem('token')
  return token ? children : <Navigate to="/login" replace />
}

// Khung quản lý: thanh Header kiểu KiotViet + nội dung trang
const AdminLayout = () => (
  <>
    <Header />
    <Outlet />
  </>
)

const Placeholder = ({ title }) => (
  <div style={{ padding: 32 }}>
    <h1 style={{ fontSize: 22 }}>{title}</h1>
    <p style={{ color: 'var(--text-mute)' }}>Trang này sẽ được làm tiếp.</p>
  </div>
)

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route
          element={
            <ProtectedRoute>
              <AdminLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/menu" element={<Menu />} />
          <Route path="/inventory" element={<Placeholder title="Kho hàng" />} />
          <Route path="/tables" element={<Placeholder title="Phòng/Bàn" />} />
          <Route path="/orders" element={<Placeholder title="Đơn hàng" />} />
          <Route path="/customers" element={<Placeholder title="Khách hàng" />} />
          <Route path="/staff" element={<Placeholder title="Nhân viên" />} />
          <Route path="/cashbook" element={<Placeholder title="Sổ quỹ" />} />
          <Route path="/reports" element={<Placeholder title="Báo cáo" />} />
        </Route>

        {/* Màn hình bán hàng: toàn màn hình, không có Header quản lý */}
        <Route
          path="/pos"
          element={
            <ProtectedRoute>
              <POS />
            </ProtectedRoute>
          }
        />

        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </Router>
  )
}

export default App
