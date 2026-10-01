import axios from 'axios';

const axiosClient = axios.create({
  baseURL: 'https://localhost:7053/api', // Đường dẫn Backend ASP.NET Core
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

export default axiosClient;