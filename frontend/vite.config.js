import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: process.env.API_PROXY_TARGET || 'https://localhost:7053',
        changeOrigin: true,
        // ASP.NET's development certificate is only used by this local proxy.
        secure: false,
      },
    },
  },
})
