import React, { useState } from 'react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';
import './ChartCard.css';

// Hàm rút gọn trục Y (VD: 150.000 -> 150k, 2.000.000 -> 2M)
const formatYAxis = (value) => {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(0)}k`;
  return value;
};

// Hàm định dạng tiền tệ Việt Nam Đầy Đủ cho Tooltip
const formatVND = (value) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value || 0);
};

// Custom Tooltip hiển thị khi di chuột vào cột/điểm trên biểu đồ
const CustomTooltip = ({ active, payload, label }) => {
  if (active && payload && payload.length) {
    const data = payload[0].payload;
    return (
      <div
        style={{
          backgroundColor: 'var(--surface, #ffffff)',
          padding: '10px 14px',
          border: '1px solid var(--line, #e2e8f0)',
          borderRadius: '8px',
          boxShadow: 'var(--shadow-card, 0 4px 12px rgba(0,0,0,0.1))',
          fontSize: '13px',
        }}
      >
        <p style={{ margin: 0, fontWeight: 'bold', color: 'var(--coffee-900, #1e293b)' }}>
          Ngày {label}
        </p>
        <p style={{ margin: '4px 0 0 0', color: 'var(--coffee-700, #2563eb)', fontWeight: '600' }}>
          Doanh thu: {formatVND(data.revenue)}
        </p>
        {data.orderCount !== undefined && (
          <p style={{ margin: '2px 0 0 0', color: 'var(--text-mute, #64748b)', fontSize: '12px' }}>
            Số đơn: <strong>{data.orderCount} đơn</strong>
          </p>
        )}
      </div>
    );
  }
  return null;
};

function ChartCard({
  title = "Doanh thu thuần",
  tooltip,
  value,
  valueSuffix,
  periodOptions = ['7 ngày qua', '30 ngày qua'],
  defaultPeriod = '7 ngày qua',
  onPeriodChange,
  chartData = [],
}) {
  const [chartType, setChartType] = useState('bar'); // 'bar' | 'line'
  const [selectedPeriod, setSelectedPeriod] = useState(defaultPeriod);

  const handlePeriodSelect = (e) => {
    const val = e.target.value;
    setSelectedPeriod(val);
    if (onPeriodChange) {
      onPeriodChange(val);
    }
  };

  return (
    <div className="chart-card">
      {/* Tiêu đề & Chọn khoảng thời gian */}
      <div className="chart-card__head">
        <div className="chart-card__title-wrap">
          <h3>{title}</h3>
          {tooltip && (
            <span className="chart-card__info" title={tooltip}>
              ⓘ
            </span>
          )}
        </div>

        {periodOptions && (
          <select
            className="chart-card__select"
            value={selectedPeriod}
            onChange={handlePeriodSelect}
          >
            {periodOptions.map((opt) => (
              <option key={opt} value={opt}>
                {opt}
              </option>
            ))}
          </select>
        )}
      </div>

      {/* Giá trị tổng */}
      {value !== undefined && (
        <div className="chart-card__value">
          {value}
          {valueSuffix && <span className="chart-card__value-suffix"> {valueSuffix}</span>}
        </div>
      )}

      {/* Chuyển đổi giữa dạng Biểu đồ Cột và Biểu đồ Đường */}
      <div className="chart-card__tabs">
        <button
          className={`chart-card__tab ${chartType === 'bar' ? 'is-active' : ''}`}
          onClick={() => setChartType('bar')}
        >
          📊 Biểu đồ Cột
        </button>
        <button
          className={`chart-card__tab ${chartType === 'line' ? 'is-active' : ''}`}
          onClick={() => setChartType('line')}
        >
          📈 Biểu đồ Đường
        </button>
      </div>

      {/* Vẽ biểu đồ Recharts */}
      <div style={{ flex: 1, width: '100%', minHeight: '220px', marginTop: '10px' }}>
        {!chartData || chartData.length === 0 ? (
          <div className="empty-chart">
            <span className="empty-chart__icon">📈</span>
            <span>Chưa có dữ liệu thống kê</span>
          </div>
        ) : chartType === 'bar' ? (
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--line, #f1f5f9)" />
              <XAxis dataKey="date" tickLine={false} axisLine={{ stroke: 'var(--line, #e2e8f0)' }} style={{ fontSize: '12px' }} />
              <YAxis tickFormatter={formatYAxis} tickLine={false} axisLine={false} style={{ fontSize: '12px' }} />
              <Tooltip content={<CustomTooltip />} />
              <Bar dataKey="revenue" fill="var(--coffee-700, #3b82f6)" radius={[4, 4, 0, 0]} maxBarSize={36} />
            </BarChart>
          </ResponsiveContainer>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="var(--line, #f1f5f9)" />
              <XAxis dataKey="date" tickLine={false} axisLine={{ stroke: 'var(--line, #e2e8f0)' }} style={{ fontSize: '12px' }} />
              <YAxis tickFormatter={formatYAxis} tickLine={false} axisLine={false} style={{ fontSize: '12px' }} />
              <Tooltip content={<CustomTooltip />} />
              <Line
                type="monotone"
                dataKey="revenue"
                stroke="var(--coffee-700, #2563eb)"
                strokeWidth={3}
                dot={{ r: 4, fill: 'var(--coffee-700, #2563eb)' }}
                activeDot={{ r: 6 }}
              />
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  );
}

export default ChartCard;