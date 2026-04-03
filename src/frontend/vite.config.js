import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
     test: {
     globals: true,
     environment: 'jsdom',
     coverage: {
       provider: 'v8',
       reporter: ['text', 'lcov', 'cobertura'],
       reportsDirectory: './coverage',
       thresholds: {
         lines: 70,
         branches: 70,
         functions: 70,
         statements: 70,
       },
     },
   },
})
