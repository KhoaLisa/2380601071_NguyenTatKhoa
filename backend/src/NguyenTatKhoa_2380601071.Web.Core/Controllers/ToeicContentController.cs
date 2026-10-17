using Abp.AspNetCore.Mvc.Authorization;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Authorization;
using NguyenTatKhoa_2380601071.Toeic;
using System;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Controllers
{
    [Route("api/toeic/content")]
    [AbpMvcAuthorize]
    public class ToeicContentController : NguyenTatKhoa_2380601071ControllerBase
    {
        private readonly IRepository<ContentBlock, Guid> _contentBlockRepository;

        public ToeicContentController(IRepository<ContentBlock, Guid> contentBlockRepository)
        {
            _contentBlockRepository = contentBlockRepository;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            if (AbpSession.TenantId.HasValue) return StatusCode(403);
            var block = await _contentBlockRepository.GetAll()
                .Include(x => x.PassageGroup).ThenInclude(x => x.Exam)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (block == null || block.Type != ContentBlockType.Image || block.BinaryData == null)
            {
                return NotFound();
            }

            if (block.PassageGroup.Exam.Status != ExamStatus.Published &&
                !PermissionChecker.IsGranted(PermissionNames.Pages_Exams_Manage))
            {
                return StatusCode(403);
            }
            // Defense in depth for rows imported before the safe-raster policy.
            // Never reflect a stored/caller-controlled active MIME type inline.
            string mediaType;
            if (string.IsNullOrWhiteSpace(block.MediaType) ||
                !ExamImageValidator.TryGetMediaType(block.BinaryData, block.FileName, block.MediaType, out mediaType))
            {
                return NotFound();
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
            Response.Headers.CacheControl = "private, no-store";
            return File(block.BinaryData, mediaType);
        }
    }
}
