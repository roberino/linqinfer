rem .nuget\nuget pack "src\LinqInfer\LinqInfer.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive
dotnet pack "src\LinqInfer\project.json" --output artifacts
PAUSE