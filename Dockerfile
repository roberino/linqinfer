# Builds and runs tests

FROM microsoft/dotnet:1.1.2-sdk

# NOTE 2.0.0-preview2-sdk currently causes odd build errors

WORKDIR /app

ADD ./ /app

# EXPOSE 8083

WORKDIR ./build
RUN ./build-docker.sh

WORKDIR ../
RUN dotnet run -f netcoreapp1.1 -p ./tests/LinqInfer.Tests/LinqInfer.Tests-dotnetcore.csproj
# RUN dotnet run -p ./tests/LinqInfer.AspNetCoreTestHarness/LinqInfer.AspNetCoreTestHarness.csproj