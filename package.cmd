rem build main lib

dotnet restore "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet build "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet pack "src\LinqInfer\LinqInfer-dotnetcore.csproj" --output ..\..\artifacts

rem build aspnet core lib

dotnet restore "src\LinqInfer.Microservices\LinqInfer.Microservices.csproj"
dotnet build "src\LinqInfer.Microservices\LinqInfer.Microservices.csproj"
dotnet pack "src\LinqInfer.Microservices\LinqInfer.Microservices.csproj" --output ..\..\artifacts

PAUSE