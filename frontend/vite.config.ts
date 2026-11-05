import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    // The IDE API (launchSettings.json) keeps the auth cookie working through the same origin
    proxy: {
      '/api': 'http://localhost:5063',
    },
  },
})
