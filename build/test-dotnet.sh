dotnet restore "../tests/LinqInfer.Tests/LinqInfer.Tests.csproj"
dotnet build -f netcoreapp1.1.2 "../tests/LinqInfer.Tests/LinqInfer.Tests.csproj"
dotnet test -v d "../tests/LinqInfer.Tests/LinqInfer.Tests.csproj"