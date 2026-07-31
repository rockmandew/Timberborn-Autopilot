import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The dashboard serves on :3000; everything stateful lives in the gateway
// on :8081 (Timberborn itself owns :8080).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': 'http://127.0.0.1:8081',
      '/ws': { target: 'ws://127.0.0.1:8081', ws: true },
    },
  },
})
