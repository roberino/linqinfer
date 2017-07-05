rem build main lib

dotnet restore "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet build "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet pack "src\LinqInfer\LinqInfer-dotnetcore.csproj" --output ..\..\artifacts

rem build aspnet core lib

dotnet restore "src\LinqInfer.AspNetCore\LinqInfer.AspNetCore.csproj"
dotnet build "src\LinqInfer.AspNetCore\LinqInfer.AspNetCore.csproj"
dotnet pack "src\LinqInfer.AspNetCore\LinqInfer.AspNetCore.csproj" --output ..\..\artifacts

PAUSE