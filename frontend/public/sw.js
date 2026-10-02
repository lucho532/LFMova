// Service worker mínimo de LFMova: existe solo para que el navegador permita
// instalar la versión web como aplicación de escritorio. No guarda nada en
// caché a propósito: cada despliegue de la web llega al instante y la app
// nunca muestra datos de operación viejos. Sin conexión, se comporta igual
// que la web normal. Tampoco intercepta peticiones (no hay manejador de
// "fetch"): las llamadas a la API van directas, como sin service worker.

self.addEventListener('install', () => self.skipWaiting())

self.addEventListener('activate', (evento) => evento.waitUntil(self.clients.claim()))
