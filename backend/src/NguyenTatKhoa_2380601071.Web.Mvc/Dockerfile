FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN ln -s ~/.dotnet/tools/libman /usr/local/bin/libman

WORKDIR /src
COPY ["src/NguyenTatKhoa_2380601071.Web.Mvc/NguyenTatKhoa_2380601071.Web.Mvc.csproj", "src/NguyenTatKhoa_2380601071.Web.Mvc/"]
COPY ["src/NguyenTatKhoa_2380601071.Web.Core/NguyenTatKhoa_2380601071.Web.Core.csproj", "src/NguyenTatKhoa_2380601071.Web.Core/"]
COPY ["src/NguyenTatKhoa_2380601071.Application/NguyenTatKhoa_2380601071.Application.csproj", "src/NguyenTatKhoa_2380601071.Application/"]
COPY ["src/NguyenTatKhoa_2380601071.Core/NguyenTatKhoa_2380601071.Core.csproj", "src/NguyenTatKhoa_2380601071.Core/"]
COPY ["src/NguyenTatKhoa_2380601071.EntityFrameworkCore/NguyenTatKhoa_2380601071.EntityFrameworkCore.csproj", "src/NguyenTatKhoa_2380601071.EntityFrameworkCore/"]
WORKDIR "/src/src/NguyenTatKhoa_2380601071.Web.Mvc"
RUN dotnet restore 

WORKDIR /src
COPY ["src/NguyenTatKhoa_2380601071.Web.Mvc", "src/NguyenTatKhoa_2380601071.Web.Mvc"]
COPY ["src/NguyenTatKhoa_2380601071.Web.Core", "src/NguyenTatKhoa_2380601071.Web.Core"]
COPY ["src/NguyenTatKhoa_2380601071.Application", "src/NguyenTatKhoa_2380601071.Application"]
COPY ["src/NguyenTatKhoa_2380601071.Core", "src/NguyenTatKhoa_2380601071.Core"]
COPY ["src/NguyenTatKhoa_2380601071.EntityFrameworkCore", "src/NguyenTatKhoa_2380601071.EntityFrameworkCore"]
WORKDIR "/src/src/NguyenTatKhoa_2380601071.Web.Mvc"
RUN libman restore
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "NguyenTatKhoa_2380601071.Web.Mvc.dll"]
