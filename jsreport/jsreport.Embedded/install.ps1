param($installPath, $toolsPath, $package, $project)

$file1 = $project.ProjectItems.Item("jsreport-net-embedded.zip")
 
# set 'Copy To Output Directory' to 'Copy always'
$copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
$copyToOutput1.Value = 2

$file2 = $project.ProjectItems.Item("node.exe")
 
# set 'Copy To Output Directory' to 'Copy always'
$copyToOutput2 = $file2.Properties.Item("CopyToOutputDirectory")
$copyToOutput2.Value = 2