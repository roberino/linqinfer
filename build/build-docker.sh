dotnet restore "../tests/LinqInfer.UnitTests/LinqInfer.UnitTests.csproj"
dotnet restore "../tests/LinqInfer.Benchmarking/LinqInfer.Benchmarking.csproj"

dotnet build -c Release -f netcoreapp2.0 "../tests/LinqInfer.UnitTests/LinqInfer.UnitTests.csproj"
dotnet build -c Release -f netcoreapp2.0 "../tests/LinqInfer.Benchmarking/LinqInfer.Benchmarking.csproj"

dotnet publish -c Release -f netcoreapp2.0 -o "../tests/Benchmarks" "../tests/LinqInfer.Benchmarking/LinqInfer.Benchmarking.csproj"
