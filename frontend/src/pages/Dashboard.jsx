import React from 'react';
import { useNavigate } from 'react-router-dom';

const Dashboard = () => {
  const navigate = useNavigate();
  const user = JSON.parse(localStorage.getItem('user') || '{}');

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    navigate('/login');
  };

  return (
    <div style={{ padding: '30px' }}>
      <h1>Quản Lý Quán Cafe - Dashboard</h1>
      <p>Xin chào, <strong>{user.displayName || 'Người dùng'}</strong> ({user.roleName})</p>
      
      <h3>Quyền hạn của bạn:</h3>
      <ul>
        {user.permissions?.map((perm, index) => (
          <li key={index}>{perm}</li>
        ))}
      </ul>

      <button 
        onClick={handleLogout} 
        style={{ padding: '10px 20px', backgroundColor: '#dc3545', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
      >
        Đăng xuất
      </button>
    </div>
  );
};

export default Dashboard;