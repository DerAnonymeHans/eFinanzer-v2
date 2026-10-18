/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  // All variables of the .env files, not only the VITE_ ones that end up in the browser bundle
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],
    server: {
      // Registered as redirect URI of the Keycloak client and as allowed CORS origin of the backend
      port: 3000,
      strictPort: true,
      // Lets the app use relative /api URLs like in production
      proxy: {
        '/api': {
          target: env.API_PROXY_TARGET || 'http://localhost:5114',
          changeOrigin: true,
        },
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: './src/setupTests.ts',
      // Call history is per test, the mocked return values are set in each test file's beforeEach
      clearMocks: true,
    },
  };
});
