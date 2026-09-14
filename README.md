# PetPals 🐾

Red social y marketplace para el mundo de las mascotas: dueños, veterinarias y refugios de animales, todo en una sola plataforma.

## 📋 Descripción general

PetPals es una plataforma web donde los dueños de mascotas pueden compartir momentos con sus animales, las veterinarias pueden ofrecer servicios y vender productos especializados, y los refugios pueden gestionar procesos de adopción, todo conectado en un mismo ecosistema.

Es un proyecto personal, desarrollado en solitario, construido con **arquitectura Web API + frontend separado**.

## 🎯 Objetivo del proyecto

Centralizar en un solo lugar:
- La vida social de las mascotas (posts, momentos, perfiles de animales).
- El acceso a servicios veterinarios (citas, productos especializados).
- Los procesos de adopción gestionados por refugios.
- La cadena de suministro entre refugios y veterinarias.

## 👥 Roles / Tipos de usuario

### 1. Usuario / Dueño de mascota
- Crear perfil de sus mascotas.
- Publicar posts, fotos y momentos (estilo red social).
- Interactuar con otros usuarios (likes, comentarios, seguir perfiles).
- Ver veterinarias cercanas en el mapa.
- Agendar citas veterinarias.
- Comprar productos en la tienda de las veterinarias (vacunas, alimento especializado, etc.).
- Ver animales en adopción publicados por refugios.

### 2. Veterinaria
- Perfil de negocio con ubicación geolocalizada.
- Publicar y gestionar servicios ofrecidos.
- Gestionar agenda de citas.
- Catálogo de productos (vacunas, comida especializada, etc.) — venta a usuarios y a refugios.
- Recibir y gestionar compras de refugios.

### 3. Refugio
- Publicar animales disponibles para adopción, incluyendo historial de vacunación y cuidados.
- Gestionar el proceso de solicitud/aprobación de adopciones.
- Comprar productos (comida, insumos) directamente a las veterinarias.

## 🧩 Módulos principales (alcance completo v1)

1. **Red social de mascotas**
   - Perfiles de usuario y de mascota.
   - Publicaciones (fotos, texto, momentos).
   - Interacciones sociales (likes, comentarios, follow).

2. **Veterinarias**
   - Perfil y ubicación de la veterinaria.
   - Servicios ofrecidos.
   - Sistema de citas (agendar, confirmar, cancelar).
   - Tienda / catálogo de productos.

3. **Refugios y adopciones**
   - Perfil del refugio.
   - Publicación de animales en adopción (con datos de salud/vacunas).
   - Proceso de solicitud de adopción.

4. **Marketplace de productos**
   - Las veterinarias publican y gestionan su propio catálogo de productos (vacunas, alimento, accesorios, etc.), organizado por categorías fijas.
   - Cada veterinaria controla su propio inventario/stock.
   - Pueden comprar tanto usuarios normales como refugios.
   - Sin pasarela de pagos en esta versión: solo catálogo y carrito (sin checkout de pago real).

5. **Geolocalización**
   - Mapa con veterinarias cercanas según la ubicación del usuario, usando **OpenStreetMap + Leaflet**.

## 🏗️ Arquitectura técnica

- **Backend:** ASP.NET Core Web API (C#)
- **Frontend:** aplicación separada (SPA — React/Angular, a definir en detalle durante el desarrollo)
- **Base de datos:** SQL Server
- **ORM:** Entity Framework Core
- **Mapas:** OpenStreetMap + Leaflet; Nominatim para geocoding
- **Chat en tiempo real:** SignalR
- **Patrón de arquitectura:** Clean Architecture (capas: Domain, Application, Infrastructure, API)

### Estructura sugerida del backend

```
PetPals.sln
 ├── PetPals.Domain          # Entidades y lógica de negocio central
 ├── PetPals.Application     # Casos de uso, DTOs, interfaces
 ├── PetPals.Infrastructure  # EF Core, repositorios y servicios externos
 └── PetPals.API             # Controllers, configuración, middlewares
```

## 🗃️ Entidades principales (borrador inicial)

- `User` (dueño de mascota)
- `Pet` (mascota)
- `Post` (publicación en el feed social)
- `Clinic` (veterinaria)
- `ClinicService` (servicio ofrecido por la veterinaria)
- `Appointment` (cita veterinaria)
- `Product` (producto publicado por la veterinaria: nombre, categoría, precio, stock)
- `ProductCategory` (categorías fijas: vacunas, alimento, accesorios, etc.)
- `Shelter` (refugio)
- `AdoptablePet` (animal en adopción)
- `AdoptionRequest` (solicitud de adopción)
- `Order` / `Cart` (compras de usuarios o refugios a veterinarias, sin pago real)
- `Conversation` / `Message` (chat entre veterinaria y clientes)

### Entidades opcionales (si se implementan las features sugeridas)
- `Notification`
- `Review` (calificación de veterinaria, producto o refugio)
- `PetHealthRecord` (historial de vacunas/visitas)
- `LostPetAlert`
- `Follow` (relación seguidor/seguido)
- `Group` / `Community`
- `Coupon`
- `Badge` / `Achievement` (gamificación)
- `Report` (moderación de contenido)
- `AdoptionFollowUp` (seguimiento post-adopción)

## 🗂️ Apartados por rol (dashboards)

Cada tipo de cuenta tiene su propio login y su propio panel:

### Usuario normal
- **Home / Feed:** publicaciones de otros usuarios y sus mascotas.
- **Marketplace:** comprar productos a las veterinarias.
 - **Veterinarias cercanas:** mapa (OpenStreetMap) con las veterinarias cerca de su ubicación.
- **Perfil / Mis mascotas.**

### Veterinaria
- **Marketplace propio:** publicar y gestionar sus productos (crear, editar, controlar stock).
- **Citas:** agenda de servicios reservados por usuarios.
- **Chat:** conversar en tiempo real con los clientes que le escriben.
- **Perfil de negocio:** ubicación, servicios, horarios.

### Refugio
- **Marketplace:** comprarle productos a las veterinarias (comida, insumos).
- **Adopciones:** publicar y gestionar animales disponibles para adopción.
- **Perfil del refugio.**

## 💡 Features sugeridas (propuestas para elevar el proyecto)

Lista amplia de ideas para nutrir el roadmap. No todas van en la v1; quedan aquí como banco de features para ir incorporando.

### Social / Comunidad
- **Chat en tiempo real:** no solo veterinaria-usuario, sino también refugio-usuario (consultas de adopción) y refugio-veterinaria (pedidos). Implementable con SignalR.
- **Stories/momentos temporales** de las mascotas (estilo historias de 24h).
- **Grupos o comunidades** por tipo de mascota, raza o intereses (ej. "Dueños de gatos", "Adiestramiento canino").
- **Sistema de seguidores/seguidos** y feed personalizado según a quién sigues.
- **Etiquetado de mascotas/usuarios** en publicaciones.
- **Alertas de mascota perdida/encontrada:** publicación especial en el feed con geolocalización, visible a usuarios cercanos, con opción de compartir rápido.

### Marketplace / Comercio
- **Reseñas y calificaciones** de veterinarias, productos y refugios, para generar confianza.
- **Favoritos/Wishlist:** guardar productos o animales en adopción para revisar después.
- **Cupones/descuentos** que las veterinarias pueden ofrecer a sus clientes frecuentes.
- **Historial de compras** por usuario/refugio.
- **Comparador de productos** dentro del marketplace (precio, categoría, veterinaria).

### Veterinaria / Salud
- **Ficha de salud digital de la mascota:** historial de vacunas y visitas, alimentada automáticamente desde las citas completadas.
- **Recordatorios automáticos** de próxima vacuna o cita (vía notificación).
- **Expediente compartido entre veterinarias:** si el dueño cambia de veterinaria, puede autorizar el traspaso del historial.
- **Teleconsulta básica** (videollamada o chat con la veterinaria antes de decidir si amerita cita presencial).

### Refugios / Adopción
- **Formulario de solicitud de adopción** con seguimiento de estado (pendiente, en revisión, aprobada, rechazada).
- **Seguimiento post-adopción:** el refugio puede pedir actualizaciones/fotos del animal adoptado tiempo después.
- **Campañas de donación** para refugios (insumos o fondos), aunque el manejo del dinero quede fuera de alcance por ahora.

### Confianza / Seguridad
- **Panel de administración (Admin):** moderar publicaciones reportadas, verificar veterinarias y refugios (badge de "verificado"), gestionar disputas.
- **Sistema de reportes/moderación** para publicaciones o usuarios inapropiados en el feed social.
- **Verificación de identidad** para veterinarias y refugios antes de operar en la plataforma (documentos, licencia).

### Engagement / Extras
- **Sistema de notificaciones** (citas, vacunas, mensajes nuevos, solicitudes de adopción, etc.).
- **Gamificación:** insignias o logros por participación (ej. "Adoptante responsable", "Comunidad activa").
- **Búsqueda avanzada** de veterinarias, productos o animales en adopción con filtros (ubicación, precio, especie, raza).
- **Modo oscuro** y accesibilidad general de la interfaz.





- Pasarela de pagos real o simulada (solo catálogo/carrito en esta versión).
- App móvil nativa (se evaluará a futuro).

## 📌 Estado del proyecto

La v1 funcional está implementada en las ramas de features: autenticación y roles, feed social, marketplace, citas, adopciones, geolocalización y chat en tiempo real. Falta configurar la conexión local a SQL Server, aplicar las migraciones y ejecutar las pruebas end-to-end en el entorno de desarrollo.

Las ideas de la sección de features sugeridas permanecen fuera del alcance de v1 hasta que se prioricen explícitamente.
