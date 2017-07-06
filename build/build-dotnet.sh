dotnet restore -f netstandard2.0 "../src/LinqInfer/LinqInfer-dotnetcore.csproj"
dotnet build -f netstandard2.0 "../src/LinqInfer/LinqInfer-dotnetcore.csproj"

dotnet restore -f netstandard1.6 "../tests/LinqInfer.Tests/LinqInfer.Tests-dotnetcore.csproj"
dotnet build -f netstandard1.6 "../tests/LinqInfer.Tests/LinqInfer.Tests-dotnetcore.csproj"

dotnet restore "../tests/LinqInfer.AspNetCoreTestHarness/LinqInfer.AspNetCoreTestHarness.csproj"
dotnet build "../tests/LinqInfer.AspNetCoreTestHarness/LinqInfer.AspNetCoreTestHarness.csproj"
