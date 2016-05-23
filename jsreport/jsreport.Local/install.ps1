param($installPath, $toolsPath, $package, $project)

$production = $project.ProjectItems.Item("jsreport").ProjectItems.Item("production")

$file1 = $production.ProjectItems.Item("jsreport.zip")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file1 = $production.ProjectItems.Item("server.js")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file1 = $production.ProjectItems.Item("prod.config.json")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2


$reports = $project.ProjectItems.Item("jsreport").ProjectItems.Item("reports")

$data = $reports.ProjectItems.Item("data").ProjectItems.Item("Sample data")

$file1 = $data.ProjectItems.Item("config.json")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file1 = $data.ProjectItems.Item("dataJson.json")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$template = $reports.ProjectItems.Item("templates").ProjectItems.Item("Sample report")

$file1 = $template.ProjectItems.Item("config.json")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file1 = $template.ProjectItems.Item("content.handlebars")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file1 = $template.ProjectItems.Item("footer.handlebars")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file1 = $template.ProjectItems.Item("header.handlebars")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file1 = $template.ProjectItems.Item("helpers.js")
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$fileInfo = new-object -typename System.IO.FileInfo -ArgumentList $project.FullName
$projectDirectory = $fileInfo.DirectoryName
$nodePath = join-path $projectDirectory .bin/node.cmd
$nodePath = $nodePath

$installPath = join-path $projectDirectory jsreport/install.js
$installPath = $installPath
Write-Host "Running " $installPath

& $nodePath $installPath

if ($lastexitcode) {
	throw 'Failed to install jsreport from npm, check install-log.txt for details'
}

$publishPath = join-path $projectDirectory jsreport/pack-production.js
Write-Host "Running " $publishPath
& $nodePath  $publishPath

if ($lastexitcode) {
	throw 'Failed to pack jsreport for publishing, see the package manager output window for details'
}

start "http://jsreport.net/blog/csharp-integration-improvements"

