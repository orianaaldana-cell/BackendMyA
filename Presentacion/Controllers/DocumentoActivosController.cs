using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAMIS.Core.DTO;
using MyAMIS.Core.Entidades;

namespace MyAMIS.Presentacion.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentoActivosController : ControllerBase
    {
        private readonly MyAMISContext _context;

        public DocumentoActivosController(MyAMISContext context)
        {
            _context = context;
        }

        // GET: api/DocumentoActivos/lista
        [HttpGet("lista")]
        public async Task<IActionResult> Lista()
        {
            var query = await (from d in _context.DocumentoActivo
                               join a in _context.Activo on d.activoId equals a.idActivo
                               join t in _context.TipoDocumento on d.tipoDocumentoId equals t.idTipoDocumento
                               where d.estado == "Activo"
                               select new
                               {
                                   d.codigo,
                                   Activo = a.codigo,
                                   TipoDocumento = t.nombre,
                                   d.referenciaDocumento,
                                   d.fechaRegistro
                               }).ToListAsync();

            return Ok(query);
        }

        // GET: api/DocumentoActivos/porActivo/ACT-001
        [HttpGet("porActivo/{codigoActivo}")]
        public async Task<IActionResult> GetByActivo(string codigoActivo)
        {
            var query = await (from d in _context.DocumentoActivo
                               join a in _context.Activo on d.activoId equals a.idActivo
                               join t in _context.TipoDocumento on d.tipoDocumentoId equals t.idTipoDocumento
                               where a.codigo == codigoActivo
                                     && d.estado == "Activo"
                                     && a.estado == "Activo"
                               select new
                               {
                                   d.codigo,
                                   TipoDocumento = t.nombre,
                                   d.referenciaDocumento,
                                   d.fechaRegistro
                               }).ToListAsync();

            return Ok(query);
        }

        // POST: api/DocumentoActivos/crear
        [HttpPost("crear")]
        public async Task<IActionResult> Crear(DocumentoActivoCreateDTO dto)
        {
            var activo = await (from a in _context.Activo
                                where a.codigo == dto.codigoActivo && a.estado == "Activo"
                                select a).FirstOrDefaultAsync();

            var tipo = await (from t in _context.TipoDocumento
                              where t.codigo == dto.codigoTipoDocumento && t.estado == "Activo"
                              select t).FirstOrDefaultAsync();

            if (activo == null || tipo == null)
                return BadRequest("Activo o TipoDocumento inválido");

            int? mantenimientoId = null;

            if (!string.IsNullOrWhiteSpace(dto.codigoMantenimiento))
            {
                var mantenimiento = await (from m in _context.Mantenimiento
                                           where m.codigo == dto.codigoMantenimiento && m.estado == "Activo"
                                           select m).FirstOrDefaultAsync();

                if (mantenimiento != null)
                    mantenimientoId = mantenimiento.idMantenimiento;
            }

            int ultimoId = await (from d in _context.DocumentoActivo
                                  orderby d.idDocumentoActivo descending
                                  select d.idDocumentoActivo).FirstOrDefaultAsync();

            var doc = new DocumentoActivo()
            {
                codigo = $"DOC-{(ultimoId + 1):D3}",
                estado = "Activo",
                activoId = activo.idActivo,
                tipoDocumentoId = tipo.idTipoDocumento,
                referenciaDocumento = dto.referenciaDocumento,
                mantenimientoId = mantenimientoId,
                fechaRegistro = DateTime.Now
            };

            _context.DocumentoActivo.Add(doc);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Documento registrado", codigoGenerado = doc.codigo });
        }

        // DELETE: api/DocumentoActivos/borrar/DOC-001
        [HttpDelete("borrar/{codigo}")]
        public async Task<IActionResult> SoftDelete(string codigo)
        {
            var doc = await (from d in _context.DocumentoActivo
                             where d.codigo == codigo
                             select d).FirstOrDefaultAsync();

            if (doc == null)
                return NotFound("Documento no encontrado");

            doc.estado = "Inactivo";
            await _context.SaveChangesAsync();

            return Ok("Documento desactivado");
        }
    }
}