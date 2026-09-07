# Quickstart — Versión 1: arranque y smoke test

## 1. Arranque

Un solo comando, desde la raíz del proyecto:

```powershell
docker compose up -d --build
```

La primera vez tarda unos minutos: descarga la imagen de SQL Server,
espera a que el motor responda, crea la base con sus 25 tablas y sus
catálogos, y compila la API. Al terminar:

| Qué | Dónde |
|---|---|
| API — diagnóstico | http://localhost:8072/ |
| Documentación interactiva | http://localhost:8072/swagger |
| Listado de aliados | http://localhost:8072/api/aliado |
| SQL Server (SSMS o SQLTools, opcional) | `localhost,11471` · usuario `sa` |

> **¿La contraseña?** Está en el `docker-compose.yml`, a la vista: esta es
> una plantilla didáctica y esa es la excepción declarada en el Artículo 7
> de la [constitución](../../1_constitution.md). **Para correr el sistema
> no hace falta** —el compose se la entrega a los contenedores—; solo se
> necesita para conectarse por fuera con SSMS o SQLTools.
>
> **En su proyecto de aula eso no se copia:** ahí va en un `.env` fuera de
> git, con un `.env.example` adentro.

**Si cambia la contraseña**, no basta con editar el compose:

```powershell
docker compose down -v        # -v borra el volumen: la base olvida el sa viejo
docker compose up -d --build
```

Sin el `-v`, el usuario `sa` sigue existiendo dentro del volumen con la
clave anterior y el login falla — con un error que no menciona los
volúmenes por ninguna parte.

## 2. Smoke test

Los comandos van **numerados igual que los criterios de aceptación** de
[2_spec.md](2_spec.md). Si los siete pasan, la versión está terminada.

```powershell
# 1. Un solo comando: la API responde y dice qué versión es
curl http://localhost:8072/
#    → {"mensaje":"...","version":"v1","contratos":"/swagger"}

# 2. El sistema arranca VACÍO: sin aliados, el listado responde 204
curl -i http://localhost:8072/api/aliado
#    → HTTP 204, sin cuerpo. Vacío no es error.

# 3. Crear y listar
curl -X POST http://localhost:8072/api/aliado `
  -H "Content-Type: application/json" `
  -d '{"nit":900123456,"razonSocial":"Fundacion Tecnologica del Norte","nombreContacto":"Ana Restrepo","correo":"ana@ftn.edu.co","telefono":"604 555 1234","ciudad":"Medellin"}'
#    → 200 creado
curl http://localhost:8072/api/aliado
#    → 200 con total: 1

# 4. El ciclo de los cinco verbos
curl -X PUT http://localhost:8072/api/aliado/900123456 `
  -H "Content-Type: application/json" `
  -d '{"razonSocial":"Fundacion Tecnologica del Norte S.A.S.","nombreContacto":"Ana Restrepo","correo":"contacto@ftn.edu.co","telefono":"604 555 9999","ciudad":"Bogota"}'
#    → 200 filasAfectadas: 1

curl -X PATCH http://localhost:8072/api/aliado/900123456 `
  -H "Content-Type: application/json" -d '{"ciudad":"Cartagena"}'
#    → 200 filasAfectadas: 1

curl http://localhost:8072/api/aliado/900123456
#    → el aliado con la razón social nueva y ciudad Cartagena

# 4b. La pareja que enseña la diferencia: MISMO cuerpo, dos verbos
curl -i -X PUT http://localhost:8072/api/aliado/900123456 `
  -H "Content-Type: application/json" `
  -d '{"razonSocial":"X","nombreContacto":"Y","telefono":"Z","ciudad":"W"}'
#    → 422: al PUT le falta 'correo' y reemplazar exige todo

curl -i -X PATCH http://localhost:8072/api/aliado/900123456 `
  -H "Content-Type: application/json" `
  -d '{"razonSocial":"X","nombreContacto":"Y","telefono":"Z","ciudad":"W"}'
#    → 200: al PATCH le basta con lo enviado

# 5. El borrado es LÓGICO, y se comprueba
curl -X DELETE http://localhost:8072/api/aliado/900123456
#    → 200 filasAfectadas: 1
curl -i http://localhost:8072/api/aliado
#    → 204 otra vez: el único aliado desapareció del listado
curl -i -X DELETE http://localhost:8072/api/aliado/900123456
#    → 404: para la API ya no existe

#    …pero la fila SIGUE en la base. Comprobarlo:
docker compose exec sqlserver bash -c '/opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d innovacion_local `
  -Q "SELECT nit, activo FROM aliado"'
#    → 900123456 | 0

# 6. La validación es la frontera: nada de esto llega a la base
curl -i -X POST http://localhost:8072/api/aliado `
  -H "Content-Type: application/json" `
  -d '{"nit":900999999,"razonSocial":"X","nombreContacto":"Y","telefono":"Z","ciudad":"W"}'
#    → 422 con errores: falta correo

curl -i -X POST http://localhost:8072/api/aliado `
  -H "Content-Type: application/json" `
  -d '{"nit":"no-es-un-numero","razonSocial":"X","nombreContacto":"Y","correo":"a@b.co","telefono":"Z","ciudad":"W"}'
#    → 422: el tipo también es regla

#    (para el 500 del NIT duplicado, cree uno y repita el mismo POST)

# 7. La prueba de capas: sin base de datos
docker compose exec api-innovacion dotnet run --project pruebas
#    → todas las verificaciones pasan, con un repositorio de mentiras
```

## 3. Regresión

Esta es la primera versión: no hay nada anterior que probar. **Desde la
v2**, esta sección conserva los smokes de todas las versiones cerradas y
todos deben seguir pasando antes de cerrar la nueva.

## 4. Si algo falla

| Síntoma | Causa probable |
|---|---|
| `Login failed for user 'sa'` | Se cambió la contraseña sin `docker compose down -v` (§1) |
| La API responde 500 en todo, con "No address associated with hostname" | La API arrancó antes que la base. `docker compose restart api-innovacion` |
| El listado responde 200 con `total: 0` en vez de 204 | El controlador no está devolviendo `NoContent()` cuando la lista viene vacía (RF1) |
| El contenedor de SQL Server se reinicia solo | Contraseña que no cumple la política (8+ caracteres, mayúscula, minúscula, dígito y símbolo) o poca memoria: pide ~2 GB |
| Un inactivo aparece en el listado | A alguna consulta le falta `WHERE activo = 1` ([3_plan](3_plan.md) §4.2) |
| `bad interpreter: /bin/bash^M` | `db/init.sh` se guardó con finales de línea de Windows. Es lo que previene `*.sh text eol=lf` en `.gitattributes` |

---

## Y la pantalla

Los criterios de arriba son de la API. **La versión no está cerrada sin su
pantalla** (Artículo 1.1), y la pantalla se comprueba de dos maneras.

### Con el guion, que hace la mitad automática

```powershell
python pruebas_humo/humo_front.py
```

Comprueba que cada pantalla responde por su dirección, que **el HTML ya trae
los datos de la API**, que no hay jerga, y —lo que importa— **apaga la API** y
verifica que la pantalla siga en pie, con su aviso y sin un solo dato.

> Tarda **varios minutos**, y no está colgada: para volver a encender la API
> hay que esperar a que `dotnet watch` la recompile.

### A mano, que es la mitad que ningún guion puede hacer

Blazor Server manda los clics por una conexión persistente, no como peticiones
HTTP sueltas: un guion no puede llenar el formulario. Esto sí lo hace una
persona, en **http://localhost:8073**:

1. entrar a **Aliados**: se ve el listado (o el recuadro de «todavía no
   hay», si la tabla está vacía — **vacío no es error**);
2. **Agregar** una ficha, llenarla y guardar: aparece el aviso verde y la
   ficha entra en la tabla;
3. **Editar** esa ficha, borrar un campo obligatorio y oprimir **«Guardar la
   ficha completa»**: se rechaza, el motivo sale **en español**, y **lo que
   usted escribió sigue ahí**;
4. con el mismo formulario a medio llenar, oprimir **«Guardar solo lo que
   cambié»**: ahora sí guarda. Ésa es la diferencia entre reemplazar y
   actualizar, vista desde el lado del usuario — y no la decide ningún `if`,
   la decide qué se envía;
5. **Retirar** la ficha: pregunta antes, y después desaparece del listado. Y
   **sigue en la base** con `activo = 0`, porque el borrado es lógico
   (Artículo 6).

Fíjese en lo que **no** aparece en ninguna de esas cinco pantallas: ni `PUT`,
ni `PATCH`, ni un número de estado, ni el nombre de la tabla.
