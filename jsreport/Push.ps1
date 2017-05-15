param([String]$key) 

dotnet nuget push ./output/*.nupkg -k $key
