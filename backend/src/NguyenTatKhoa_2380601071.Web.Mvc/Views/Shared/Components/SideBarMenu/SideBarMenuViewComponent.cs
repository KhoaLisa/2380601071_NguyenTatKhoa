using Abp.Application.Navigation;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Web.Views.Shared.Components.SideBarMenu;

public class SideBarMenuViewComponent : NguyenTatKhoa_2380601071ViewComponent
{
    private readonly IUserNavigationManager _userNavigationManager;
    private readonly IAbpSession _abpSession;

    public SideBarMenuViewComponent(
        IUserNavigationManager userNavigationManager,
        IAbpSession abpSession)
    {
        _userNavigationManager = userNavigationManager;
        _abpSession = abpSession;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new SideBarMenuViewModel
        {
            MainMenu = await _userNavigationManager.GetMenuAsync("MainMenu", _abpSession.ToUserIdentifier())
        };

        return View(model);
    }
}
