var target = Argument("target", "Default");
var configuration = HasArgument("Configuration") ? Argument<string>("Configuration") : "Release";
var solutionFile = "LinqInfer.sln";

Task("Build")
    .Does(() =>
    {
        DotNetCoreBuild(
            solutionFile,
            new DotNetCoreBuildSettings()
            {
                Configuration = configuration
            });
        
    });

Task("RunUnitTests")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var projects = GetFiles("./tests/LinqInfer.Tests/*.csproj");
        foreach(var project in projects)
        {
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = true
                });
        }
    });

Task("Default")
    .IsDependentOn("RunUnitTests")
	.Does(() =>
	{
	  Information("Build complete");
	});

RunTarget(target);