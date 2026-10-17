using Abp.Auditing;
using Abp.Authorization;
using Abp.Runtime.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NguyenTatKhoa_2380601071.Authentication.JwtBearer;
using NguyenTatKhoa_2380601071.Demo;
using NguyenTatKhoa_2380601071.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Controllers
{
    [Route("api/demo")]
    [AllowAnonymous]
    [AbpAllowAnonymous]
    [DisableAuditing]
    public class DemoController : NguyenTatKhoa_2380601071ControllerBase
    {
        private readonly DemoIdentityService _identities;
        private readonly TokenAuthConfiguration _tokens;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public DemoController(DemoIdentityService identities, TokenAuthConfiguration tokens,
            IWebHostEnvironment environment)
        {
            _identities = identities;
            _tokens = tokens;
            _environment = environment;
            _configuration = environment.GetAppConfiguration();
        }

        [HttpPost("student-session")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StudentSession([FromBody] DemoStudentSessionInput input)
        {
            if (!DemoAccessPolicy.IsEnabled(_environment, _configuration)) return NotFound();
            if (!DemoAccessPolicy.IsAllowedRequest(HttpContext, _environment, _configuration))
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden);
            if (input == null || !ModelState.IsValid) return BadRequest(ModelState);
            var principal = await _identities.GetStudentAsync(input.InstallationId);
            var now = DateTime.UtcNow;
            var claims = principal.Claims.ToList();
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));
            var jwt = new JwtSecurityToken(_tokens.Issuer, _tokens.Audience, claims, now, now.AddHours(8), _tokens.SigningCredentials);
            return Ok(new
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                ExpireInSeconds = 8 * 60 * 60,
                UserId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier))
            });
        }
    }

    public class DemoStudentSessionInput
    {
        [Required]
        [RegularExpression("^[a-fA-F0-9]{64}$", ErrorMessage = "installationId must contain 32 cryptographically random bytes encoded as 64 hex characters.")]
        public string InstallationId { get; set; }
    }
}
