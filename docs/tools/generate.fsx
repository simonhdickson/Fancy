#I "../../packages/FSharp.Formatting/lib/net40"
#r "FSharp.CodeFormat.dll"
#r "FSharp.Literate.dll"
open FSharp.Literate
open System.IO

let source = Path.Combine(__SOURCE_DIRECTORY__, "../source")
let template = Path.Combine(__SOURCE_DIRECTORY__, "../templates/") 
let output = Path.Combine(__SOURCE_DIRECTORY__, "../output/")

let projectTemplate = Path.Combine(template, "template.html")
let projectInfo =
  [ "page-description", "Nancy for F#"
    "page-author", "Simon Dickson"
    "github-link", "https://github.com/simonhdickson/Fancy"
    "project-name", "Fancy"
    "root", "content" ]

Directory.Delete(output, true)

if not (Directory.Exists(output)) then
  Directory.CreateDirectory(output) |> ignore
  Directory.CreateDirectory (Path.Combine(output, "content")) |> ignore

Literate.ProcessDirectory
  (source, projectTemplate, output, OutputKind.Html, replacements = projectInfo)

for fileInfo in DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "..\content")).EnumerateFiles() do
  fileInfo.CopyTo(Path.Combine(Path.Combine(output, "content"), fileInfo.Name)) |> ignore
