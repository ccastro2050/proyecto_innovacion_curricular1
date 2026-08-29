using System.ComponentModel.DataAnnotations;

namespace ApiInnovacion.Peticiones;

/// <summary>
/// El cuerpo del PUT. Reemplazar es poner TODO de nuevo, así que los cinco campos
/// son obligatorios. El nit no va aquí: identifica la fila y viaja en la ruta
/// (D-v1-5 del modelo de datos).
/// </summary>
public class AliadoReemplazo
{
    [Required(ErrorMessage = "El campo razonSocial es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El campo razonSocial no puede exceder los 60 caracteres.")]
    public string RazonSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo nombreContacto es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El campo nombreContacto no puede exceder los 60 caracteres.")]
    public string NombreContacto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo correo es obligatorio.")]
    [MaxLength(70, ErrorMessage = "El campo correo no puede exceder los 70 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo telefono es obligatorio.")]
    [MaxLength(45, ErrorMessage = "El campo telefono no puede exceder los 45 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo ciudad es obligatorio.")]
    [MaxLength(45, ErrorMessage = "El campo ciudad no puede exceder los 45 caracteres.")]
    public string Ciudad { get; set; } = string.Empty;
}
