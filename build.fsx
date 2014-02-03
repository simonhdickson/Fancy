#r "packages/FAKE/tools/FakeLib.dll"
open System.IO
open Fake            
open Fake.Git

RestorePackages()

let buildDir = "./bin/"
let testDir  = "./test/"
let nugetDir = "./nuget/"
let fsharpCoreDir = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0"

let project = "Fanciful"
let authors = ["Simon Dickson"]
let summary = "A more strongly typed way to use Nancy"
let description = """
  An F# friendly wrapper around Nancy, alose aiming for compatibile with Nancy"""

let tags = "F# fsharp nancy fanciful"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; nugetDir; "./temp/"]
)

Target "Build" (fun _ ->
   !! "src/**/*.fsproj"
     |> MSBuildRelease buildDir "Build"
     |> Log "AppBuild-Output: "
   // TODO: Work out why this is required :s
   CopyDir buildDir fsharpCoreDir (fun file -> file.EndsWith ".optdata" || file.EndsWith ".sigdata")
)

Target "Test" (fun _ ->
    !! (testDir + "/*.Test.*.dll") 
      |> xUnit (fun p ->
          {p with
             ShadowCopy = false;
             OutputDir = testDir})
)
              
let release = ReleaseNotesHelper.parseReleaseNotes (File.ReadLines "ReleaseNotes.md")

Target "NuGet" (fun _ ->
    let description = description.Replace("\r", "").Replace("\n", "").Replace("  ", " ")
    let nugetPath = ".nuget/nuget.exe"
    let nugetlibDir = nugetDir @@ "lib/net45"
    CopyDir nugetlibDir buildDir (fun file -> file.Contains "Fancy.dll")
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
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
  ==> "Build"
  ==> "Test"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"
  ==> "NuGet"

RunTargetOrDefault "GenerateDocs"