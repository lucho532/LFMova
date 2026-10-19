/** Entrega un archivo ya descargado al navegador para que lo guarde con el nombre indicado. */
export function guardarArchivo(nombre: string, archivo: Blob): void {
  const url = URL.createObjectURL(archivo)
  const enlace = document.createElement('a')
  enlace.href = url
  enlace.download = nombre
  document.body.appendChild(enlace)
  enlace.click()
  enlace.remove()
  URL.revokeObjectURL(url)
}
