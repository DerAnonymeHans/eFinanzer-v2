// Dev server only: forwards API calls to the backend (http launch profile), so the app can use relative URLs like in
// production. CRA loads this file with Node, so it has to stay plain JavaScript.
// Replaces the "proxy" field in package.json, which breaks the dev server (allowedHosts error) in react-scripts 5.
const { createProxyMiddleware } = require('http-proxy-middleware');

module.exports = function (app) {
  app.use(
    createProxyMiddleware('/api', {
      target: process.env.API_PROXY_TARGET || 'http://localhost:5114',
      changeOrigin: true,
    }),
  );
};
