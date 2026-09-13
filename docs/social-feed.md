# Social Feed API

Todas las rutas requieren `Authorization: Bearer <token>`.

## Perfil

- `GET /api/social/profile/me`
- `PUT /api/social/profile/me`

```json
{
  "displayName": "Ana",
  "bio": "Amante de los animales",
  "avatarUrl": "https://example.com/avatar.jpg",
  "latitude": 19.4326,
  "longitude": -99.1332
}
```

## Mascotas

- `GET /api/social/pets`
- `POST /api/social/pets`
- `PUT /api/social/pets/{petId}`
- `DELETE /api/social/pets/{petId}`

## Feed y publicaciones

- `GET /api/social/feed?page=1&pageSize=20`
- `POST /api/social/posts`
- `DELETE /api/social/posts/{postId}`

```json
{
  "content": "Paseo de domingo",
  "petId": "00000000-0000-0000-0000-000000000000",
  "mediaUrl": "https://example.com/photo.jpg"
}
```

## Interacciones

- `POST /api/social/posts/{postId}/likes`
- `DELETE /api/social/posts/{postId}/likes`
- `GET /api/social/posts/{postId}/comments`
- `POST /api/social/posts/{postId}/comments`
- `POST /api/social/users/{userId}/follow`
- `DELETE /api/social/users/{userId}/follow`

La migracion `SocialFeed` crea las tablas y restricciones de este modulo. Los archivos de migracion estan en `backend/src/PetPals.Infrastructure/Persistence/Migrations`.
