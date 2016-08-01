param($installPath, $toolsPath, $package, $project)


$fileInfo = new-object -typename System.IO.FileInfo -ArgumentList $project.FullName
$projectDirectory = $fileInfo.DirectoryName

$jsreport = join-path $projectDirectory jsreport

if (-Not (Test-Path $jsreport)) { 
  $jsreportTools = join-path $toolsPath jsreport
  $project.ProjectItems.AddFromDirectory($jsreportTools)

  $app = $project.ProjectItems.Item("jsreport").ProjectItems.Item("app")
  $jsreport = $project.ProjectItems.Item("jsreport")

  $file1 = $jsreport.ProjectItems.Item("jsreport.zip")
  $copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
  $copyToOutput1.Value = 2
 
  $file1 = $app.ProjectItems.Item("server.js")
  $copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
  $copyToOutput1.Value = 2

  $file1 = $app.ProjectItems.Item("prod.config.json")
  $copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
  $copyToOutput1.Value = 2


  $reports = $jsreport.ProjectItems.Item("reports")
   
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
}

$jsreportPath = join-path $projectDirectory jsreport
$installPath = join-path $projectDirectory jsreport/install.cmd
$installToolsPath = join-path $toolsPath jsreport/install.cmd

if (Test-Path $installPath) {   
  write-Host "Copy " $installToolsPath " to " $jsreportPath
  Copy-Item $installToolsPath -destination $jsreportPath -Force

  Push-Location $jsreportPath
  write-Host "Running " $installPath
  & $installPath

  if ($lastexitcode) {
	throw 'Failed to install jsreport from npm, check install-log.txt for details'
  }
}

Pop-Location

start "http://jsreport.net/blog/csharp-integration-improvements"