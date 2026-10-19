const sharp = require('sharp')
const path = require('path')

const ORIGEN = path.join(__dirname, '..', 'public', 'ChatGPT Image 18 sept 2026, 23_08_13.png')
const RES_DIR = path.join(__dirname, '..', 'android', 'app', 'src', 'main', 'res')

// Recorte cuadrado del emblema (bus + pin + carretera), sin el texto "LFMova" debajo.
const RECORTE = { left: 441, top: 46, width: 614, height: 614 }
const FONDO = { r: 255, g: 255, b: 255 }

const DENSIDADES = {
  mdpi: { icono: 48, foreground: 108 },
  hdpi: { icono: 72, foreground: 162 },
  xhdpi: { icono: 96, foreground: 216 },
  xxhdpi: { icono: 144, foreground: 324 },
  xxxhdpi: { icono: 192, foreground: 432 },
}

async function generar() {
  const emblema = sharp(ORIGEN).extract(RECORTE)

  for (const [densidad, tamanos] of Object.entries(DENSIDADES)) {
    const carpeta = path.join(RES_DIR, `mipmap-${densidad}`)

    // Ícono "legacy" (pre-Android 8): el emblema llena todo el cuadrado.
    const iconoCompleto = await emblema.clone().resize(tamanos.icono, tamanos.icono).png().toBuffer()
    await sharp(iconoCompleto).toFile(path.join(carpeta, 'ic_launcher.png'))
    await sharp(iconoCompleto).toFile(path.join(carpeta, 'ic_launcher_round.png'))

    // Capa "foreground" del ícono adaptativo: el emblema ocupa ~68% del lienzo, centrado, con el
    // mismo blanco de fondo (coincide con ic_launcher_background) para que no se note la costura.
    const ladoEmblema = Math.round(tamanos.foreground * 0.68)
    const emblemaChico = await emblema.clone().resize(ladoEmblema, ladoEmblema).png().toBuffer()
    await sharp({
      create: { width: tamanos.foreground, height: tamanos.foreground, channels: 4, background: { ...FONDO, alpha: 1 } },
    })
      .composite([{ input: emblemaChico, gravity: 'center' }])
      .png()
      .toFile(path.join(carpeta, 'ic_launcher_foreground.png'))

    console.log(`Generado mipmap-${densidad}`)
  }
}

generar().then(() => console.log('Listo')).catch((error) => {
  console.error(error)
  process.exit(1)
})
