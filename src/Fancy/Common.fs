module Common  

open System.Text.RegularExpressions

let applyReplace inputString regex f =
    let index = ref -1
    Regex.Replace(inputString, regex, fun (m:Match) -> index:=!index+1; f (m.Value,!index))
