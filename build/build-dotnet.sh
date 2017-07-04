dotnet restore "../src/LinqInfer/LinqInfer-dotnetcore.csproj"
dotnet build -f netstandard1.6 "../src/src/LinqInfer/LinqInfer-dotnetcore.csproj"
dotnet restore "../src/tests/LinqInfer.Tests/LinqInfer.Tests-dotnetcore.csproj"
dotnet build -f netstandard1.6 "../src/tests/LinqInfer.Tests/LinqInfer.Tests-dotnetcore.csproj"
