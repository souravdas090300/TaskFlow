import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      // Same-origin /api calls in dev -> local API (docker-compose.dev.yml maps 5000:80)
      '/api': 'http://localhost:5000',
      '/health': 'http://localhost:5000',
    },
  },
})
