param($installPath, $toolsPath, $package, $project)


$fileInfo = new-object -typename System.IO.FileInfo -ArgumentList $project.FullName
$projectDirectory = $fileInfo.DirectoryName

$jsreport = join-path $projectDirectory jsreport

if (-Not (Test-Path $jsreport)) { 
  $jsreportTools = join-path $toolsPath jsreport  
    
  $jsreport = $project.ProjectItems.AddFolder("jsreport")

  $filePath = join-path $jsreportTools install.cmd
  $jsreport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $jsreportTools jsreport.zip
  $jsreport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $jsreportTools studio.cmd
  $jsreport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $jsreportTools update.cmd
  $jsreport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $jsreportTools install-log.txt
  $jsreport.ProjectItems.AddFromFileCopy($filePath)  

  $app = $jsreport.ProjectItems.AddFolder("app")

  $appPath = join-path $jsreportTools app  

  $filePath = join-path $appPath dev.config.json
  $app.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $appPath prod.config.json
  $app.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $appPath package.json
  $app.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $appPath server.js
  $app.ProjectItems.AddFromFileCopy($filePath) 
  
  $reports = $jsreport.ProjectItems.AddFolder("reports") 
  $data = $reports.ProjectItems.AddFolder("data") 
  $templates = $reports.ProjectItems.AddFolder("templates") 

  $sampleData = $data.ProjectItems.AddFolder("Sample data") 
  $sampleReport = $templates.ProjectItems.AddFolder("Sample report") 

  $reportsPath = join-path $jsreportTools reports
  $dataPath = join-path $reportsPath data
  $sampleDataPath = join-path $dataPath "Sample data"

  $filePath = join-path $sampleDataPath config.json
  $sampleData.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $sampleDataPath dataJson.json
  $sampleData.ProjectItems.AddFromFileCopy($filePath)  

  $templatesPath = join-path $reportsPath templates
  $sampleReportPath = join-path $templatesPath "Sample report"

  $filePath = join-path $sampleReportPath config.json
  $sampleReport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $sampleReportPath content.handlebars
  $sampleReport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $sampleReportPath footer.handlebars
  $sampleReport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $sampleReportPath header.handlebars
  $sampleReport.ProjectItems.AddFromFileCopy($filePath)  

  $filePath = join-path $sampleReportPath helpers.js
  $sampleReport.ProjectItems.AddFromFileCopy($filePath)  

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

start "http://jsreport.net/learn/net-embedded"