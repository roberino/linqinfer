dotnet restore "../src/LinqInfer/LinqInfer.csproj"
dotnet build -f netstandard2.0 "../src/LinqInfer/LinqInfer.csproj"

dotnet restore "../tests/LinqInfer.Tests/LinqInfer.Tests.csproj"
dotnet build -f netstandard1.6 "../tests/LinqInfer.Tests/LinqInfer.Tests.csproj"

dotnet restore "../tests/LinqInfer.AspNetCoreTestHarness/LinqInfer.AspNetCoreTestHarness.csproj"
dotnet build "../tests/LinqInfer.AspNetCoreTestHarness/LinqInfer.AspNetCoreTestHarness.csproj"
