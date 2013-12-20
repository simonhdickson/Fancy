#r "packages/FAKE/tools/FakeLib.dll"
open Fake

RestorePackages()

let buildDir = "./build/"
let testDir  = "./test/"

let project = "Fanciful"
let authors = ["Simon Dickson"]
let summary = "A more strongly typed way to use Nancy"
let description = """
  An F# friendly wrapper around Nancy, alose aiming for compatibile with Nancy"""

let tags = "F# fsharp nancy fancy fanciful"

Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir]
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
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = "0.1"
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        "/FSharp.Formatting.nuspec"
)

"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"

RunTargetOrDefault "Test"