import React, { useState, useEffect } from 'react';
import StatCard from '../components/StatCard';
import ChartCard from '../components/ChartCard';
import { QuickLinks, PromoCard, ActivityFeed } from '../components/SidePanel';
import './Dashboard.css';

function Dashboard() {
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  const token = localStorage.getItem('token');

  const [loading, setLoading] = useState(true);
  const [days, setDays] = useState(7);
  const [dashboardData, setDashboardData] = useState({
    summary: {
      todayRevenue: 0,
      todayOrderCount: 0,
      occupiedTables: 0,
      totalTables: 0,
      occupancyRate: 0,
    },
    topSellingFoods: [],
    categorySales: [],
    revenueChart: [],
  });

  // Hàm định dạng tiền tệ VNĐ
  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount || 0);
  };

  // Gọi API từ Backend ASP.NET Core
  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        const response = await fetch(`https://localhost:7053/api/Dashboard/overview?days=${days}`, {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        });

        if (response.ok) {
          const result = await response.json();
          setDashboardData(result);
        } else {
          console.error('Lỗi khi tải dữ liệu dashboard:', response.statusText);
        }
      } catch (error) {
        console.error('Lỗi kết nối API:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, [days, token]);

  const { summary, revenueChart, topSellingFoods } = dashboardData;

  return (
    <div className="dashboard">
      <div className="dashboard__title-row" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1>Bức tranh kinh doanh</h1>
          <p style={{ margin: '5px 0' }}>
            Xin chào, <strong>{user.displayName || user.username || 'Người dùng'}</strong> ({user.roleName || 'Quản lý'})
          </p>
        </div>

        <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
          <select className="dashboard__branch-select">
            <option>Tất cả chi nhánh</option>
            <option>Chi nhánh trung tâm</option>
          </select>
        </div>
      </div>

      {loading ? (
        <div style={{ padding: '40px', textAlign: 'center' }}>Đang tải dữ liệu kinh doanh...</div>
      ) : (
        <>
          {/* HÀNG 1: THẺ KPI THỐNG KÊ */}
          <div className="dashboard__stats-row">
            <StatCard
              tone="blue"
              title="Doanh thu hôm nay"
              badge="Bao gồm VAT ⓘ"
              value={formatCurrency(summary.todayRevenue)}
              subtitle={summary.todayRevenue > 0 ? "Phát sinh doanh thu hôm nay" : "Chưa có doanh thu hôm nay"}
              rows={[
                { label: 'Giảm giá hóa đơn', value: '0 đ' },
                { label: 'Trả hàng (0)', value: '0 đ' },
              ]}
            />
            <StatCard
              tone="green"
              title="Số lượng đơn hôm nay"
              value={`${summary.todayOrderCount} đơn`}
              subtitle={summary.todayOrderCount > 0 ? "Số đơn đã hoàn tất" : "Chưa phát sinh đơn"}
              rows={[
                {
                  label: 'Trung bình đơn',
                  value: formatCurrency(summary.todayOrderCount > 0 ? summary.todayRevenue / summary.todayOrderCount : 0)
                },
                { label: 'Số khách/đơn ⓘ', value: '1' },
              ]}
            />
            <StatCard
              tone="amber"
              title="Tỷ lệ phủ bàn"
              value={`${summary.occupancyRate}%`}
              subtitle={`${summary.occupiedTables}/${summary.totalTables} bàn đang sử dụng`}
              rows={[
                { label: 'Đơn đang phục vụ', value: `${summary.occupiedTables}` },
                { label: 'Khách đang phục vụ', value: `${summary.occupiedTables}` },
              ]}
            />
            <QuickLinks />
          </div>

          {/* HÀNG 2: BIỂU ĐỒ VÀ BẢNG TOP BÁN CHẠY */}
          <div className="dashboard__main-row">
            <ChartCard
              title="Doanh thu thuần"
              tooltip="Doanh thu đã trừ chiết khấu, trả hàng"
              value={formatCurrency(summary.todayRevenue)}
              valueSuffix={`(${summary.todayOrderCount} hóa đơn)`}
              periodOptions={['7 ngày qua', '30 ngày qua']}
              defaultPeriod={`${days} ngày qua`}
              onPeriodChange={(selectedDays) => setDays(selectedDays === '30 ngày qua' ? 30 : 7)}
              chartData={revenueChart}
            />

            {/* BẢNG TOP 10 MÓN BÁN CHẠY */}
            <div className="chart-card">
              <div className="chart-card__head">
                <h3>Top món bán chạy</h3>
              </div>
              <div className="top-foods-list" style={{ marginTop: '12px' }}>
                {topSellingFoods && topSellingFoods.length > 0 ? (
                  <table style={{ width: '100%', fontSize: '13px', borderCollapse: 'collapse' }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid #eee', textAlign: 'left', color: '#666' }}>
                        <th style={{ padding: '6px 0' }}>#</th>
                        <th>Tên món</th>
                        <th style={{ textAlign: 'right' }}>SL</th>
                        <th style={{ textAlign: 'right' }}>Thành tiền</th>
                      </tr>
                    </thead>
                    <tbody>
                      {topSellingFoods.map((item) => (
                        <tr key={item.stt} style={{ borderBottom: '1px solid #f8f8f8' }}>
                          <td style={{ padding: '8px 0', fontWeight: 'bold' }}>{item.stt}</td>
                          <td>{item.foodName}</td>
                          <td style={{ textAlign: 'right', fontWeight: 'bold' }}>{item.quantitySold}</td>
                          <td style={{ textAlign: 'right' }}>{formatCurrency(item.totalRevenue)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  <div className="empty-chart">
                    <span className="empty-chart__icon">☕</span>
                    Chưa có món nào bán ra
                  </div>
                )}
              </div>
            </div>

            <div className="dashboard__side-col">
              <PromoCard />
              <ActivityFeed />
            </div>
          </div>
        </>
      )}
    </div>
  );
}

export default Dashboard;