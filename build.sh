#!/bin/sh
dotnet restore "LinqInfer.sln"
dotnet build "LinqInfer.sln"
dotnet pack "src/LinqInfer/LinqInfer.csproj" --output ../../artifacts
dotnet pack "src/LinqInfer.Microservices/LinqInfer.Microservices.csproj" --output ../../artifacts