cd $PSScriptRoot
[System.IO.Directory]::SetCurrentDirectory($PSScriptRoot)

cls

$currentDirectory = [System.IO.DirectoryInfo]::new([System.IO.Directory]::GetCurrentDirectory())

# clean all previously generated packages
$directories = @($currentDirectory) + $currentDirectory.GetDirectories("Debug", [System.IO.SearchOption]::AllDirectories)

foreach ($directory in $directories) {
    $packageFiles = $directory.GetFiles("*.nupkg")
    
    foreach ($file in $packageFiles) {
        $file.Delete()
    }
}

# run gitversion, get the string output
$str = GitVersion.exe | out-string
# convert the result to a JSON object
$json = ConvertFrom-Json $str
# get the appropriate version string (we need the nuget one)
$version = $json.NuGetVersionV2
echo Version=$version

$currentDirectory = [System.IO.DirectoryInfo]::new([System.IO.Directory]::GetCurrentDirectory())

$slnFiles = $currentDirectory.GetFiles("*.sln")

# build the solutions
foreach ($slnFile in $slnFiles) {
    [System.Console]::WriteLine("Building " + $slnFile.FullName)
    cmd.exe /c echo "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\msbuild.exe" $slnFile.FullName /p:Version=$version -target:Rebuild -v:quiet
    & "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\msbuild.exe" $slnFile.FullName /p:Version=$version -target:Rebuild -v:quiet
}

$directories = @($currentDirectory) + $currentDirectory.GetDirectories("Debug", [System.IO.SearchOption]::AllDirectories)

foreach ($directory in $directories) {
    $packageFiles = $directory.GetFiles("*.nupkg")
    
    foreach ($file in $packageFiles) {
        nuget.exe add $file.FullName -source C:\FileNugetPackageSource
    }
}