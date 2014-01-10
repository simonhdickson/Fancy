#r "packages/FAKE/tools/FakeLib.dll"
open System.IO
open Fake            
open Fake.Git

RestorePackages()

let buildDir = "./build/"
let testDir  = "./test/"
let nugetDir = "./nuget/"

let project = "Fanciful"
let authors = ["Simon Dickson"]
let summary = "A more strongly typed way to use Nancy"
let description = """
  An F# friendly wrapper around Nancy, alose aiming for compatibile with Nancy"""

let tags = "F# fsharp nancy fancy fanciful"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; nugetDir]
)

Target "BuildApp" (fun _ ->
   !! "src/**/*.fsproj"
     |> MSBuildRelease buildDir "Build"
     |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ ->
    !! "src/**/*.fsproj"
      |> MSBuildDebug testDir "Build"
      |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->
    !! (testDir + "/*.Test.*.dll") 
      |> xUnit (fun p ->
          {p with
             ShadowCopy = false;
             OutputDir = testDir})
)

Target "NuGet" (fun _ ->
    let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    let nugetlibDir = nugetDir @@ "lib/net45"
    CopyDir nugetlibDir "build" (fun file -> file.Contains "Fancy.dll")
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = "0.1.0-beta"
            Tags = tags
            OutputPath = nugetDir
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = 
                ["Nancy", GetPackageVersion "./packages/" "Nancy"] })
        "fancy.nuspec"
)

Target "GenerateDocs" (fun _ ->
    executeFSI "docs/tools" "generate.fsx" [] |> ignore
)

let release = ReleaseNotesHelper.parseReleaseNotes (File.ReadLines "RELEASE_NOTES.md")
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

"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"
  ==> "NuGet"

RunTargetOrDefault "GenerateDocs"