using ApiInnovacion.Modelos;

namespace ApiInnovacion.Repositorios;

/// <summary>
/// El contrato de la capa de datos. El servicio conoce ESTA interfaz y nada más:
/// no sabe que detrás hay SQL Server, y por eso se le puede enchufar un
/// repositorio de mentiras para probarlo sin base de datos (Artículo 3).
/// </summary>
public interface IRepositorioAliado
{
    Task<IEnumerable<Aliado>> ObtenerTodos(int limite);
    Task<Aliado?> ObtenerPorNit(int nit);
    Task Crear(Aliado aliado);
    Task<int> Reemplazar(Aliado aliado);
    Task<int> ActualizarParcial(int nit, string? razonSocial, string? nombreContacto,
                                string? correo, string? telefono, string? ciudad);
    Task<int> EliminarLogico(int nit);
}
