#r "packages/FAKE/tools/FakeLib.dll"
open Fake

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

"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"  
  ==> "NuGet"

RunTargetOrDefault "Test"