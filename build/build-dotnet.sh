dotnet restore "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet build -f netstandard1.6 "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet restore "tests\LinqInfer.Tests\LinqInfer.Tests-dotnetcore.csproj"
dotnet build -f netstandard1.6 "tests\LinqInfer.Tests\LinqInfer.Tests-dotnetcore.csproj"