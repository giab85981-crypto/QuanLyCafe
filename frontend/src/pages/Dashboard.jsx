<<<<<<< HEAD
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
=======
import StatCard from '../components/StatCard'
import ChartCard from '../components/ChartCard'
import { QuickLinks, PromoCard, ActivityFeed } from '../components/SidePanel'
import './Dashboard.css'

function Dashboard() {
  return (
    <div className="dashboard">
      <div className="dashboard__title-row">
        <h1>Bức tranh kinh doanh</h1>
        <select className="dashboard__branch-select">
          <option>Tất cả chi nhánh</option>
          <option>Chi nhánh trung tâm</option>
        </select>
      </div>

      <div className="dashboard__stats-row">
        <StatCard
          tone="blue"
          title="Doanh thu hôm nay"
          badge="Bao gồm VAT ⓘ"
          value="0"
          subtitle="Không phát sinh doanh thu"
          rows={[
            { label: 'Giảm giá hóa đơn', value: '0' },
            { label: 'Trả hàng (0)', value: '0' },
          ]}
        />
        <StatCard
          tone="green"
          title="Số lượng đơn hôm nay"
          value="0"
          subtitle="Không phát sinh đơn"
          rows={[
            { label: 'Trung bình đơn', value: '0' },
            { label: 'Số khách/đơn ⓘ', value: '0' },
          ]}
        />
        <StatCard
          tone="amber"
          title="Tỷ lệ phủ bàn"
          value="0%"
          subtitle="0/2 bàn đang sử dụng"
          rows={[
            { label: 'Đơn đang phục vụ (0)', value: '0' },
            { label: 'Khách đang phục vụ', value: '0' },
          ]}
        />
        <QuickLinks />
      </div>

      <div className="dashboard__main-row">
        <ChartCard
          title="Doanh thu thuần"
          tooltip="Doanh thu đã trừ chiết khấu, trả hàng"
          value="50,000"
          valueSuffix="(1 hóa đơn)"
          periodOptions={['7 ngày qua', '30 ngày qua', 'Tháng này']}
          defaultPeriod="7 ngày qua"
        />

        <div className="chart-card">
          <div className="chart-card__head">
            <h3>Lượng khách hàng</h3>
            <select className="chart-card__select" defaultValue="Hôm nay">
              <option>Hôm nay</option>
              <option>7 ngày qua</option>
            </select>
          </div>
          <div className="chart-card__value">0 lượt khách</div>
          <div className="chart-card__tabs">
            <button className="chart-card__tab is-active">Theo giờ</button>
          </div>
          <div className="empty-chart">
            <span className="empty-chart__icon">📈</span>
            Chưa có lượt khách nào
          </div>
        </div>

        <div className="dashboard__side-col">
          <PromoCard />
          <ActivityFeed />
        </div>
      </div>

      <div className="dashboard__trial-banner">
        <span>⚠️ Bạn đang dùng thử phần mềm bản không giới hạn tính năng.</span>
        <a href="#">Xem chi tiết</a>
      </div>
    </div>
  )
}

export default Dashboard
>>>>>>> f7bd02673a8e7430fa9874fdcc5d5f9f734bba83
