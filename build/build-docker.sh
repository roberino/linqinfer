dotnet restore "../tests/LinqInfer.Tests/LinqInfer.Tests-dotnetcore.csproj"
dotnet build -f netstandard2.0 "../tests/LinqInfer.Tests/LinqInfer.Tests-dotnetcore.csproj"

# dotnet restore "../src/LinqInfer.AspNetCore/LinqInfer.AspNetCore.csproj"
# dotnet build -f netstandard2.0 "../src/LinqInfer.AspNetCore/LinqInfer.AspNetCore.csproj"

#dotnet restore "../tests/LinqInfer.AspNetCoreTestHarness/LinqInfer.AspNetCoreTestHarness.csproj"
#dotnet build -f netcoreapp2.0.0-preview2 "../tests/LinqInfer.AspNetCoreTestHarness/LinqInfer.AspNetCoreTestHarness.csproj"
