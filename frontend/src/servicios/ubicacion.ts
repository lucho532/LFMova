import { Capacitor } from '@capacitor/core'
import { AndroidSettings, IOSSettings, NativeSettings } from 'capacitor-native-settings'

/** Punto GPS real del dispositivo. */
export interface PosicionGps {
  latitud: number
  longitud: number
}

/** Qué impidió obtener la ubicación o usar la cámara, para decidir qué ajuste del sistema ofrecer. */
export type MotivoPermiso = 'denegado' | 'gpsApagado' | 'sinSoporte' | 'camaraDenegada'

/**
 * Permiso o servicio del dispositivo que la app necesita y no está disponible. Lleva un mensaje ya
 * redactado para el usuario; no decide qué hacer con él (eso corresponde a quien lo captura).
 */
export class ErrorPermiso extends Error {
  readonly motivo: MotivoPermiso

  constructor(motivo: MotivoPermiso, mensaje: string) {
    super(mensaje)
    this.name = 'ErrorPermiso'
    this.motivo = motivo
  }
}

const MENSAJES: Record<MotivoPermiso, string> = {
  denegado: 'La aplicación no tiene permiso de ubicación. Sin ella no se puede continuar: concédelo en la configuración.',
  gpsApagado: 'No se pudo obtener tu ubicación. Revisa que el GPS del teléfono esté encendido y vuelve a intentar.',
  sinSoporte: 'Este dispositivo no permite obtener la ubicación.',
  camaraDenegada: 'La aplicación no tiene permiso para usar la cámara. Concédelo en la configuración para tomar la foto.',
}

/** Crea el error de permiso con el mensaje estándar de su motivo. */
export function crearErrorPermiso(motivo: MotivoPermiso): ErrorPermiso {
  return new ErrorPermiso(motivo, MENSAJES[motivo])
}

/**
 * Pide el punto GPS actual. La ubicación es obligatoria para registrar inicio y fin de ruta, llamadas e
 * incidencias, así que nunca resuelve con un valor vacío: si no se puede, rechaza con un `ErrorPermiso`.
 * `maximumAge` permite aceptar un punto reciente cuando hacer esperar al usuario no vale la pena.
 */
export function obtenerUbicacionObligatoria(opciones: { maximumAge?: number; timeout?: number } = {}): Promise<PosicionGps> {
  return new Promise((resolver, rechazar) => {
    if (!navigator.geolocation) {
      rechazar(crearErrorPermiso('sinSoporte'))
      return
    }
    navigator.geolocation.getCurrentPosition(
      (posicion) => resolver({ latitud: posicion.coords.latitude, longitud: posicion.coords.longitude }),
      (fallo) => rechazar(crearErrorPermiso(fallo.code === fallo.PERMISSION_DENIED ? 'denegado' : 'gpsApagado')),
      { enableHighAccuracy: true, timeout: opciones.timeout ?? 15000, maximumAge: opciones.maximumAge ?? 0 },
    )
  })
}

/** Verdadero si la app corre empaquetada en el teléfono, donde sí se puede abrir la configuración del sistema. */
export function puedeAbrirConfiguracion(): boolean {
  return Capacitor.isNativePlatform()
}

/**
 * Abre la pantalla de ajustes del sistema que corresponde al motivo: la del GPS si está apagado, y la
 * ficha de la aplicación (permisos) en los demás casos. Solo funciona en la app instalada; en el
 * navegador el permiso se cambia desde el candado de la barra de direcciones.
 */
export async function abrirConfiguracion(motivo: MotivoPermiso): Promise<void> {
  if (!puedeAbrirConfiguracion()) return
  await NativeSettings.open({
    optionAndroid: motivo === 'gpsApagado' ? AndroidSettings.Location : AndroidSettings.ApplicationDetails,
    optionIOS: IOSSettings.App,
  })
}
