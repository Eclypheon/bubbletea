const CACHE_NAME = 'bubbletea-pwa-v1.7.1';

self.addEventListener('install', (e) => {
  self.skipWaiting();
});

self.addEventListener('activate', (e) => {
  e.waitUntil(clients.claim());
});

self.addEventListener('fetch', (e) => {
  const url = new URL(e.request.url);
  // Never intercept large Unity Build assets (.wasm, .data, .framework.js, .loader.js).
  // Intercepting these via service worker stream breaks WebAssembly.instantiateStreaming
  // in WebKit/iOS Safari due to synthetic response stream handling.
  if (
    url.pathname.includes('/Build/') ||
    url.pathname.endsWith('.wasm') ||
    url.pathname.endsWith('.data') ||
    url.pathname.endsWith('.framework.js') ||
    url.pathname.endsWith('.loader.js')
  ) {
    return;
  }

  e.respondWith(
    fetch(e.request).catch(() => caches.match(e.request))
  );
});
