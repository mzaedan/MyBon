const CACHE_VERSION = 'mybon-v4';
const STATIC_CACHE = `${CACHE_VERSION}-static`;
const OFFLINE_URL = '/offline.html';
const PRECACHE_URLS = [
    OFFLINE_URL,
    '/manifest.webmanifest',
    '/img/favicon.png',
    '/img/icon-192.png',
    '/img/icon-512.png'
];

// Pages and API responses hold per-account data, so only static assets are cached.
function isStaticAsset(url) {
    return url.origin === self.location.origin
        && (url.pathname.startsWith('/lib/')
            || url.pathname.startsWith('/css/')
            || url.pathname.startsWith('/js/')
            || url.pathname.startsWith('/img/')
            || url.pathname.startsWith('/lottie/'));
}

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then(cache => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys.filter(key => key.startsWith('mybon-') && key !== STATIC_CACHE)
                    .map(key => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);

    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(() => caches.match(OFFLINE_URL))
        );
        return;
    }

    if (isStaticAsset(url)) {
        event.respondWith(
            caches.match(request).then(cached => {
                if (cached) {
                    return cached;
                }

                return fetch(request).then(response => {
                    if (response.ok) {
                        const copy = response.clone();
                        caches.open(STATIC_CACHE).then(cache => cache.put(request, copy));
                    }
                    return response;
                });
            })
        );
    }
});
