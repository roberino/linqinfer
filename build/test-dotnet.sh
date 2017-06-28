dotnet restore "tests\LinqInfer.Tests\LinqInfer.Tests-dotnetcore.csproj"
dotnet build -f netcoreapp1.1.2 "tests\LinqInfer.Tests\LinqInfer.Tests-dotnetcore.csproj"
dotnet run -f netcoreapp1.1.2 --project "tests\LinqInfer.Tests\LinqInfer.Tests-dotnetcore.csproj"
# dotnet test -v d -f netcoreapp1.1.2 "tests\LinqInfer.Tests\LinqInfer.Tests-dotnetcore.csproj"