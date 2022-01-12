rem build main lib

dotnet pack "src\LinqInfer\LinqInfer.csproj" -p:PackageVersion=6.0.1 --output artifacts

PAUSE