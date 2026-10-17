param(
    [string]$Server = '.\SQLEXPRESS',
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,100}$')]
    [string]$DatabaseName = 'ToeicReadingDb',
    [switch]$RunChecks
)

$ErrorActionPreference = 'Stop'
$taskWorkspace = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (@('master','model','msdb','tempdb') -contains $DatabaseName.ToLowerInvariant()) { throw 'System databases are not allowed.' }
$taskMutexName = 'Local\ToeicSetup_' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("$Server/$DatabaseName")).Replace('/', '_').Replace('=', '')
$taskMutex = [Threading.Mutex]::new($false, $taskMutexName)
if (-not $taskMutex.WaitOne(0)) { $taskMutex.Dispose(); throw 'Another setup process is already running for this target.' }
$previousConnection = $env:ConnectionStrings__Default
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousPath = $env:PATH
try {
    Push-Location -LiteralPath $taskWorkspace
    $dotnetPath = Join-Path $taskWorkspace '.dotnet\dotnet.exe'
    if (-not (Test-Path -LiteralPath $dotnetPath)) { $dotnetPath = (Get-Command dotnet -ErrorAction Stop).Source }
    $env:PATH = (Split-Path -Parent $dotnetPath) + ';' + $env:PATH
    $env:DOTNET_ROOT = Split-Path -Parent $dotnetPath
    $env:DOTNET_HOST_PATH = $dotnetPath
    $env:DOTNET_CLI_HOME = Join-Path $taskWorkspace '.dotnet_home'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__Default = "Server=$Server;Database=$DatabaseName;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
    $sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
    & $sqlcmd -S $Server -E -Nm -C -d master -l 10 -b -W -Q 'SELECT CAST(@@SERVERNAME AS nvarchar(128)) AS ServerName, CAST(ORIGINAL_LOGIN() AS nvarchar(128)) AS WindowsLogin, HAS_PERMS_BY_NAME(NULL,NULL,''CREATE ANY DATABASE'') AS CanCreateDatabase;'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot connect to SQL Server using the current Windows identity.' }

    # The database name is validated above before SQL identifier interpolation.
    $createSql = @"
SET NOCOUNT ON;
IF DB_ID(N'$DatabaseName') IS NULL
BEGIN
    IF HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE') <> 1
        THROW 51010,'Current account cannot create the requested database.',1;
    EXEC(N'CREATE DATABASE [$DatabaseName]');
    EXEC(N'USE [$DatabaseName]; EXEC sys.sp_addextendedproperty @name=N''TOEIC.Project'',@value=N''NguyenTatKhoa_2380601071'';');
END
ELSE
    EXEC(N'IF NOT EXISTS(SELECT 1 FROM [$DatabaseName].sys.extended_properties WHERE class=0 AND name=N''TOEIC.Project'' AND CONVERT(nvarchar(128),value)=N''NguyenTatKhoa_2380601071'') THROW 51011,''Existing database has no matching project ownership marker. Choose a new DatabaseName.'',1;');
"@
    & $sqlcmd -S $Server -E -Nm -C -d master -l 10 -b -Q $createSql
    if ($LASTEXITCODE -ne 0) { throw 'Target ownership check or database creation failed.' }

    & $dotnetPath tool restore --tool-manifest (Join-Path $taskWorkspace '.config\dotnet-tools.json')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot restore the pinned dotnet-ef 9.0.5 tool.' }
    $efProject = Join-Path $taskWorkspace 'backend\src\NguyenTatKhoa_2380601071.EntityFrameworkCore'
    $startupProject = Join-Path $taskWorkspace 'backend\src\NguyenTatKhoa_2380601071.Web.Mvc'
    & $dotnetPath build $startupProject --nologo --verbosity quiet -p:WarningLevel=0
    if ($LASTEXITCODE -ne 0) { throw 'Backend build failed.' }
    & $dotnetPath tool run dotnet-ef database update --project $efProject --startup-project $startupProject --context NguyenTatKhoa_2380601071DbContext --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed. No reset or database deletion will be attempted.' }

    foreach ($project in @('NguyenTatKhoa_2380601071.Web.Mvc','NguyenTatKhoa_2380601071.Web.Host','NguyenTatKhoa_2380601071.Migrator')) {
        $localConfig = Join-Path $taskWorkspace "backend\src\$project\appsettings.Local.json"
        $settings = if (Test-Path -LiteralPath $localConfig) { Get-Content -Raw -Encoding UTF8 -LiteralPath $localConfig | ConvertFrom-Json } else { [PSCustomObject]@{} }
        if (-not $settings.PSObject.Properties['ConnectionStrings']) { $settings | Add-Member NoteProperty ConnectionStrings ([PSCustomObject]@{}) }
        if (-not $settings.ConnectionStrings.PSObject.Properties['Default']) { $settings.ConnectionStrings | Add-Member NoteProperty Default $env:ConnectionStrings__Default } else { $settings.ConnectionStrings.Default = $env:ConnectionStrings__Default }
        [IO.File]::WriteAllText($localConfig, ($settings | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
    }
    $checkProject = Join-Path $taskWorkspace 'backend\tools\Toeic.DatabaseCheck'
    & $dotnetPath build $checkProject --nologo --verbosity quiet -p:WarningLevel=0
    if ($LASTEXITCODE -ne 0) { throw 'Database verification utility build failed.' }
    $checkDll = Join-Path $checkProject 'bin\Debug\net9.0\Toeic.DatabaseCheck.dll'
    $checkArguments = @($checkDll, '--seed')
    if ($RunChecks) { $checkArguments += '--test' }
    & $dotnetPath @checkArguments
    if ($LASTEXITCODE -ne 0) { throw 'Database verification or rollback checks failed.' }
    & $sqlcmd -S $Server -E -Nm -C -d $DatabaseName -l 10 -b -W -i (Join-Path $taskWorkspace 'docs\database\verify-database.sql')
    if ($LASTEXITCODE -ne 0) { throw 'SQL verification failed.' }
    Write-Output "Setup completed: $Server / $DatabaseName. No Reading document imported."
}
finally {
    $env:ConnectionStrings__Default = $previousConnection
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:PATH = $previousPath
    Pop-Location
    $taskMutex.ReleaseMutex()
    $taskMutex.Dispose()
}
