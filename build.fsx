#r "packages/FAKE/tools/FakeLib.dll"
open Fake

RestorePackages()

let buildDir = "./build/"
let testDir  = "./test/"

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

"Clean"
  ==> "BuildApp"
  ==> "BuildTest"
  ==> "Test"

RunTargetOrDefault "Test"