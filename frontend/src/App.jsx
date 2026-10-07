import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, Outlet } from 'react-router-dom';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Menu from './pages/Menu';
import POS from './pages/POS';
import Header from './components/Header';
import TableManagement from './pages/TableManagement';
import Inventory from './pages/Inventory';
import Cashbook from './pages/Cashbook';
import PublicMenu from './pages/PublicMenu';
import Orders from './pages/Orders';
import Customers from './pages/Customers';
import Staff from './pages/Staff';
import KitchenBoard from './pages/KitchenBoard';
import Reports from './pages/Reports';
import Permissions from './pages/Permissions';
import Shifts from './pages/Shifts';
import AccessSync from './components/AccessSync';
import ThemeToggle from './components/ThemeToggle';
import { canOpen, landing, useAccess } from './utils/staffAccess';
import { useLocation } from 'react-router-dom';

const ProtectedRoute = ({ children }) => {
  const user = useAccess();
  const token = localStorage.getItem('token');
  const path = useLocation().pathname;
  if (token && !canOpen(path, user)) return <Navigate to={landing(user)} replace />;
  return token ? children : <Navigate to="/login" replace />;
};

// Layout có thanh Header xanh phía trên, các trang con render vào <Outlet />
const MainLayout = () => (
  <>
    <Header />
    <Outlet />
  </>
);

function NoAccessRedirect() { const user = useAccess(); return !localStorage.getItem('token') ? <Navigate to="/login" replace /> : landing(user) !== '/no-access' ? <Navigate to={landing(user)} replace /> : null }
function App() {
  return (
    <Router>
      <ThemeToggle />
      <AccessSync />
      <Routes>
        <Route path="/no-access" element={<div style={{padding:60,textAlign:'center'}}><h1>Chưa được cấp quyền chức năng</h1><p>Liên hệ quản trị viên. Quyền mới sẽ tự cập nhật sau vài giây.</p><button onClick={() => { localStorage.removeItem('token'); localStorage.removeItem('user'); window.location.assign('/login') }}>Đăng xuất</button><NoAccessRedirect /></div>} />
        <Route path="/login" element={<Login />} />
        <Route path="/menu-view/:tableId" element={<PublicMenu />} />

        {/* Các trang có Header */}
        <Route
          element={
            <ProtectedRoute>
              <MainLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/reports" element={<Reports />} />
          <Route path="/shifts" element={<Shifts />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/menu" element={<Menu />} />
          <Route path="/inventory" element={<Inventory />} />
          <Route path="/orders" element={<Orders />} />
          <Route path="/customers" element={<Customers />} />
          <Route path="/permissions" element={<Permissions />} />
          <Route path="/staff" element={<Staff />} />

          <Route path="/cashbook" element={<Cashbook />} />
          {/* Đã bổ sung trang Quản lý Phòng/Bàn */}
          <Route path="/tables" element={<TableManagement />} />
        </Route>

        <Route path="/kitchen" element={<ProtectedRoute><KitchenBoard /></ProtectedRoute>} />

        {/* POS toàn màn hình, không dùng Header */}
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
  );
}

export default App;
