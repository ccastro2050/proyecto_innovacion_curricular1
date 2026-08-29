namespace ApiInnovacion.Peticiones;

/// <summary>
/// El cuerpo del PATCH: los cinco campos son OPCIONALES, y solo se escriben los
/// que lleguen.
///
/// La diferencia con AliadoReemplazo es toda la lección del contrato: el MISMO
/// cuerpo responde 422 en PUT y 200 en PATCH, y no lo decide un if en el
/// servicio — lo decide el tipo.
/// </summary>
public class AliadoActualizar
{
    public string? RazonSocial { get; set; }
    public string? NombreContacto { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Ciudad { get; set; }
}
