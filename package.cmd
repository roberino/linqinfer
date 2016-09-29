rem .nuget\nuget pack "src\LinqInfer\LinqInfer.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive
dotnet pack "src\LinqInfer\project.json" --output artifacts
.nuget\nuget pack "src\LinqInfer.NeuralServer\LinqInfer.NeuralServer.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive
PAUSE