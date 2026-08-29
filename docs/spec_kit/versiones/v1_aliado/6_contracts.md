# Contratos HTTP — Versión 1: los 7 endpoints exactos

> Base: `http://localhost:8072` · Documentación interactiva en
> `/swagger`. Lo que este documento dice se cumple **al pie de la letra**
> (Artículo 9): un cliente puede exigirlo sin leer el código.

## 0. Convenciones globales

**Sobre de lectura** (listados):

```json
{ "tabla": "aliado", "limite": 1000, "total": 1, "datos": [ … ] }
```

**Sobre de error**:

```json
{ "estado": 422, "mensaje": "Datos inválidos.", "detalle": "…",
  "errores": ["El campo correo es obligatorio."] }
```

`errores[]` aparece **solo** en el 422.

**Los nombres de los campos JSON van en camelCase** (`razonSocial`,
`filasAfectadas`), que es lo que ASP.NET Core hace **por defecto**: no hay
que configurar nada, y por lo tanto no hay nada que se pueda configurar mal.

> **Ojo con la diferencia entre la ruta y el cuerpo.** La ruta es
> `/api/aliado` —nombra la tabla (Artículo 10)— y el cuerpo usa
> `razonSocial`, no `razon_social`. No es una inconsistencia: la ruta
> identifica **el recurso**, y el JSON sigue la convención de quien lo
> consume. **El JSON no es una ventana a la tabla**: si mañana la columna
> se renombra, el contrato no tiene por qué cambiar.

**Catálogo de códigos** (Artículo 10):

| Situación | Código |
|---|---|
| Lectura correcta · escritura correcta | **200** |
| Lectura sin filas activas | **204** (sin cuerpo) |
| Regla de negocio rota (`limite` ≤ 0, `PATCH` sin campos) | **400** |
| Cuerpo inválido: falta un campo, tipo equivocado, texto muy largo | **422** |
| El NIT no existe, o está inactivo | **404** |
| La base rechaza (llave duplicada) o falla | **500** (motor en `detalle`) |

## 1. `GET /` — Diagnóstico

```
GET /
→ 200 { "mensaje": "API Innovación Curricular — módulo de aliados",
        "version": "v1",
        "contratos": "/swagger" }
```

**Sin desenlaces de error, y a propósito:** no recibe parámetros ni cuerpo,
y no consulta la base. Si este endpoint no responde 200, el problema no es
de contrato — es que la API no está arriba.

## 2. `GET /api/aliado[?limite=N]` — Listar

```
GET /api/aliado
→ 204 (sin cuerpo)          ← el ESTADO INICIAL del sistema: no hay aliados

…y una vez creado alguno:
→ 200 { "tabla":"aliado", "limite":1000, "total":1,
        "datos":[ {"nit":900123456,"razonSocial":"Fundación Tecnológica del Norte",
                   "nombreContacto":"Ana Restrepo","correo":"ana@ftn.edu.co",
                   "telefono":"604 555 1234","ciudad":"Medellín"} ] }

GET /api/aliado?limite=1
→ 200 { …, "limite":1, "total":1 }

→ 400 si limite <= 0
```

Devuelve **solo** las filas con `activo = 1`. El campo `activo` **no viaja
en la respuesta**: es un detalle interno, no parte del catálogo.

## 3. `GET /api/aliado/{nit}` — Obtener uno

```
GET /api/aliado/900123456
→ 200 { "nit":900123456, "razonSocial":"Fundación Tecnológica del Norte",
        "nombreContacto":"Ana Restrepo", "correo":"ana@ftn.edu.co",
        "telefono":"604 555 1234", "ciudad":"Medellín" }

GET /api/aliado/999999999          ← no existe
→ 404 { "estado":404, "mensaje":"Aliado no encontrado.",
        "detalle":"No existe un aliado con el NIT 999999999." }
```

Una fila **inactiva** responde igual: 404 (C8).

## 4. `POST /api/aliado` — Crear

Cuerpo (petición `AliadoCrear` — los seis obligatorios):

```
POST /api/aliado
body {"nit":900123456,"razonSocial":"Fundación Tecnológica del Norte",
      "nombreContacto":"Ana Restrepo","correo":"ana@ftn.edu.co",
      "telefono":"604 555 1234","ciudad":"Medellín"}
→ 200 { "estado":200, "mensaje":"Aliado creado exitosamente." }

body {"nit":900123456,"razonSocial":"X"}       ← faltan cuatro campos
→ 422 { "estado":422, "mensaje":"Datos inválidos.",
        "errores":["El campo nombreContacto es obligatorio.",
                   "El campo correo es obligatorio.", …] }

body {"nit":"no-es-un-numero", …}              ← el tipo también es regla
→ 422

body {"nit":900123456, …}                      ← NIT duplicado (PK)
→ 500 con el error del motor en detalle
```

El registro nace con `activo = 1`. **El cuerpo no acepta `activo`**: si
llega, se ignora.

## 5. `PUT /api/aliado/{nit}` — Reemplazo COMPLETO

```
PUT /api/aliado/900123456
body {"razonSocial":"Fundación Tecnológica del Norte S.A.S.",
      "nombreContacto":"Ana Restrepo","correo":"contacto@ftn.edu.co",
      "telefono":"604 555 9999","ciudad":"Bogotá"}
→ 200 { "estado":200, "mensaje":"Aliado reemplazado.", "filasAfectadas":1 }

body {"razonSocial":"X","nombreContacto":"Y","telefono":"Z","ciudad":"W"}
                                               ← falta correo
→ 422 { …, "errores":["El campo correo es obligatorio."] }

PUT /api/aliado/999999999                      ← no existe
→ 404
```

**Los cinco campos son obligatorios**: reemplazar es poner todo de nuevo.
El `nit` no va en el cuerpo — identifica la fila, no se cambia.

## 6. `PATCH /api/aliado/{nit}` — Actualización PARCIAL

```
PATCH /api/aliado/900123456
body {"ciudad":"Cartagena"}                   ← solo lo que cambia
→ 200 { "estado":200, "mensaje":"Aliado actualizado.", "filasAfectadas":1 }

body {"razonSocial":"X","nombreContacto":"Y","telefono":"Z","ciudad":"W"}
                                               ← el MISMO cuerpo que el PUT rechazó
→ 200                                          ← aquí es válido

body {}                                        ← nada que actualizar
→ 400 { "estado":400, "mensaje":"Parámetros inválidos.",
        "detalle":"No se envió ningún campo para actualizar." }

PATCH /api/aliado/999999999
→ 404
```

**Esta pareja es la lección del contrato:** el mismo cuerpo da 422 en `PUT`
y 200 en `PATCH`. Reemplazar exige todo; actualizar, solo lo enviado.

## 7. `DELETE /api/aliado/{nit}` — Eliminar (LÓGICO)

```
DELETE /api/aliado/900123456
→ 200 { "estado":200, "mensaje":"Aliado eliminado.", "filasAfectadas":1 }

DELETE /api/aliado/900123456                  ← segunda vez: ya está inactivo
→ 404

DELETE /api/aliado/999999999                  ← nunca existió
→ 404
```

**La fila no se borra:** queda con `activo = 0` y desaparece de los
listados. Comprobarlo es el criterio 5: el listado **vuelve a responder
204** y la fila sigue en la base.
