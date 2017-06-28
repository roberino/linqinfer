rem .nuget\nuget pack "src\LinqInfer\LinqInfer.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive
dotnet restore "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet build "src\LinqInfer\LinqInfer-dotnetcore.csproj"
dotnet pack "src\LinqInfer\LinqInfer-dotnetcore.csproj" --output ..\..\artifacts
rem dotnet pack "src\LinqInfer.Data.Orm\project.json" --output artifacts
rem dotnet pack "src\LinqInfer.Storage.SQLite\project.json" --output artifacts
rem .nuget\nuget pack "src\LinqInfer.NeuralServer\LinqInfer.NeuralServer.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive -Prop Configuration=Release
rem .nuget\nuget pack "src\LinqInfer.Owin\LinqInfer.Owin.csproj" -OutputDirectory artifacts -IncludeReferencedProjects -NonInteractive -Prop Configuration=Release
PAUSE