using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Web.Resources;

public interface IWebResourceManager
{
    void AddScript(string url, bool addMinifiedOnProd = true);

    IReadOnlyList<string> GetScripts();

    HelperResult RenderScripts();
}
