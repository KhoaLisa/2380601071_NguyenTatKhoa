namespace NguyenTatKhoa_2380601071.EntityFrameworkCore.Seed.Host;

public class InitialHostDbBuilder
{
    private readonly NguyenTatKhoa_2380601071DbContext _context;

    public InitialHostDbBuilder(NguyenTatKhoa_2380601071DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        new DefaultEditionCreator(_context).Create();
        new DefaultLanguagesCreator(_context).Create();
        new HostRoleAndUserCreator(_context).Create();
        new DefaultSettingsCreator(_context).Create();

        _context.SaveChanges();
    }
}
