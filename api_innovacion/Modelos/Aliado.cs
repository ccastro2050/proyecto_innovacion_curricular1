namespace ApiInnovacion.Modelos;

/// <summary>
/// Entidad de dominio que representa la tabla aliado: una entidad externa con la
/// que la universidad establece alianzas.
/// Es lo que viaja entre las capas de repositorio, servicio y controlador.
///
/// No incluye la propiedad Activo: el borrado lógico es un detalle interno del
/// motor y no forma parte de lo que la API expone (5_data_model.md §4).
/// </summary>
public class Aliado
{
    /// <summary>Número de identificación tributaria. Es la llave primaria.</summary>
    public int Nit { get; set; }

    /// <summary>El nombre legal de la entidad.</summary>
    public string RazonSocial { get; set; } = string.Empty;

    /// <summary>La persona con quien se habla en esa entidad.</summary>
    public string NombreContacto { get; set; } = string.Empty;

    /// <summary>Correo de contacto. La v1 no valida su formato (D-v1-5).</summary>
    public string Correo { get; set; } = string.Empty;

    /// <summary>Teléfono. Es TEXTO: lleva prefijos, espacios y extensiones,
    /// y nunca se suma ni se ordena aritméticamente (D-v1-5).</summary>
    public string Telefono { get; set; } = string.Empty;

    public string Ciudad { get; set; } = string.Empty;
}
