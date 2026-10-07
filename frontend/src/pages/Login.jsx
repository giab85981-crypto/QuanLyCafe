import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Coffee, Eye, EyeOff, LayoutDashboard, ShoppingCart } from 'lucide-react';
import authApi from '../api/authApi';
import { canOpen, landing } from '../utils/staffAccess';

const Login = () => {
  const [formData, setFormData] = useState({
    userName: '',
    passWord: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(true);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const navigate = useNavigate();

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleLogin = async (targetPath) => {
    if (!formData.userName || !formData.passWord) {
      setError('Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!');
      return;
    }

    setError('');
    setLoading(true);

    try {
      const response = await authApi.login(formData);
      const { token, userName, displayName, roleName, permissions } = response.data;

      // Lưu thông tin vào LocalStorage
      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify({ userName, displayName, roleName, permissions }));

      // Chuyển hướng tới trang tương ứng (Dashboard hoặc POS)
      const user = { userName, displayName, roleName, permissions };
      navigate(canOpen(targetPath, user) ? targetPath : landing(user));
    } catch (err) {
      if (err.response && err.response.status === 401) {
        setError(err.response?.data?.message || 'Tài khoản hoặc mật khẩu không chính xác!');
      } else {
        setError('Không thể kết nối tới máy chủ!');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.background}>
      <div style={styles.card}>
        {/* Header Branding */}
        <div style={styles.header}>
          <div style={styles.logoBadge}>
            <Coffee size={28} color="#0070e0" />
          </div>
          <h1 style={styles.brandTitle}>Quản lý cafe</h1>
        </div>

        {/* Form Inputs */}
        <div style={styles.body}>
          {error && <div style={styles.errorAlert}>{error}</div>}

          <div style={styles.inputWrapper}>
            <input
              type="text"
              name="userName"
              value={formData.userName}
              onChange={handleChange}
              placeholder="Tên đăng nhập"
              style={styles.input}
            />
          </div>

          <div style={styles.inputWrapper}>
            <input
              type={showPassword ? 'text' : 'password'}
              name="passWord"
              value={formData.passWord}
              onChange={handleChange}
              placeholder="Mật khẩu"
              style={styles.input}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              style={styles.eyeBtn}
            >
              {showPassword ? <EyeOff size={18} color="#64748b" /> : <Eye size={18} color="#64748b" />}
            </button>
          </div>

          {/* Sub options */}
          <div style={styles.optionsRow}>
            <label style={styles.checkboxLabel}>
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                style={styles.checkbox}
              />
              Duy trì đăng nhập
            </label>
            <a href="#forgot" onClick={(e) => e.preventDefault()} style={styles.forgotLink}>
              Quên mật khẩu?
            </a>
          </div>
        </div>

        {/* Dual Action Buttons at Bottom */}
        <div style={styles.buttonGroup}>
          <button
            type="button"
            disabled={loading}
            onClick={() => handleLogin('/dashboard')}
            style={styles.btnAdmin}
          >
            <LayoutDashboard size={18} />
            {loading ? 'Đang xử lý...' : 'Quản lý'}
          </button>

          <button
            type="button"
            disabled={loading}
            onClick={() => handleLogin('/pos')}
            style={styles.btnPos}
          >
            <ShoppingCart size={18} />
            {loading ? 'Đang xử lý...' : 'Bán hàng'}
          </button>
        </div>
      </div>
    </div>
  );
};

// Style chuẩn theo giao diện KiotViet
const styles = {
  background: {
    // Dùng position: fixed + inset thay vì 100vw/100vh để tránh lệch do thanh cuộn
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundImage: `linear-gradient(rgba(0, 0, 0, 0.45), rgba(0, 0, 0, 0.45)), url('https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?q=80&w=1920&auto=format&fit=crop')`,
    backgroundSize: 'cover',
    backgroundPosition: 'center',
    backgroundRepeat: 'no-repeat',
    fontFamily: "'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
    margin: 0,
    overflow: 'hidden',
  },
  card: {
    width: '400px',
    backgroundColor: '#ffffff',
    borderRadius: '12px',
    boxShadow: '0 12px 32px rgba(0, 0, 0, 0.25)',
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '10px',
    paddingTop: '32px',
    paddingBottom: '16px',
  },
  logoBadge: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '42px',
    height: '42px',
    borderRadius: '50%',
    backgroundColor: '#e6f0fa',
  },
  brandTitle: {
    fontSize: '26px',
    fontWeight: '700',
    color: '#0f172a',
    margin: 0,
  },
  body: {
    padding: '0 32px 24px 32px',
  },
  errorAlert: {
    backgroundColor: '#fef2f2',
    color: '#ef4444',
    padding: '10px 12px',
    borderRadius: '6px',
    fontSize: '13px',
    marginBottom: '16px',
    textAlign: 'center',
    border: '1px solid #fecaca',
  },
  inputWrapper: {
    position: 'relative',
    marginBottom: '16px',
  },
  input: {
    width: '100%',
    height: '44px',
    padding: '0 40px 0 14px',
    fontSize: '14px',
    backgroundColor: '#ffffff', // ép nền trắng, tránh bị theme tối override
    color: '#0f172a',
    border: '1px solid #cbd5e1',
    borderRadius: '8px',
    outline: 'none',
    boxSizing: 'border-box',
    transition: 'border-color 0.2s',
    colorScheme: 'light', // tránh trình duyệt tự tô nền tối theo dark mode hệ thống
  },
  eyeBtn: {
    position: 'absolute',
    right: '12px',
    top: '50%',
    transform: 'translateY(-50%)',
    background: 'none',
    border: 'none',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    padding: 0,
  },
  optionsRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: '8px',
    fontSize: '13px',
  },
  checkboxLabel: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    color: '#334155',
    cursor: 'pointer',
  },
  checkbox: {
    accentColor: '#0070e0',
    cursor: 'pointer',
  },
  forgotLink: {
    color: '#0070e0',
    textDecoration: 'none',
    fontWeight: '500',
  },
  buttonGroup: {
    display: 'flex',
    height: '50px',
  },
  btnAdmin: {
    flex: 1,
    backgroundColor: '#0070e0',
    color: '#ffffff',
    border: 'none',
    fontSize: '15px',
    fontWeight: '600',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '8px',
    transition: 'background-color 0.2s',
  },
  btnPos: {
    flex: 1,
    backgroundColor: '#00b852',
    color: '#ffffff',
    border: 'none',
    fontSize: '15px',
    fontWeight: '600',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: '8px',
    transition: 'background-color 0.2s',
  },
};

export default Login;