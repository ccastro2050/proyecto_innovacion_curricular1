using ApiInnovacion.Excepciones;
using ApiInnovacion.Modelos;
using ApiInnovacion.Repositorios;

namespace ApiInnovacion.Servicios;

/// <summary>
/// La capa 2: las reglas de negocio. Depende solo de la interfaz del repositorio,
/// así que no sabe qué motor hay detrás (Artículo 3) — y por eso se puede probar
/// sin base de datos.
///
/// No conoce las clases de Peticiones/: recibe entidades y campos sueltos.
/// </summary>
public class ServicioAliado : IServicioAliado
{
    private readonly IRepositorioAliado _repositorio;

    public ServicioAliado(IRepositorioAliado repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IEnumerable<Aliado>> ObtenerTodos(int limite)
    {
        // Regla de negocio: el límite debe ser positivo. La FORMA del dato es
        // correcta (sí es un entero), así que esto es 400 y no 422 (C10).
        if (limite <= 0)
        {
            throw new ArgumentException("El parámetro limite debe ser un número mayor a 0.");
        }

        return await _repositorio.ObtenerTodos(limite);
    }

    public async Task<Aliado> ObtenerPorNit(int nit)
    {
        var aliado = await _repositorio.ObtenerPorNit(nit);
        if (aliado == null)
        {
            throw new NoEncontradoExcepcion($"No existe un aliado con el NIT {nit}.");
        }

        return aliado;
    }

    public async Task Crear(Aliado aliado)
    {
        await _repositorio.Crear(aliado);
    }

    public async Task<int> Reemplazar(int nit, Aliado aliado)
    {
        // El NIT identifica la fila y viene de la ruta, no del cuerpo
        aliado.Nit = nit;

        var filasAfectadas = await _repositorio.Reemplazar(aliado);
        if (filasAfectadas == 0)
        {
            throw new NoEncontradoExcepcion($"No existe un aliado con el NIT {nit}.");
        }

        return filasAfectadas;
    }

    public async Task<int> ActualizarParcial(int nit, string? razonSocial, string? nombreContacto,
                                             string? correo, string? telefono, string? ciudad)
    {
        // Sin esta comprobación el repositorio devolvería 0 filas, que en toda la
        // demás lógica significa "no existe" — y responderíamos 404 en vez del 400
        // que exige el contrato para un cuerpo vacío.
        if (razonSocial == null && nombreContacto == null && correo == null
            && telefono == null && ciudad == null)
        {
            throw new ArgumentException("No se envió ningún campo para actualizar.");
        }

        var filasAfectadas = await _repositorio.ActualizarParcial(
            nit, razonSocial, nombreContacto, correo, telefono, ciudad);

        if (filasAfectadas == 0)
        {
            throw new NoEncontradoExcepcion($"No existe un aliado con el NIT {nit}.");
        }

        return filasAfectadas;
    }

    public async Task<int> Eliminar(int nit)
    {
        var filasAfectadas = await _repositorio.EliminarLogico(nit);
        if (filasAfectadas == 0)
        {
            throw new NoEncontradoExcepcion($"No existe un aliado con el NIT {nit}.");
        }

        return filasAfectadas;
    }
}
