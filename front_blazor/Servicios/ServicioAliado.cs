using System.Net.Http.Json;
using System.Text.Json;

namespace FrontInnovacion.Servicios;

/// <summary>
/// Aliado, tal como el front lo maneja.
///
/// **Es una clase del front, no de la API.** Se parece a la de allá porque el
/// contrato es el mismo, y aun así son dos clases distintas en dos proyectos
/// distintos: si compartieran una biblioteca, los dos procesos dejarían de ser
/// independientes y el front podría romperse por un cambio interno de la API.
///
/// Lo único que los une es el JSON.
/// </summary>
public class Aliado
{
    /// <summary>NIT</summary>
    public int Nit { get; set; }

    /// <summary>Razón social</summary>
    public string RazonSocial { get; set; } = string.Empty;

    /// <summary>Nombre del contacto</summary>
    public string NombreContacto { get; set; } = string.Empty;

    /// <summary>Correo</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Teléfono</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Ciudad</summary>
    public string Ciudad { get; set; } = string.Empty;
}

/// <summary>
/// Lo que devuelve cada operación: si salió bien, qué trajo, y qué errores hay
/// que mostrar.
///
/// Existe para que las páginas **no vean códigos de estado**. Una página
/// pregunta «¿salió bien?», no «¿fue 200 o 204?».
/// </summary>
public record Resultado<T>(bool Ok, T? Datos, List<string> Errores)
{
    public static Resultado<T> Bien(T datos) => new(true, datos, new());
    public static Resultado<T> Mal(List<string> errores) => new(false, default, errores);
}

/// <summary>
/// ==========================================================================
/// LA CAPA DE DATOS DEL FRONT — y por qué es de `aliado` y no «de cualquier
/// tabla»
/// ==========================================================================
///
/// Este servicio es al front lo que el repositorio es a la API: la ÚNICA pieza
/// que sabe dónde viven los datos —en la API, nunca en la base de datos— y la
/// única que habla HTTP.
///
/// **Y es específico de un recurso, no genérico.** Podría escribirse un
/// `ApiService.Listar("aliado")` que sirviera para cualquier tabla, y sería
/// más corto. No se hace, por el Artículo 10.1 y por lo mismo que del lado de
/// la API: un método `Listar(string tabla)` no le dice a nadie qué recursos
/// existen, y el compilador deja de revisar si esa tabla es una de las que hay.
///
/// Cuando el proyecto tenga más recursos habrá un servicio como este por cada
/// uno. Se van a parecer mucho — y cada uno va a decir sus campos, sus
/// mensajes y sus operaciones, que es justamente lo que un molde único borra.
///
/// ==========================================================================
/// LO QUE ESTE ARCHIVO NO SABE, Y NO LE HACE FALTA
/// ==========================================================================
///
/// No sabe que la API está en C#. Da la casualidad de que sí lo está —el front
/// también— pero en ninguna línea se aprovecha: todo viaja como JSON por HTTP,
/// igual que si la API estuviera en Python.
///
/// Y no sabe que detrás hay SQL Server. Eso es asunto de la API.
/// </summary>
public class ServicioAliado
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _opciones = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly List<string> NoDisponible = new()
    {
        "El servicio no está disponible. ¿Está arriba la API?"
    };

    public ServicioAliado(HttpClient http)
    {
        _http = http;
    }

    // ------------------------------------------------------------------
    // RF1 — Listar
    // ------------------------------------------------------------------
    public async Task<Resultado<List<Aliado>>> Listar(int limite = 1000)
    {
        try
        {
            var r = await _http.GetAsync($"/api/aliado?limite={limite}");

            // 204 es «no hay ninguno», y NO es un error: la pantalla muestra
            // un recuadro que lo dice, no un aviso rojo.
            if (r.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return Resultado<List<Aliado>>.Bien(new());
            }

            if (!r.IsSuccessStatusCode)
            {
                return Resultado<List<Aliado>>.Mal(await Mensajes(r));
            }

            // El sobre del contrato: { tabla, limite, total, datos[] }
            var sobre = await r.Content.ReadFromJsonAsync<JsonElement>();
            var datos = sobre.GetProperty("datos")
                .Deserialize<List<Aliado>>(_opciones) ?? new();

            return Resultado<List<Aliado>>.Bien(datos);
        }
        catch (HttpRequestException)
        {
            return Resultado<List<Aliado>>.Mal(NoDisponible);
        }
        catch (TaskCanceledException)
        {
            return Resultado<List<Aliado>>.Mal(NoDisponible);
        }
    }

    // ------------------------------------------------------------------
    // RF2 — Obtener uno
    // ------------------------------------------------------------------
    public async Task<Resultado<Aliado>> Obtener(int nit)
    {
        try
        {
            var r = await _http.GetAsync($"/api/aliado/{nit}");
            if (!r.IsSuccessStatusCode)
            {
                return Resultado<Aliado>.Mal(await Mensajes(r));
            }

            var ficha = await r.Content.ReadFromJsonAsync<Aliado>(_opciones);
            return Resultado<Aliado>.Bien(ficha!);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return Resultado<Aliado>.Mal(NoDisponible);
        }
    }

    // ------------------------------------------------------------------
    // RF3 — Crear
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Crear(Aliado entidad)
    {
        return await Enviar(HttpMethod.Post, "/api/aliado", entidad);
    }

    // ------------------------------------------------------------------
    // RF4 — Reemplazar: «guardar la ficha completa»
    //
    // La llave NO va en el cuerpo: identifica la fila y viaja en la ruta.
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Reemplazar(int nit, Aliado entidad)
    {
        var cuerpo = new
        {
            razonSocial = entidad.RazonSocial,
            nombreContacto = entidad.NombreContacto,
            correo = entidad.Correo,
            telefono = entidad.Telefono,
            ciudad = entidad.Ciudad
        };
        return await Enviar(HttpMethod.Put, $"/api/aliado/{nit}", cuerpo);
    }

    // ------------------------------------------------------------------
    // RF5 — Actualizar: «guardar solo lo que cambié»
    //
    // Solo viaja lo diligenciado. Un campo en blanco NO se envía — no es que
    // se envíe vacío: sencillamente no va, y la API deja ese campo como estaba.
    //
    // El diccionario es de `object?` y no de `string`: hay campos que el
    // contrato pide como NÚMERO, y un número entre comillas la API lo
    // rechazaría aunque el valor fuera correcto.
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Actualizar(
        int nit,
        string? razonSocial,
        string? nombreContacto,
        string? correo,
        string? telefono,
        string? ciudad)
    {
        var cuerpo = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(razonSocial)) cuerpo["razonSocial"] = razonSocial;
        if (!string.IsNullOrWhiteSpace(nombreContacto)) cuerpo["nombreContacto"] = nombreContacto;
        if (!string.IsNullOrWhiteSpace(correo)) cuerpo["correo"] = correo;
        if (!string.IsNullOrWhiteSpace(telefono)) cuerpo["telefono"] = telefono;
        if (!string.IsNullOrWhiteSpace(ciudad)) cuerpo["ciudad"] = ciudad;

        return await Enviar(HttpMethod.Patch, $"/api/aliado/{nit}", cuerpo);
    }

    // ------------------------------------------------------------------
    // RF6 — Retirar del uso (la API lo hace lógico: la fila no se borra)
    // ------------------------------------------------------------------
    public async Task<Resultado<bool>> Eliminar(int nit)
    {
        return await Enviar(HttpMethod.Delete, $"/api/aliado/{nit}", null);
    }

    // ------------------------------------------------------------------
    // Lo común a las cuatro operaciones que escriben
    // ------------------------------------------------------------------
    private async Task<Resultado<bool>> Enviar(HttpMethod metodo, string ruta, object? cuerpo)
    {
        try
        {
            var peticion = new HttpRequestMessage(metodo, ruta);
            if (cuerpo != null)
            {
                peticion.Content = JsonContent.Create(cuerpo);
            }

            var r = await _http.SendAsync(peticion);
            return r.IsSuccessStatusCode
                ? Resultado<bool>.Bien(true)
                : Resultado<bool>.Mal(await Mensajes(r));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return Resultado<bool>.Mal(NoDisponible);
        }
    }

    /// <summary>
    /// Traduce a texto los errores que produce ESTA API.
    ///
    /// El sobre es plano y tiene dos formas:
    ///   { estado, mensaje, detalle }   → 400, 404, 500
    ///   { estado, mensaje, errores[] } → 422, cuando el cuerpo no cumple
    ///
    /// **Este método es el único sitio del front que conoce ese formato.** Si
    /// mañana la API cambia el sobre, se cambia aquí y en ninguna página.
    /// </summary>
    private static async Task<List<string>> Mensajes(HttpResponseMessage r)
    {
        try
        {
            var sobre = await r.Content.ReadFromJsonAsync<JsonElement>();

            if (sobre.TryGetProperty("errores", out var errores)
                && errores.ValueKind == JsonValueKind.Array
                && errores.GetArrayLength() > 0)
            {
                return errores.EnumerateArray()
                    .Select(x => x.GetString() ?? "")
                    .Where(x => x.Length > 0)
                    .ToList();
            }

            var partes = new List<string>();
            if (sobre.TryGetProperty("mensaje", out var m)) partes.Add(m.GetString() ?? "");
            if (sobre.TryGetProperty("detalle", out var dt)) partes.Add(dt.GetString() ?? "");
            partes.RemoveAll(string.IsNullOrWhiteSpace);

            return partes.Count > 0
                ? partes
                : new List<string> { "No se pudo completar la operación." };
        }
        catch
        {
            // Un 500 puede devolver HTML en vez de JSON.
            return new List<string> { "No se pudo completar la operación." };
        }
    }
}
