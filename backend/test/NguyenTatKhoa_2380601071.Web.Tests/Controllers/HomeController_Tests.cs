using NguyenTatKhoa_2380601071.Models.TokenAuth;
using NguyenTatKhoa_2380601071.Web.Controllers;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace NguyenTatKhoa_2380601071.Web.Tests.Controllers;

public class HomeController_Tests : NguyenTatKhoa_2380601071WebTestBase
{
    [Fact]
    public async Task Index_Test()
    {
        await AuthenticateAsync(null, new AuthenticateModel
        {
            UserNameOrEmailAddress = "admin",
            Password = "123qwe"
        });

        //Act
        var response = await GetResponseAsStringAsync(
            GetUrl<HomeController>(nameof(HomeController.Index))
        );

        //Assert
        response.ShouldNotBeNullOrEmpty();
    }
}