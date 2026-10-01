const sharp = require('sharp')
const path = require('path')

const ORIGEN = path.join(__dirname, '..', 'public', 'ChatGPT Image 18 sept 2026, 23_08_13.png')
const RES_DIR = path.join(__dirname, '..', 'android', 'app', 'src', 'main', 'res')

// Recorte ajustado del logo completo (emblema + "TransportApp" + eslogan), sin el margen blanco sobrante.
const RECORTE_LOGO = { left: 257, top: 134, width: 1023, height: 746 }
const FONDO = { r: 255, g: 255, b: 255, alpha: 1 }

const TAMANOS = {
  'drawable/splash.png': { width: 480, height: 320 },
  'drawable-land-mdpi/splash.png': { width: 480, height: 320 },
  'drawable-land-hdpi/splash.png': { width: 800, height: 480 },
  'drawable-land-xhdpi/splash.png': { width: 1280, height: 720 },
  'drawable-land-xxhdpi/splash.png': { width: 1600, height: 960 },
  'drawable-land-xxxhdpi/splash.png': { width: 1920, height: 1280 },
  'drawable-port-mdpi/splash.png': { width: 320, height: 480 },
  'drawable-port-hdpi/splash.png': { width: 480, height: 800 },
  'drawable-port-xhdpi/splash.png': { width: 720, height: 1280 },
  'drawable-port-xxhdpi/splash.png': { width: 960, height: 1600 },
  'drawable-port-xxxhdpi/splash.png': { width: 1280, height: 1920 },
}

async function generar() {
  const logo = sharp(ORIGEN).extract(RECORTE_LOGO)
  const logoBuffer = await logo.clone().png().toBuffer()

  for (const [rutaRelativa, tamano] of Object.entries(TAMANOS)) {
    // El logo ocupa como máximo el 62% del lado más corto del lienzo, para dejar aire alrededor.
    const ladoCorto = Math.min(tamano.width, tamano.height)
    const anchoLogo = Math.round(ladoCorto * 0.62)
    const logoRedimensionado = await sharp(logoBuffer).resize({ width: anchoLogo, fit: 'inside' }).png().toBuffer()

    await sharp({ create: { width: tamano.width, height: tamano.height, channels: 4, background: FONDO } })
      .composite([{ input: logoRedimensionado, gravity: 'center' }])
      .png()
      .toFile(path.join(RES_DIR, rutaRelativa))

    console.log(`Generado ${rutaRelativa}`)
  }
}

generar().then(() => console.log('Listo')).catch((error) => {
  console.error(error)
  process.exit(1)
})
