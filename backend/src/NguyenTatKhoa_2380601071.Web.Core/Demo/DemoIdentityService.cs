using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.Runtime.Session;
using NguyenTatKhoa_2380601071.Authorization;
using NguyenTatKhoa_2380601071.Authorization.Users;
using NguyenTatKhoa_2380601071.Toeic;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Demo
{
    public class DemoIdentityService : ITransientDependency
    {
        private readonly UserManager _users;
        private readonly UserClaimsPrincipalFactory _claims;
        private readonly IUnitOfWorkManager _units;
        private readonly IAbpSession _session;
        private readonly IPermissionManager _permissions;
        private readonly IToeicTransactionLock _transactionLock;

        public DemoIdentityService(UserManager users, UserClaimsPrincipalFactory claims, IUnitOfWorkManager units,
            IAbpSession session, IPermissionManager permissions, IToeicTransactionLock transactionLock)
        {
            _users = users;
            _claims = claims;
            _units = units;
            _session = session;
            _permissions = permissions;
            _transactionLock = transactionLock;
        }

        public async Task<ClaimsPrincipal> GetLecturerAsync()
        {
            return await GetPrincipalAsync("toeicflow.demo.lecturer", "lecturer");
        }

        public async Task<ClaimsPrincipal> GetStudentAsync(string installationId)
        {
            byte[] hash;
            using (var sha = SHA256.Create()) hash = sha.ComputeHash(Encoding.UTF8.GetBytes(installationId.ToLowerInvariant()));
            var key = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return await GetPrincipalAsync("toeicflow.student." + key, "student");
        }

        private async Task<ClaimsPrincipal> GetPrincipalAsync(string userName, string kind)
        {
            using (_session.Use(null, null))
            using (var unit = _units.Begin())
            {
                using (_units.Current.SetTenantId(null))
                {
                    await _transactionLock.AcquireAsync("demo-user:" + userName);
                    var user = await _users.FindByNameAsync(userName);
                    if (user == null)
                    {
                        user = new User
                        {
                            TenantId = null,
                            UserName = userName,
                            Name = kind == "lecturer" ? "Giảng viên" : "Sinh viên",
                            Surname = "TOEICFlow",
                            EmailAddress = userName + "@demo.toeicflow.invalid",
                            IsActive = true,
                            IsEmailConfirmed = true
                        };
                        user.SetNormalizedNames();
                        (await _users.CreateAsync(user, "Tf!" + Guid.NewGuid().ToString("N") + "a9")).CheckErrors();
                        await _units.Current.SaveChangesAsync();
                    }

                    // Demo users never inherit Administrator or tenant-management permissions.
                    (await _users.SetRolesAsync(user, Array.Empty<string>())).CheckErrors();
                    var names = kind == "lecturer"
                        ? new[] { PermissionNames.Pages_Exams, PermissionNames.Pages_Exams_Manage, PermissionNames.Pages_Exams_Statistics }
                        : Array.Empty<string>();
                    await _users.SetGrantedPermissionsAsync(user, names.Select(name => _permissions.GetPermission(name)));
                    var principal = await _claims.CreateAsync(user);
                    ((ClaimsIdentity)principal.Identity).AddClaim(new Claim(DemoAccessPolicy.ClaimType, kind));
                    await unit.CompleteAsync();
                    return principal;
                }
            }
        }
    }
}
