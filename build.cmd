"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" /p:AllowUnsafeBlocks=true /m:8 /p:Configuration=Release "LinqInfer.sln"
"packages\NUnit.ConsoleRunner.3.5.0\tools\nunit3-console.exe" "tests\LinqInfer.Tests\LinqInfer.Tests.csproj"
"packages\NUnit.ConsoleRunner.3.5.0\tools\nunit3-console.exe" "tests\LinqInfer.Owin.Tests\LinqInfer.Owin.Tests.csproj"
"packages\NUnit.ConsoleRunner.3.5.0\tools\nunit3-console.exe" "tests\LinqInfer.Storage.SQLite.Tests\LinqInfer.Storage.SQLite.Tests.csproj"

PAUSE