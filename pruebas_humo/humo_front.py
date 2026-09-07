"""Prueba de humo del FRONT Blazor — Innovación curricular.

QUÉ COMPRUEBA Y QUÉ NO — hay que decirlo, porque la limitación es real.

Blazor Server hace las interacciones por una conexión persistente: los clics,
los formularios y los botones NO son peticiones HTTP que un guion pueda
enviar. Este guion, entonces, no puede llenar un formulario como sí lo hace el
de un front hecho de formularios HTML.

Lo que sí comprueba, que es la mitad que importa para cerrar la versión:

  · que cada pantalla RESPONDE por su dirección propia;
  · que el HTML que llega **ya trae los datos de la API** — Blazor los pide en
    el servidor antes de mandar la página, así que si aparecen aquí es que el
    front habló con la API de verdad;
  · que la pantalla NO le habla al usuario en jerga;
  · y lo más importante: que **con la API apagada la pantalla sigue en pie**,
    con su aviso adentro. Eso es lo que demuestra que son dos procesos.

**El guion siembra su propia ficha por la API** —no por la base— y la retira
al final. Así la comprobación de «la pantalla muestra lo que dio la API» no
depende de que alguien haya dejado datos, y la de «sin la API no se ve nada»
tiene algo concreto que no encontrar.

Lo que queda para una persona: llenar el formulario, usar los dos botones de
guardar y retirar una ficha. Está escrito paso a paso en 7_quickstart.md.

La sección 5 **congela** la API con `docker compose pause` en vez de apagarla
con `stop`. La razón está escrita ahí mismo, y vale la pena: apagarla obliga a
recompilarla al encenderla —el contenedor corre `dotnet watch`— y eso tardó
entre 160 segundos y más de cinco minutos según la carga de la máquina. Una
prueba que se pone roja por eso no está midiendo el sistema: está midiendo el
computador.

Uso:  python pruebas_humo/humo_front.py     (parado en la raíz del proyecto)
"""
import html
import json
import random
import re
import string
import subprocess
import time
import urllib.error
import urllib.request

FRONT = "http://localhost:8073"
API = "http://localhost:8072"
SERVICIO_API = "api-innovacion"
fallos = []

# Un sufijo distinto en cada corrida: si una se interrumpe a la mitad deja la
# ficha puesta, y la siguiente fallaría al crearla por llave duplicada.
SUFIJO = "".join(random.choices(string.digits, k=3))
LLAVE = int("99" + SUFIJO)

FICHA = {
    "nit": LLAVE,
    "razonSocial": "Razón social " + SUFIJO,
    "nombreContacto": "Nombre del contacto " + SUFIJO,
    "correo": "Correo " + SUFIJO,
    "telefono": "Teléfono " + SUFIJO,
    "ciudad": "Ciudad " + SUFIJO,
}

# Las columnas que la tabla debe traer, en el idioma del usuario.
ETIQUETAS = ("NIT", "Razón social", "Nombre del contacto", "Correo", "Ciudad")


def ver(url):
    try:
        with urllib.request.urlopen(url, timeout=15) as r:
            return r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")
    except Exception as e:
        return 0, str(e)


def api(metodo, ruta, cuerpo=None):
    """Una petición a la API. El front no se toca por aquí: esto es para
    poner y quitar la ficha con la que después se mira la pantalla."""
    datos = json.dumps(cuerpo).encode() if cuerpo is not None else None
    p = urllib.request.Request(API + ruta, data=datos, method=metodo,
                               headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(p, timeout=15) as r:
            return r.status
    except urllib.error.HTTPError as e:
        return e.code
    except Exception:
        return 0


def visible(pagina: str) -> str:
    """El texto que el usuario VE: sin etiquetas y con las tildes de verdad.

    Comprobar sobre el HTML crudo da falsos positivos de los dos lados: un
    «422» puede aparecer dentro del hash de un archivo estático, y el aviso
    «no está disponible» llega escrito `est&#xE1;`, así que buscar la «á»
    literal no lo encuentra.

    Una prueba de pantalla comprueba **lo que se ve**, no el código fuente.
    """
    sin_script = re.sub(r"(?is)<(script|style)[^>]*>.*?</\1>", " ", pagina)
    sin_etiquetas = re.sub(r"<[^>]*>", " ", sin_script)
    return re.sub(r"\s+", " ", html.unescape(sin_etiquetas))


def revisar(nombre, condicion, detalle=""):
    print(f"{'[OK]    ' if condicion else '[FALLO] '}{nombre} {detalle[:140]}")
    if not condicion:
        fallos.append(nombre)


def esperar_api(segundos=180):
    """Acepta 200 y **204**: un 204 es la API respondiendo que la tabla está
    vacía, que es una respuesta válida — no una API a medio arrancar.

    Hace falta porque este mismo guion la apaga y la enciende en la sección 5,
    y porque el contenedor corre `dotnet watch`: encenderla no es lo mismo que
    estar lista.
    """
    for _ in range(segundos // 3):
        if ver(f"{API}/api/aliado?limite=1")[0] in (200, 204):
            return True
        time.sleep(3)
    return False


if not esperar_api():
    print("La API no respondió. ¿Está levantado el sistema?")
    print("   docker compose up -d --build")
    raise SystemExit(1)

# ----------------------------------------------------------------------
# Se siembra la ficha de la corrida, POR LA API
# ----------------------------------------------------------------------
if api("POST", "/api/aliado", FICHA) != 200:
    print(f"No pude crear la ficha de prueba {LLAVE}. ¿La API está sana?")
    raise SystemExit(1)
print(f"    ficha de prueba {LLAVE} creada por la API\n")

TESTIGO = FICHA["razonSocial"]

print("=== 1. Las pantallas responden, cada una por su dirección ===")
for ruta, titulo in [("/", "Sistema de innovación curricular"),
                     ("/aliados", "Aliados")]:
    c, t = ver(f"{FRONT}{ruta}")
    revisar(f"{ruta:26s} responde y se titula «{titulo}»",
            c == 200 and titulo in visible(t))

print()
print("=== 2. El menú lleva a la pantalla, con una dirección de verdad ===")
c, t = ver(f"{FRONT}/")
revisar("el menú tiene el enlace", 'href="aliados"' in t)
revisar("y NO hay ninguna dirección con el nombre de la tabla como parámetro",
        "{tabla}" not in t and "?tabla=" not in t)

print()
print("=== 3. La pantalla trae los datos que dio la API ===")
c, cuerpo = ver(f"{API}/api/aliado?limite=5")
filas = json.loads(cuerpo)["datos"] if c == 200 else []
c, t = ver(f"{FRONT}/aliados")
revisar("la API responde", len(filas) > 0, f"{len(filas)} filas")
revisar("y la ficha que se acaba de crear se ve en la pantalla",
        str(LLAVE) in visible(t) and str(TESTIGO) in visible(t))
revisar("la tabla trae sus columnas",
        all(x in visible(t) for x in ETIQUETAS),
        str([x for x in ETIQUETAS if x not in visible(t)]))

print()
print("=== 4. Lo que la pantalla NO debe decirle al usuario ===")
# La jerga se busca como TOKEN TÉCNICO, no como palabra suelta: «aliado»
# es un nombre de tabla Y una palabra que el usuario dice todos los días.
# Jerga de verdad es la ruta de la API, los verbos y los motores.
JERGA = ["PUT", "PATCH", "DELETE", "422", "500", "/api/",
         "Dapper", "SQL Server", "endpoint", "localhost:"]
for ruta in ("/", "/aliados"):
    c, t = ver(f"{FRONT}{ruta}")
    visto = [j for j in JERGA if j in visible(t)]
    revisar(f"{ruta:26s} sin jerga", not visto, str(visto))

print()
print("=== 5. LA PRUEBA DE LOS DOS PROCESOS: se deja a la API sin responder ===")
print("    (la pantalla espera su tiempo de espera, unos diez segundos)")
# ======================================================================
# POR QUÉ `pause` Y NO `stop`, QUE ES LO QUE UNO ESCRIBIRÍA
#
# `docker compose stop` + `start` también sirve para dejar la API fuera de
# juego… pero volver a encenderla NO es arrancar un programa ya compilado:
# el contenedor corre `dotnet watch`, así que **recompila**. Medido en esta
# máquina: 160 y 220 segundos según la carga, y una vez ni siquiera volvió
# en cinco minutos. La prueba se ponía roja con el sistema perfectamente
# sano, que es la peor clase de prueba que hay.
#
# `pause` congela el proceso y `unpause` lo descongela, **al instante y sin
# recompilar**. Para lo que aquí se quiere demostrar da igual —y hasta es
# más fiel— que la API esté caída o colgada: en los dos casos el front se
# queda sin datos y tiene que sostenerse solo.
# ======================================================================
subprocess.run(["docker", "compose", "pause", SERVICIO_API],
               capture_output=True, text=True)
time.sleep(2)

c, t = ver(f"{FRONT}/aliados")
texto = visible(t)
revisar("la pantalla SIGUE respondiendo con la API sin responder", c == 200)
revisar("  y muestra el aviso dentro de la aplicación",
        "no está disponible" in texto)
revisar("  con su menú y su marco intactos",
        "Aliados" in texto and "Innovación curricular" in texto)
# SQL Server sigue encendido y con la ficha adentro. Si el front pudiera
# llegar a la base por su cuenta, se seguiría viendo. No se ve.
revisar("  y SIN datos: el front no puede llegar a la base por su cuenta",
        str(TESTIGO) not in texto)

subprocess.run(["docker", "compose", "unpause", SERVICIO_API],
               capture_output=True, text=True)
print("    API descongelada; responde enseguida")
esperar_api(60)
c, t = ver(f"{FRONT}/aliados")
revisar("y al volver la API, la pantalla vuelve a traer los datos",
        str(TESTIGO) in visible(t))

# ----------------------------------------------------------------------
# Se retira la ficha de la corrida
# ----------------------------------------------------------------------
print()
codigo = api("DELETE", f"/api/aliado/{LLAVE}")
revisar("la ficha de prueba se retira al terminar", codigo == 200, f"HTTP {codigo}")
# El borrado es LÓGICO: la fila se queda en la base con activo = 0. Lo que se
# comprueba aquí es que la API ya no la entrega, que es lo que ve el usuario.
revisar("  y la API ya no la entrega",
        api("GET", f"/api/aliado/{LLAVE}") == 404)

print()
if fallos:
    print(f"=== RESULTADO: {len(fallos)} FALLO(S) ===")
    for f in fallos:
        print("   -", f)
    raise SystemExit(1)

print("=== RESULTADO: TODO EN VERDE ===")
print()
print("Falta lo que un guion no puede hacer con Blazor Server: llenar el")
print("formulario y usar los dos botones de guardar. Está en 7_quickstart.md")
print("como recorrido a mano, y lo hace una persona.")
