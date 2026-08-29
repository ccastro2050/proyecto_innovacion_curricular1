using ApiInnovacion.Excepciones;
using ApiInnovacion.Modelos;
using ApiInnovacion.Peticiones;
using ApiInnovacion.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiInnovacion.Controllers;

/// <summary>
/// La capa 1: HTTP. No contiene lógica de negocio ni SQL (Artículo 3). Traduce las
/// excepciones del negocio a códigos de estado, y las peticiones a lo que la capa 2
/// entiende (3_plan.md §4.7).
///
/// La ruta se escribe COMPLETA y no con [controller]: ese token generaría "Aliado"
/// con mayúscula y el contrato pide /api/aliado (Artículo 10).
/// </summary>
[ApiController]
[Route("api/aliado")]
public class AliadoController : ControllerBase
{
    private readonly IServicioAliado _servicio;

    public AliadoController(IServicioAliado servicio)
    {
        _servicio = servicio;
    }

    /// <summary>RF1 — Listar aliados activos.</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] int limite = 1000)
    {
        try
        {
            var datos = await _servicio.ObtenerTodos(limite);
            var lista = datos.ToList();

            // Vacío NO es error: 204 sin cuerpo. Es además el estado inicial del
            // sistema, porque la tabla arranca sin datos (C7).
            if (lista.Count == 0)
            {
                return NoContent();
            }

            return Ok(new
            {
                tabla = "aliado",
                limite = limite,
                total = lista.Count,
                datos = lista
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { estado = 400, mensaje = "Parámetros inválidos.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF2 — Obtener un aliado por su NIT.</summary>
    [HttpGet("{nit:int}")]
    public async Task<IActionResult> ObtenerPorNit(int nit)
    {
        try
        {
            var aliado = await _servicio.ObtenerPorNit(nit);
            return Ok(aliado);
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Aliado no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF3 — Crear un aliado.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] AliadoCrear peticion)
    {
        try
        {
            // El controlador traduce: la capa 2 recibe la entidad, no el cuerpo HTTP
            var aliado = new Aliado
            {
                Nit = peticion.Nit!.Value,
                RazonSocial = peticion.RazonSocial,
                NombreContacto = peticion.NombreContacto,
                Correo = peticion.Correo,
                Telefono = peticion.Telefono,
                Ciudad = peticion.Ciudad
            };

            await _servicio.Crear(aliado);
            return Ok(new { estado = 200, mensaje = "Aliado creado exitosamente." });
        }
        catch (SqlException ex)
        {
            // NIT duplicado: la llave la defiende la base, no la API (C11)
            return StatusCode(500, new { estado = 500, mensaje = "Error al insertar en la base de datos.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF4 — Reemplazar completamente un aliado.</summary>
    [HttpPut("{nit:int}")]
    public async Task<IActionResult> Reemplazar(int nit, [FromBody] AliadoReemplazo peticion)
    {
        try
        {
            var aliado = new Aliado
            {
                Nit = nit,
                RazonSocial = peticion.RazonSocial,
                NombreContacto = peticion.NombreContacto,
                Correo = peticion.Correo,
                Telefono = peticion.Telefono,
                Ciudad = peticion.Ciudad
            };

            var filas = await _servicio.Reemplazar(nit, aliado);
            return Ok(new { estado = 200, mensaje = "Aliado reemplazado.", filasAfectadas = filas });
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Aliado no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF5 — Actualizar parcialmente un aliado.</summary>
    [HttpPatch("{nit:int}")]
    public async Task<IActionResult> ActualizarParcial(int nit, [FromBody] AliadoActualizar peticion)
    {
        try
        {
            var filas = await _servicio.ActualizarParcial(nit, peticion.RazonSocial,
                peticion.NombreContacto, peticion.Correo, peticion.Telefono, peticion.Ciudad);

            return Ok(new { estado = 200, mensaje = "Aliado actualizado.", filasAfectadas = filas });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { estado = 400, mensaje = "Parámetros inválidos.", detalle = ex.Message });
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Aliado no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }

    /// <summary>RF6 — Borrado lógico.</summary>
    [HttpDelete("{nit:int}")]
    public async Task<IActionResult> Eliminar(int nit)
    {
        try
        {
            var filas = await _servicio.Eliminar(nit);
            return Ok(new { estado = 200, mensaje = "Aliado eliminado.", filasAfectadas = filas });
        }
        catch (NoEncontradoExcepcion ex)
        {
            return NotFound(new { estado = 404, mensaje = "Aliado no encontrado.", detalle = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { estado = 500, mensaje = "Error interno del servidor.", detalle = ex.Message });
        }
    }
}
