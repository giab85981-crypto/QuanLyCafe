import React from 'react';
import StatCard from '../components/StatCard';
import ChartCard from '../components/ChartCard';
import { QuickLinks, PromoCard, ActivityFeed } from '../components/SidePanel';
import './Dashboard.css';

function Dashboard() {
  const user = JSON.parse(localStorage.getItem('user') || '{}');

  return (
    <div className="dashboard">
      <div className="dashboard__title-row" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1>Bức tranh kinh doanh</h1>
          <p style={{ margin: '5px 0' }}>
            Xin chào, <strong>{user.displayName || 'Người dùng'}</strong> ({user.roleName || 'N/A'})
          </p>
        </div>

        <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
          <select className="dashboard__branch-select">
            <option>Tất cả chi nhánh</option>
            <option>Chi nhánh trung tâm</option>
          </select>
        </div>
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
    </div>
  );
}

export default Dashboard;