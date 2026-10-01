import type { CapacitorConfig } from '@capacitor/cli'

const config: CapacitorConfig = {
  appId: 'com.transportapp.app',
  appName: 'TransportApp',
  webDir: 'dist',
  server: {
    // La app corre en http (no https) para poder llamar al backend de desarrollo por HTTP plano sin
    // que el WebView bloquee la petición como "Mixed Content" (HTTPS pidiendo a HTTP). Cuando el
    // backend tenga HTTPS real, esto se puede quitar (o dejar en 'https', su valor por defecto).
    androidScheme: 'http',
  },
}

export default config
