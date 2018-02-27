dotnet restore "../tests/LinqInfer.Tests/LinqInfer.UnitTests.csproj"
dotnet build -c Release -f netcoreapp2.0 "../tests/LinqInfer.UnitTests/LinqInfer.UnitTests.csproj"
dotnet build -c Release -f netcoreapp2.0 "../tests/LinqInfer.Benchmarking/LinqInfer.Benchmarking.csproj"
