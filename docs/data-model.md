# PetPals - Modelo de datos v1

El diagrama en formato DBML esta en [`data-model.dbml`](./data-model.dbml). Puedes pegar su contenido directamente en [dbdiagram.io](https://dbdiagram.io) o importar el archivo.

## Reglas importantes

- Cada usuario tiene un unico rol: `User`, `Clinic` o `Shelter`.
- Una veterinaria y un refugio tienen un unico usuario propietario en v1.
- Un carrito puede contener productos de varias veterinarias.
- Al confirmar un carrito, se crea un pedido por veterinaria.
- `OrderItem` conserva el nombre y precio del producto al momento de la compra.
- `ProductCategory` es un enum porque las categorias son fijas en v1.
- `Pet` representa mascotas de usuarios; `AdoptablePet` representa animales publicados por refugios.
- Un usuario no puede repetir likes ni follows sobre el mismo objetivo.
- Una conversacion puede incluir usuarios de cualquier rol permitido.

## Entidades del dominio

### Identidad y perfiles

- `ApplicationUser`: usuario de ASP.NET Core Identity, almacenado en Infrastructure.
- `UserProfile`: datos publicos del usuario.
- `Clinic`: perfil de veterinaria.
- `Shelter`: perfil de refugio.

### Social

- `Pet`
- `Post`
- `Comment`
- `PostLike`
- `Follow`

### Veterinaria

- `ClinicService`
- `ClinicSchedule`
- `Appointment`

### Marketplace

- `Product`
- `Cart`
- `CartItem`
- `Order`
- `OrderItem`

### Adopciones

- `AdoptablePet`
- `VaccinationRecord`
- `AdoptionRequest`

### Chat

- `Conversation`
- `ConversationParticipant`
- `Message`

Las funcionalidades opcionales, como reseñas, notificaciones, reportes y gamificacion, quedan fuera de este modelo inicial.
