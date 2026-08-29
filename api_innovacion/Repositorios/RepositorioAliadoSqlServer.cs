using System.Data;
using ApiInnovacion.Modelos;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ApiInnovacion.Repositorios;

/// <summary>
/// La capa 3 contra SQL Server, con Dapper (Artículo 2): el SQL se escribe a mano,
/// queda a la vista y SIEMPRE va parametrizado (@parametro) — concatenar un valor
/// sería inyección esperando turno.
///
/// Todas las consultas filtran por activo = 1: el borrado es lógico (Artículo 6).
/// </summary>
public class RepositorioAliadoSqlServer : IRepositorioAliado
{
    private readonly string _cadenaConexion;

    public RepositorioAliadoSqlServer(IConfiguration configuracion)
    {
        _cadenaConexion = configuracion.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'SqlServer'.");
    }

    private IDbConnection CrearConexion() => new SqlConnection(_cadenaConexion);

    // Los alias traducen los nombres de la tabla (snake_case) a los de la entidad
    // (PascalCase): Dapper mapea por nombre.
    private const string COLUMNAS = @"nit AS Nit, razon_social AS RazonSocial,
            nombre_contacto AS NombreContacto, correo AS Correo,
            telefono AS Telefono, ciudad AS Ciudad";

    public async Task<IEnumerable<Aliado>> ObtenerTodos(int limite)
    {
        using var conexion = CrearConexion();
        var sql = $@"
            SELECT TOP (@Limite) {COLUMNAS}
            FROM aliado
            WHERE activo = 1
            ORDER BY nit ASC";

        return await conexion.QueryAsync<Aliado>(sql, new { Limite = limite });
    }

    public async Task<Aliado?> ObtenerPorNit(int nit)
    {
        using var conexion = CrearConexion();
        // Un aliado inactivo responde como inexistente (C8)
        var sql = $@"
            SELECT {COLUMNAS}
            FROM aliado
            WHERE nit = @Nit AND activo = 1";

        return await conexion.QueryFirstOrDefaultAsync<Aliado>(sql, new { Nit = nit });
    }

    public async Task Crear(Aliado aliado)
    {
        using var conexion = CrearConexion();
        // Nace activo. Un nit repetido lo rechaza la llave primaria: 500 (C11).
        const string sql = @"
            INSERT INTO aliado (nit, razon_social, nombre_contacto, correo, telefono, ciudad, activo)
            VALUES (@Nit, @RazonSocial, @NombreContacto, @Correo, @Telefono, @Ciudad, 1)";

        await conexion.ExecuteAsync(sql, aliado);
    }

    public async Task<int> Reemplazar(Aliado aliado)
    {
        using var conexion = CrearConexion();
        const string sql = @"
            UPDATE aliado
            SET razon_social = @RazonSocial, nombre_contacto = @NombreContacto,
                correo = @Correo, telefono = @Telefono, ciudad = @Ciudad
            WHERE nit = @Nit AND activo = 1";

        return await conexion.ExecuteAsync(sql, aliado);
    }

    public async Task<int> ActualizarParcial(int nit, string? razonSocial, string? nombreContacto,
                                             string? correo, string? telefono, string? ciudad)
    {
        using var conexion = CrearConexion();

        // El PATCH escribe solo lo que llegó, así que la consulta se compone.
        // OJO: lo que se compone son NOMBRES DE COLUMNA de una lista cerrada,
        // escrita aquí; los VALORES siempre viajan como @parametro (3_plan.md §4.8).
        var asignaciones = new List<string>();
        var parametros = new DynamicParameters();
        parametros.Add("Nit", nit);

        if (razonSocial != null)
        {
            asignaciones.Add("razon_social = @RazonSocial");
            parametros.Add("RazonSocial", razonSocial);
        }
        if (nombreContacto != null)
        {
            asignaciones.Add("nombre_contacto = @NombreContacto");
            parametros.Add("NombreContacto", nombreContacto);
        }
        if (correo != null)
        {
            asignaciones.Add("correo = @Correo");
            parametros.Add("Correo", correo);
        }
        if (telefono != null)
        {
            asignaciones.Add("telefono = @Telefono");
            parametros.Add("Telefono", telefono);
        }
        if (ciudad != null)
        {
            asignaciones.Add("ciudad = @Ciudad");
            parametros.Add("Ciudad", ciudad);
        }

        if (asignaciones.Count == 0) return 0;

        var sql = $"UPDATE aliado SET {string.Join(", ", asignaciones)} WHERE nit = @Nit AND activo = 1";

        return await conexion.ExecuteAsync(sql, parametros);
    }

    public async Task<int> EliminarLogico(int nit)
    {
        using var conexion = CrearConexion();
        // Borrado LÓGICO en una sola consulta: cero filas afectadas significa
        // "no existe o ya estaba inactivo", que es justo el 404 que pide el
        // contrato — sin necesidad de consultar antes (D-v1-4).
        const string sql = @"
            UPDATE aliado
            SET activo = 0
            WHERE nit = @Nit AND activo = 1";

        return await conexion.ExecuteAsync(sql, new { Nit = nit });
    }
}
