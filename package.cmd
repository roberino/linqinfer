rem .nuget\nuget pack "src\LinqInfer\LinqInfer.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive
dotnet build "src\LinqInfer\project.json"
dotnet pack "src\LinqInfer.Storage.SQLite\project.json" --output artifacts
.nuget\nuget pack "src\LinqInfer.NeuralServer\LinqInfer.NeuralServer.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive
.nuget\nuget pack "src\LinqInfer.Owin\LinqInfer.Owin.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive
PAUSE