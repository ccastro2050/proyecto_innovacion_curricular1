using System.ComponentModel.DataAnnotations;

namespace ApiInnovacion.Peticiones;

/// <summary>
/// El cuerpo del POST. Los SEIS campos son obligatorios: crear un aliado a medias
/// no tiene sentido. Si falta uno, el framework responde 422 antes de que el
/// negocio se entere (3_plan.md §4.1).
/// </summary>
public class AliadoCrear
{
    [Required(ErrorMessage = "El campo nit es obligatorio.")]
    public int? Nit { get; set; }

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
