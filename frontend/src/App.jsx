import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, Outlet } from 'react-router-dom';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Menu from './pages/Menu';
import POS from './pages/POS';
import Header from './components/Header';
import TableManagement from './pages/TableManagement';

const ProtectedRoute = ({ children }) => {
  const token = localStorage.getItem('token');
  return token ? children : <Navigate to="/login" replace />;
};

// Layout có thanh Header xanh phía trên, các trang con render vào <Outlet />
const MainLayout = () => (
  <>
    <Header />
    <Outlet />
  </>
);

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />

        {/* Các trang có Header */}
        <Route
          element={
            <ProtectedRoute>
              <MainLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/menu" element={<Menu />} />
          {/* Đã bổ sung trang Quản lý Phòng/Bàn */}
          <Route path="/tables" element={<TableManagement />} />
        </Route>

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