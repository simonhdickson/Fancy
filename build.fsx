#r "packages/FAKE/tools/FakeLib.dll"
open System.IO
open Fake            
open Fake.Git
open System

let buildDir = "./bin/"
let testDir  = "./test/"
let nugetDir = "./nuget/"
let fsharpCoreDir = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0"

let project = "Fanciful"
let authors = ["Simon Dickson"; "Remko Boschker"; "Albert-Jan Nijburg"]
let summary = "A more strongly typed way to use Nancy"
let description = """
  An F# friendly wrapper around Nancy, also aiming for compatibility with Nancy"""

let tags = "F# fsharp nancy fanciful"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; nugetDir; "./temp/"]
)

Target "RestorePackages" RestorePackages

Target "Build" (fun _ ->
   !! "src/**/*.fsproj"
     |> MSBuildRelease buildDir "Build"
     |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
   !! "tests/**/*.fsproj"
     |> MSBuildRelease testDir "Build"
     |> Log "AppBuild-Output: "
)

Target "Test" (fun _ ->
    !! (testDir + "/*.Test*.dll") 
    |> xUnit (fun p ->
        { p with
            ToolPath = "./packages/xunit.runners/tools/xunit.console.clr4.exe"
            TimeOut = TimeSpan.FromMinutes 20.
            OutputDir = "./" }) 
)
              
let release = ReleaseNotesHelper.parseReleaseNotes (File.ReadLines "ReleaseNotes.md")

Target "NuGetFancy" (fun _ ->
    let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    let nugetlibDir = nugetDir @@ "Fanciful/lib/net45"
    CopyDir nugetlibDir buildDir (fun file -> file.Contains "Fancy.dll")
    !! "fancy.nuspec" |> Copy (nugetDir @@ "Fanciful") 
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Summary = summary
            Description = description
            Version = release.NugetVersion
            Tags = tags
            OutputPath = nugetDir
            WorkingDir = nugetDir @@ "Fanciful"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = 
                ["Nancy", GetPackageVersion "./packages/" "Nancy"] })
        (nugetDir @@ "Fanciful" @@  "fancy.nuspec")
)

Target "NuGetFancyTesting" (fun _ ->
    trace "test"
    let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    let nugetlibDir = nugetDir @@ "Fanciful.Testing/lib/net45"
    CopyDir nugetlibDir buildDir (fun file -> file.Contains "Fancy.Testing.dll")
    !! "fancy.testing.nuspec" |> Copy (nugetDir @@ "Fanciful.Testing") 
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Summary = summary
            Description = description
            Version = release.NugetVersion
            Tags = tags
            OutputPath = nugetDir
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            WorkingDir = nugetDir @@ "Fanciful.Testing"
            Dependencies = 
                ["Nancy", GetPackageVersion "./packages/" "Nancy"
                 "Nancy.Testing", GetPackageVersion "./packages/" "Nancy.Testing"] })
        (nugetDir @@ "Fanciful.Testing" @@ "fancy.testing.nuspec") 
)

Target "GenerateDocs" (fun _ ->
    executeFSI "docs/tools" "generate.fsx" [] |> ignore
)

let gitHome = "https://github.com/simonhdickson"

Target "ReleaseDocs" (fun _ ->
    Repository.clone "" (gitHome + "/Fancy.git") "temp/gh-pages"
    Branches.checkoutBranch "temp/gh-pages" "gh-pages"
    CopyRecursive "docs/output" "temp/gh-pages" true |> printfn "%A"
    CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
    let cmd = sprintf """commit -a -m "Update generated documentation for version %s""" release.NugetVersion
    CommandHelper.runSimpleGitCommand "temp/gh-pages" cmd |> printfn "%s"
    Branches.push "temp/gh-pages"
)

Target "Nuget" (fun _ -> 
    ()
)

"Clean"
  ==> "RestorePackages"
  ==> "Build"
  ==> "BuildTest"
  ==> "Test"
  ==> "NuGet"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"

"NuGetFancy" 
    ==> "NuGet"

"NuGetFancyTesting"
    ==> "NuGet"

RunTargetOrDefault "GenerateDocs"