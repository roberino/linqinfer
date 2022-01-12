dotnet restore "../tests/LinqInfer.UnitTests/LinqInfer.UnitTests.csproj"
dotnet restore "../tests/LinqInfer.Benchmarking/LinqInfer.Benchmarking.csproj"

dotnet build -c Release "../tests/LinqInfer.UnitTests/LinqInfer.UnitTests.csproj"
dotnet build -c Release "../tests/LinqInfer.Benchmarking/LinqInfer.Benchmarking.csproj"

dotnet publish -c Release -o "../tests/Benchmarks" "../tests/LinqInfer.Benchmarking/LinqInfer.Benchmarking.csproj"
