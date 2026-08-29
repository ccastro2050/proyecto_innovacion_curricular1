# Especificación — Versión 1: `aliado` + SQL Server

> **Versión 1** ([mapa](../0_mapa_versiones.md)) · La primera rebanada
> vertical del módulo Innovación Curricular: una tabla, sus siete endpoints
> y las tres capas completas. Ante conflicto con este documento, manda la
> [constitución](../../1_constitution.md).

## 1. Propósito de la v1

Construir la API del catálogo de **aliados** —las entidades externas con
las que la universidad establece alianzas— de punta a punta: controlador,
servicio, repositorio e interfaces, contra SQL Server y en un solo comando.

La v1 no busca cubrir el módulo: busca **dejar el patrón montado y
verificado**. Las demás tablas sin clave foránea son este mismo patrón con
otros nombres.

## 2. Alcance

**Incluye**

- El CRUD completo de `aliado`: listar (con límite), obtener por NIT,
  crear, reemplazar, actualizar parcialmente y eliminar.
- **Borrado lógico**: `DELETE` marca `activo = 0` y los listados filtran
  los inactivos (Artículo 6).
- Un endpoint de diagnóstico y la documentación interactiva en `/swagger`.
- La prueba de capas: el servicio corriendo con un repositorio de mentiras,
  sin base de datos.

**NO incluye** — y no se anticipa nada de esto (Artículo 1)

- Ninguna otra tabla de las 25, aunque existan todas en la base.
- Claves foráneas, listas desplegables ni integridad referencial: eso es
  la v2. En particular, `alianza` —que relaciona un aliado con un
  programa— pertenece a esa versión.
- Autenticación, JWT, roles ni usuarios: eso es la v3.
- Frontend, dashboard ni consultas multitabla: eso es la v4.
- **Reactivar** un registro inactivo (`activo = 1`). Nadie lo pidió.
- Validar el formato del correo o del teléfono más allá de que estén
  presentes: sería una regla de negocio que ninguna versión ha pedido.

## 3. Requisitos funcionales

### RF1 — Listar aliados (GET + query string)
`GET /api/aliado` → 200 con el sobre `{tabla, limite, total, datos:[…]}`.
- Devuelve **solo los activos**.
- Parámetro opcional `limite` (entero > 0; por defecto 1000).
- Sin filas activas → **204** sin cuerpo. **Este es el estado inicial del
  sistema**: la tabla arranca vacía.

### RF2 — Obtener por NIT (GET + parámetro de ruta)
`GET /api/aliado/{nit}` → 200 con el aliado.
- El `nit` es el número de identificación tributaria, **entero**.
- Inexistente **o inactivo** → 404.

### RF3 — Crear (POST + cuerpo completo)
`POST /api/aliado` con `{nit, razonSocial, nombreContacto, correo, telefono, ciudad}`.
- Los seis campos son obligatorios.
- Nace con `activo = 1`.
- NIT ya existente → 500 (lo rechaza la llave primaria).

### RF4 — Reemplazar (PUT + cuerpo completo)
`PUT /api/aliado/{nit}` con los cinco campos que no son la llave.
- **Los cinco son obligatorios**: es un reemplazo. Falta uno → 422.
- Devuelve `filasAfectadas`; inexistente → 404.

### RF5 — Actualizar parcialmente (PATCH + cuerpo parcial)
`PATCH /api/aliado/{nit}` con los campos que se quieran cambiar.
- Solo se modifican los enviados.
- Cuerpo vacío → 400 (no hay nada que actualizar).
- Devuelve `filasAfectadas`; inexistente → 404.

### RF6 — Eliminar (DELETE, borrado lógico)
`DELETE /api/aliado/{nit}` marca `activo = 0`.
- Devuelve `filasAfectadas`.
- Inexistente **o ya inactivo** → 404.
- La fila **no desaparece** de la base.

### RF7 — Diagnóstico
`GET /` → JSON con mensaje, versión (`"v1"`) y la ruta de los contratos.

## 4. Requisitos no funcionales

- **Un solo comando**: `docker compose up -d --build` (Artículo 4).
- **Tres capas con interfaces** y solo el ensamblador conociendo clases
  concretas (Artículo 3).
- **SQL a mano y siempre parametrizado** (`@parametro`), sin ORM de
  entidades (Artículo 2).
- **Todo en español** (Artículo 8).
- La API publica su documentación interactiva en `/swagger`.

## 5. Criterios de aceptación

1. **Un solo comando.** `docker compose up -d --build` deja corriendo SQL
   Server —con la base creada y sus 25 tablas— y la API.
   `GET http://localhost:8072/` responde el diagnóstico con
   `"version":"v1"`.
2. **El sistema arranca vacío.** `GET /api/aliado` responde **204 sin
   cuerpo**: no hay aliados todavía, y vacío no es error.
3. **Crear y listar.** Un `POST` con los seis campos responde 200; después,
   `GET /api/aliado` responde **200 con `total: 1`** y el aliado creado.
4. **Ciclo de los cinco verbos.** `POST` crea el NIT `900123456` → `PUT` lo
   reemplaza completo → `PATCH` le cambia solo `ciudad` → `GET` lo confirma
   → `DELETE` lo desactiva, y un **segundo** `DELETE` responde **404**.
   Además, un `PUT` sin el campo `correo` responde **422** mientras el
   **mismo cuerpo** enviado por `PATCH` responde **200** — la diferencia
   entre reemplazar y actualizar.
5. **El borrado es lógico, y se verifica.** Después del `DELETE` el listado
   vuelve a responder **204**, **y la fila sigue en la base** con
   `activo = 0` (comprobable con una consulta directa).
6. **La validación es la frontera.** `POST` sin `correo` → **422** con
   `errores:[…]`; `POST` con un `nit` que no es un número → **422**;
   `POST` con un NIT que ya existe → **500** con el error del motor en
   `detalle`. En ninguno de los tres casos se toca la base.
7. **Prueba de capas.** El proyecto `pruebas/` ejecuta el servicio con un
   **repositorio de mentiras** —otra implementación de la misma interfaz,
   con una lista en memoria— y todas sus verificaciones pasan **con SQL
   Server apagado**.

## 6. Clarificaciones

> **Qué es esta sección:** el registro de las ambigüedades detectadas
> ANTES de planear, con la respuesta acordada y su razón. Es la
> **compuerta 1** del método: mientras quede un `[NECESITA ACLARACIÓN: …]`
> en los requisitos de arriba, esta versión no pasa a la planeación.

| # | La pregunta | La respuesta, con su razón | Dónde quedó |
|---|---|---|---|
| C1 | El script declara `area_conocimiento.id` como `INT`, pero los datos del Excel son códigos como `1A01`. ¿Cuál manda? | **Mandan los datos: `VARCHAR(6)`.** Un entero no puede guardar `1A01`, así que el script no podría cargar su propio catálogo. Arrastra a `programa_ac.area_conocimiento`, que lo referencia | `db/innovacion_curricular.sql` |
| C2 | `area_conocimiento.disciplina` está declarada `VARCHAR(60)` y el valor más largo del Excel tiene **124** caracteres | **Se agranda a `VARCHAR(150)`.** Recortar un catálogo oficial lo falsea | `db/innovacion_curricular.sql` |
| C3 | `programa.nombre` está declarado `VARCHAR(60)` y el nombre de programa más largo tiene **92** | **Se agranda a `VARCHAR(150)`**, por la misma razón | `db/innovacion_curricular.sql` |
| C4 | Ninguna tabla del módulo trae columna `activo`, pero la metodología exige borrado lógico | **Se agrega `activo BIT NOT NULL DEFAULT 1`** a las 22 tablas del módulo. Sin ella, la versión contradice su propia rúbrica | Artículo 6 · RF6 |
| C5 | El catálogo trae **"Cienias Naturales"** (sin la `c`) en 48 de las 218 filas | **Se corrige** al generar las semillas, y queda anotado en la cabecera del script. Es un error de digitación de la fuente; cargarlo tal cual lo perpetúa en pantalla y en los informes | `db/innovacion_curricular.sql` |
| C6 | El Excel trae 35 facultades y 191 programas, pero sus tablas exigen columnas que el Excel no tiene y que no admiten nulos (`fecha_fun`; `nivel`, `numero_cohortes`, `ciudad`…). ¿Se rellenan o se dejan vacías? | **Se dejan vacías.** Rellenar seis columnas obligatorias para 191 programas sería **inventar datos**. Son tablas de la v2: esa versión decidirá si completa el catálogo o si relaja las restricciones | `db/innovacion_curricular.sql` · `5_data_model` §3 |
| C7 | La tabla `aliado` arranca **sin una sola fila**. ¿Es un problema para la v1? | **No: es una ventaja.** Permite que el smoke test recorra el ciclo completo desde el estado inicial —204, crear, listar, borrar, 204— y ejercite el **204 del listado vacío**, que una tabla llena nunca deja probar | RF1 · criterios 2 y 5 |
| C8 | Un registro inactivo, ¿se puede consultar por su NIT? | **No: responde 404.** Si el listado los filtra, individualmente tampoco existen. Ser coherente importa más que ser permisivo | RF2 · RF6 |
| C9 | ¿Y un segundo `DELETE` sobre el mismo NIT? | **404**, por consecuencia directa de C8: para la API ya no existe | RF6 · criterio 4 |
| C10 | `?limite=0` o negativo, ¿es 422 o 400? | **400.** La forma del dato es correcta —sí es un entero—; lo que se rompe es una regla de negocio. El 422 se reserva para el cuerpo mal formado | RF1 · Artículo 10 |
| C11 | Crear con un NIT que ya existe, ¿409 o 500? | **500**, con el error del motor en `detalle`. En la v1 la llave la defiende la base, no la API | RF3 · criterio 6 |
| C12 | ¿Se valida que el correo tenga formato de correo? | **No en la v1.** Solo que esté presente. Validar el formato es una regla de negocio que nadie pidió, y dejaría fuera casos legítimos | §2 Alcance |

## 7. Definición de TERMINADA

La v1 está terminada —y solo entonces se escribe la spec de la v2— cuando:

1. Los **7 criterios de aceptación** pasan, verificados con el smoke test
   de [7_quickstart.md](7_quickstart.md) **corrido por una persona**.
2. La lista de [9_checklist.md](9_checklist.md) está en verde y firmada.
3. No queda ningún `[NECESITA ACLARACIÓN: …]` en este documento.
4. Se hace commit y **tag `v1`** (Artículo 1).
