# Marketplace API

Todas las rutas requieren `Authorization: Bearer <token>`.

## Clínicas y catálogo

- `GET /api/marketplace/clinics`
- `GET /api/marketplace/clinics/me` (`Clinic`)
- `PUT /api/marketplace/clinics/me` (`Clinic`)
- `GET /api/marketplace/products?clinicId={id}&category={value}`
- `POST /api/marketplace/products` (`Clinic`)
- `PUT /api/marketplace/products/{productId}` (`Clinic`)
- `DELETE /api/marketplace/products/{productId}` (`Clinic`)

Las categorias usan los valores del enum `ProductCategory`: `0` alimento, `1` vacuna, `2` medicina, `3` accesorio, `4` higiene y `5` otros.

## Carrito y pedidos

Estas rutas estan disponibles para los roles `User` y `Shelter`:

- `GET /api/marketplace/cart`
- `POST /api/marketplace/cart/items`
- `PUT /api/marketplace/cart/items/{cartItemId}`
- `DELETE /api/marketplace/cart/items/{cartItemId}`
- `POST /api/marketplace/cart/checkout`
- `GET /api/marketplace/orders`

El carrito puede mezclar productos de varias clínicas. El checkout crea un `Order` independiente por clínica y descuenta el stock dentro de una transacción.

La versión actual no procesa pagos reales.
