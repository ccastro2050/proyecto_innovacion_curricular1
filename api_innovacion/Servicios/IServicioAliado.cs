using ApiInnovacion.Modelos;

namespace ApiInnovacion.Servicios;

/// <summary>
/// El contrato de la capa de negocio. El controlador depende de esta interfaz y
/// no sabe qué hay detrás.
///
/// Solo conoce Modelos/: las clases de Peticiones/ pertenecen a la frontera HTTP
/// y no cruzan a esta capa. El controlador es quien traduce (3_plan.md §4.7).
///
/// Los problemas se comunican con excepciones, que el controlador traduce:
///   ArgumentException      → 400
///   NoEncontradoExcepcion  → 404
/// </summary>
public interface IServicioAliado
{
    /// <summary>Hasta 'limite' aliados activos. ArgumentException si limite &lt;= 0.</summary>
    Task<IEnumerable<Aliado>> ObtenerTodos(int limite);

    /// <summary>El aliado con ese NIT. NoEncontradoExcepcion si no existe o está inactivo.</summary>
    Task<Aliado> ObtenerPorNit(int nit);

    /// <summary>Crea el aliado. El cuerpo ya fue validado por AliadoCrear.</summary>
    Task Crear(Aliado aliado);

    /// <summary>Reemplazo completo. NoEncontradoExcepcion si no existe · devuelve filas afectadas.</summary>
    Task<int> Reemplazar(int nit, Aliado aliado);

    /// <summary>Escribe solo los campos enviados. ArgumentException si no llegó ninguno ·
    /// NoEncontradoExcepcion si no existe · devuelve filas afectadas.</summary>
    Task<int> ActualizarParcial(int nit, string? razonSocial, string? nombreContacto,
                                string? correo, string? telefono, string? ciudad);

    /// <summary>Borrado lógico. NoEncontradoExcepcion si no existe o ya estaba inactivo.</summary>
    Task<int> Eliminar(int nit);
}
