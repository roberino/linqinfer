dotnet restore "../tests/LinqInfer.Tests/LinqInfer.UnitTests.csproj"
dotnet build -f netcoreapp1.1.2 "../tests/LinqInfer.Tests/LinqInfer.UnitTests.csproj"
dotnet test -v d "../tests/LinqInfer.Tests/LinqInfer.UnitTests.csproj"