import type { CapacitorConfig } from '@capacitor/cli'

const config: CapacitorConfig = {
  appId: 'com.lfmova.app',
  appName: 'LFMova',
  webDir: 'dist',
  // La app se sirve dentro del teléfono en https://localhost (valor por defecto de Capacitor) y la
  // API de producción también es HTTPS: no hay tráfico sin cifrar ni excepciones de red.
}

export default config
