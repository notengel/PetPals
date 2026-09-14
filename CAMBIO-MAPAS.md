# Cambio de proveedor de mapas — PetPals

Vamos a cambiar la solución de geolocalización del proyecto:

- ❌ **Antes:** Google Maps API
- ✅ **Ahora:** OpenStreetMap + Leaflet

## Motivo del cambio

Google Maps Platform requiere tarjeta de crédito registrada (aunque tenga capa gratuita con créditos mensuales). Para este proyecto personal, se prefiere una solución 100% gratuita y sin necesidad de facturación: **OpenStreetMap** como proveedor de datos de mapas, y **Leaflet** (con `react-leaflet` en el frontend) como librería para renderizarlos.

## Qué cambia técnicamente

- **Mapa base:** en vez de la Maps JavaScript API de Google, se usan los tiles de OpenStreetMap.
- **Librería frontend:** `react-leaflet` (sobre Leaflet.js) en lugar del SDK de Google Maps para React.
- **Geocoding** (buscar/convertir direcciones a coordenadas): se usa **Nominatim**, el servicio de geocoding gratuito de OpenStreetMap, en lugar de la Geocoding API de Google.
- **No se requiere API key** para el mapa base ni para Nominatim (Nominatim solo pide respetar límites de uso razonables y agregar un User-Agent identificable en las peticiones).
- La ubicación del usuario para calcular veterinarias cercanas se sigue obteniendo igual, vía la Geolocation API del navegador (`navigator.geolocation`) — eso no cambia.

## Impacto en el resto del proyecto

- No afecta el modelo de datos: `Clinic` sigue guardando `latitude`/`longitude` igual que antes.
- No afecta la lógica de "veterinarias cercanas" (cálculo de distancia), solo cambia cómo se dibuja el mapa y cómo se resuelven direcciones a coordenadas.
- Aplica al módulo de **Usuario normal → Veterinarias cercanas** definido en el README principal.

## Instrucciones para el desarrollo

A partir de ahora, cuando se implemente el módulo de mapas (`feature/maps`), usar:

```bash
npm install leaflet react-leaflet
```

Y para el geocoding, consumir la API pública de Nominatim:
```
https://nominatim.openstreetmap.org/search?q=<direccion>&format=json
```

No usar ninguna dependencia ni referencia al SDK de Google Maps en el código de este módulo.
