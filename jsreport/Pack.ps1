$version = "2.0.1"

dotnet restore .\jsreport.Client\jsreport.Client.csproj /property:version=$version
dotnet build .\jsreport.Client\jsreport.Client.csproj /property:version=$version
dotnet pack .\jsreport.Client\jsreport.Client.csproj -o ..\output  /property:version=$version 

dotnet restore .\jsreport.Client.HttpHandler\jsreport.Client.HttpHandler.csproj /property:version=$version
dotnet build .\jsreport.Client.HttpHandler\jsreport.Client.HttpHandler.csproj /property:version=$version
dotnet pack .\jsreport.Client.HttpHandler\jsreport.Client.HttpHandler.csproj  -o ..\output  /property:version=$version

dotnet restore .\jsreport.Embedded\jsreport.Embedded.csproj /property:version=$version
dotnet build .\jsreport.Embedded\jsreport.Embedded.csproj /property:version=$version
dotnet pack .\jsreport.Embedded\jsreport.Embedded.csproj  -o ..\output  /property:version=$version

dotnet restore .\jsreport.MVC\jsreport.MVC.csproj /property:version=$version
dotnet build .\jsreport.MVC\jsreport.MVC.csproj /property:version=$version
dotnet pack .\jsreport.MVC\jsreport.MVC.csproj  -o  ..\output  /property:version=$version

dotnet restore .\jsreport.Local\jsreport.Local.csproj /property:version=$version
dotnet build .\jsreport.Local\jsreport.Local.csproj /property:version=$version
dotnet pack .\jsreport.Local\jsreport.Local.csproj  -o ..\output  /property:version=$version