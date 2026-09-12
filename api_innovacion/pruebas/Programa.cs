using ApiInnovacion.Excepciones;
using ApiInnovacion.Modelos;
using ApiInnovacion.Repositorios;
using ApiInnovacion.Servicios;

namespace ApiInnovacion.Pruebas;

/// <summary>
/// El repositorio de mentiras: otra implementación de la MISMA interfaz que guarda
/// las filas en una lista en memoria, en vez de hablar con SQL Server.
///
/// Como el servicio solo conoce la interfaz, no se entera de la diferencia. Eso es
/// lo que hace demostrable que las capas están desacopladas (criterio 7).
/// </summary>
public class RepositorioFalso : IRepositorioAliado
{
    private readonly List<Aliado> _filas = new();

    public Task<IEnumerable<Aliado>> ObtenerTodos(int limite) =>
        Task.FromResult(_filas.Take(limite).AsEnumerable());

    public Task<Aliado?> ObtenerPorNit(int nit) =>
        Task.FromResult(_filas.FirstOrDefault(a => a.Nit == nit));

    public Task Crear(Aliado aliado)
    {
        _filas.Add(aliado);
        return Task.CompletedTask;
    }

    public Task<int> Reemplazar(Aliado aliado)
    {
        var i = _filas.FindIndex(a => a.Nit == aliado.Nit);
        if (i == -1) return Task.FromResult(0);
        _filas[i] = aliado;
        return Task.FromResult(1);
    }

    public Task<int> ActualizarParcial(int nit, string? razonSocial, string? nombreContacto,
                                       string? correo, string? telefono, string? ciudad)
    {
        var a = _filas.FirstOrDefault(x => x.Nit == nit);
        if (a == null) return Task.FromResult(0);

        if (razonSocial != null) a.RazonSocial = razonSocial;
        if (nombreContacto != null) a.NombreContacto = nombreContacto;
        if (correo != null) a.Correo = correo;
        if (telefono != null) a.Telefono = telefono;
        if (ciudad != null) a.Ciudad = ciudad;

        return Task.FromResult(1);
    }

    public Task<int> EliminarLogico(int nit)
    {
        // Sacarlo de la lista reproduce el EFECTO OBSERVABLE del borrado lógico:
        // deja de listarse y deja de encontrarse. La entidad no tiene columna
        // Activo —es detalle del motor—, así que aquí no hay nada que marcar.
        var a = _filas.FirstOrDefault(x => x.Nit == nit);
        if (a == null) return Task.FromResult(0);
        _filas.Remove(a);
        return Task.FromResult(1);
    }
}

public class Programa
{
    // Recuerda si alguna comprobación falló. Sin esto la prueba narra el
    // problema pero el proceso termina bien, y nadie que la corra se entera.
    private static bool _bien = true;

    /// <summary>
    /// La línea que PUEDE FALLAR: comprueba la condición, dice qué pasó y deja
    /// constancia si no se cumplió. Es un «assert» escrito a mano: lo mismo que
    /// hace Assert.Equal de xUnit, sin instalar el framework.
    /// </summary>
    private static void Revisar(bool condicion, string ok, string error)
    {
        Console.WriteLine(condicion ? $"[OK] {ok}" : $"[ERROR] {error}");
        if (!condicion) _bien = false;
    }

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Prueba de capas — SIN base de datos ===");

        IRepositorioAliado repoFalso = new RepositorioFalso();
        IServicioAliado servicio = new ServicioAliado(repoFalso);

        var nuevo = new Aliado
        {
            Nit = 900123456,
            RazonSocial = "Fundacion Tecnologica del Norte",
            NombreContacto = "Ana Restrepo",
            Correo = "ana@ftn.edu.co",
            Telefono = "604 555 1234",
            Ciudad = "Medellin"
        };

        // 1. El sistema arranca vacío
        var vacio = await servicio.ObtenerTodos(1000);
        Revisar(!vacio.Any(),
                "El sistema arranca vacío: sin aliados.",
                "Debía arrancar sin aliados.");

        // 2. Crear y listar
        await servicio.Crear(nuevo);
        var lista = (await servicio.ObtenerTodos(1000)).ToList();
        Revisar(lista.Count == 1,
                $"Aliado creado y listado: {lista[0].RazonSocial}",
                "Debía haber exactamente un aliado.");

        // 3. Buscar uno que no existe lanza NoEncontradoExcepcion
        try
        {
            await servicio.ObtenerPorNit(999999999);
            Revisar(false, "", "Debió lanzar NoEncontradoExcepcion.");
        }
        catch (NoEncontradoExcepcion)
        {
            Revisar(true, "Buscar un NIT inexistente lanza NoEncontradoExcepcion.", "");
        }

        // 4. El límite inválido es regla de negocio: ArgumentException (→ 400)
        try
        {
            await servicio.ObtenerTodos(0);
            Revisar(false, "", "Debió lanzar ArgumentException.");
        }
        catch (ArgumentException)
        {
            Revisar(true, "Límite menor o igual a cero rechazado con ArgumentException.", "");
        }

        // 5. PATCH sin campos: 400, no 404. Es la diferencia que más se rompe.
        try
        {
            await servicio.ActualizarParcial(900123456, null, null, null, null, null);
            Revisar(false, "", "Debió lanzar ArgumentException por cuerpo vacío.");
        }
        catch (ArgumentException)
        {
            Revisar(true, "Cuerpo vacío en actualización parcial rechazado con ArgumentException.", "");
        }

        // 6. Eliminar dos veces: la segunda falla como inexistente (C9)
        await servicio.Eliminar(900123456);
        Console.WriteLine("Primera eliminación realizada.");   // narra, no comprueba
        try
        {
            await servicio.Eliminar(900123456);
            Revisar(false, "", "La segunda eliminación debió lanzar NoEncontradoExcepcion.");
        }
        catch (NoEncontradoExcepcion)
        {
            Revisar(true, "Segunda eliminación rechazada: para la API ya no existe.", "");
        }

        // 7. Y el sistema vuelve a estar vacío
        var final = await servicio.ObtenerTodos(1000);
        Revisar(!final.Any(),
                "Tras el borrado, el sistema vuelve a estar vacío.",
                "Debía quedar vacío otra vez.");

        Console.WriteLine(_bien
            ? "=== Prueba de capas completada CON ÉXITO ==="
            : "=== Prueba de capas FALLIDA: mire los [ERROR] de arriba ===");

        // El código de salida es lo que mira quien corre la prueba en
        // automático: 0 = pasó, cualquier otra cosa = falló.
        return _bien ? 0 : 1;
    }
}
