import axios from 'axios';

const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'https://localhost:7053/api', // Đường dẫn Backend ASP.NET Core
  headers: {
    'Content-Type': 'application/json',
  },
});

axiosClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

axiosClient.interceptors.response.use(response => response, error => {
  if (error.response?.status === 401 && !error.config?.url?.includes('/Auth/login')) {
    localStorage.removeItem('token'); localStorage.removeItem('user');
    if (window.location.pathname !== '/login') window.location.assign('/login');
  }
  if (error.response?.status === 403) window.dispatchEvent(new Event('access-refresh'));
  return Promise.reject(error);
});
export default axiosClient;
