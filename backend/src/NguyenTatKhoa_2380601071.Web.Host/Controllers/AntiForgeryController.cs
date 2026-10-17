using Abp.Web.Security.AntiForgery;
using NguyenTatKhoa_2380601071.Controllers;
using Microsoft.AspNetCore.Antiforgery;

namespace NguyenTatKhoa_2380601071.Web.Host.Controllers
{
    public class AntiForgeryController : NguyenTatKhoa_2380601071ControllerBase
    {
        private readonly IAntiforgery _antiforgery;
        private readonly IAbpAntiForgeryManager _antiForgeryManager;

        public AntiForgeryController(IAntiforgery antiforgery, IAbpAntiForgeryManager antiForgeryManager)
        {
            _antiforgery = antiforgery;
            _antiForgeryManager = antiForgeryManager;
        }

        public void GetToken()
        {
            _antiforgery.SetCookieTokenAndHeader(HttpContext);
        }

        public void SetCookie()
        {
            _antiForgeryManager.SetCookie(HttpContext);
        }
    }
}
