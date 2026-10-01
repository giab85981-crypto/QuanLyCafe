<<<<<<< HEAD
import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';

// Component bảo vệ Route (chỉ cho phép truy cập khi đã Login)
const ProtectedRoute = ({ children }) => {
  const token = localStorage.getItem('token');
  return token ? children : <Navigate to="/login" replace />;
};

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </Router>
  );
}

export default App;
=======
import Header from './components/Header'
import Dashboard from './pages/Dashboard'

function App() {
  return (
    <>
      <Header />
      <Dashboard />
    </>
  )
}

export default App
>>>>>>> f7bd02673a8e7430fa9874fdcc5d5f9f734bba83
