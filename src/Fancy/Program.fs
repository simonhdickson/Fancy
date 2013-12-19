module Fancy
open System  
open System.ComponentModel   
open System.Dynamic
open System.Text.RegularExpressions
open Printf

open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations   
open Microsoft.FSharp.Quotations.Patterns 
open Microsoft.FSharp.Quotations.DerivedPatterns

open Nancy             
open Nancy.Bootstrapper

/// Takes an object that represents a function of a'->'b and returns a list of all parameters
/// required to invoke it
let getParametersFromObj (instance:obj) =
    instance.GetType().GetMethods().[0].GetParameters()
    |> Seq.map (fun parameter-> parameter.Name, parameter.ParameterType)
    |> Seq.where (fun (_,parameterType) -> parameterType <> typeof<unit>)  
      
let getParameters (instance:'a->'b) =
    getParametersFromObj instance

let makeSingleUnion (unionType:Type) (parameter:obj) =
    let unionInfo = FSharpType.GetUnionCases(unionType) |> Seq.exactlyOne
    FSharpValue.MakeUnion(unionInfo, [|parameter|])

/// This function converts a value between equivalent types
/// ie. from int64 -> int32
let changeOrConvertType value (targetType:Type) =
    let converter = TypeDescriptor.GetConverter targetType
    match converter.CanConvertFrom(value.GetType()) with
    | true -> converter.ConvertFrom(value)
    | false -> Convert.ChangeType(value, targetType)
    
let matchParameters expectedParameters (dict:Map<_,_>) =
    expectedParameters
    |> Seq.map (fun (name:string,targetType) -> dict.[name], targetType)
    |> Seq.map (fun (value,targetType) ->
        match value with 
        | x when x.GetType() = targetType -> x  
        | x when FSharpType.IsUnion(targetType) -> makeSingleUnion targetType x 
        | x -> changeOrConvertType x targetType)

let dynamicDictionaryToMap (dict:DynamicDictionary) =
    dict
    |> Seq.map (fun key -> key,dict.[key])
    |> Map.ofSeq

let invokeFunction (instance:'a->'b) parameters =
    match Array.length parameters with
    | 0 -> instance.GetType().GetMethods().[0].Invoke(instance, [|()|])
    | _ -> instance.GetType().GetMethods().[0].Invoke(instance, parameters)

let rec printHelper<'a> (fmt:string) (list:string list) : 'a =
    match list with
    | [] -> sprintf <| Printf.StringFormat<_> fmt
    | s :: rest -> printHelper<string -> 'a> fmt rest s

let toNancyParameter input =
    match input with
    | _, sType when sType = typeof<string> -> "{%s}" 
    | "%i", _ -> "{%s:int}"
    | "%b", _ -> "{%s:bool}"
    | "%d", _ -> "{%s:decimal}"
    | "%A", sType -> "{%s:" + sType.Name.ToLower() + "}"
    | _ -> failwith "Unsupported"

let formatNancyString inputString (types:Type array) =
    Regex.Replace(inputString, "%.", fun (m:Match) -> toNancyParameter (m.Value,types.[m.Index-1]))

let requestWrapper parameters processor (dictionary:obj) =
    (dictionary :?> DynamicDictionary)
    |> dynamicDictionaryToMap
    |> matchParameters parameters
    |> Seq.toArray
    |> invokeFunction processor

let parseUrl url processor =
    let parameters = getParameters processor
    let url' = printHelper (formatNancyString url (parameters |> Seq.map snd |> Seq.toArray)) (Seq.map (fun (i,_) -> i) parameters |> Seq.toList)
    (url', parameters)

/// This is derived from the StateBuilder in fsharpx                  
type State<'T, 'State> = 'State -> 'T * 'State
let getState = fun s -> (s,s)
let putState s = fun _ -> ((),s)
let eval m s = m s |> fst
let exec m s = m s |> snd
let empty = fun s -> ((), s)
let bind k m = fun s -> let (a, s') = m s in (k a) s'
                                          
type FancyBuilder() =
    member this.Return(a) : State<'T,'State> = fun s -> (a,s)
    member this.Bind(m:State<'T,'State>, k:'T -> State<'U,'State>) : State<'U,'State> = bind k m
    member this.Combine(r1, r2) = this.Bind(r1, fun () -> r2)
    [<CustomOperation("get", MaintainsVariableSpaceUsingBind=true)>]
    member this.Get(m, url:StringFormat<'a->'b,'c>, processor:'a->'b) =
        let (url', parameters) = parseUrl url.Value processor
        this.Bind(m, fun _ ->
            this.Bind(getState, fun (nancyModule:NancyModule) ->
                do nancyModule.Get.[url'] <- fun i -> requestWrapper parameters processor i
                putState nancyModule))  
    [<CustomOperation("post", MaintainsVariableSpaceUsingBind=true)>]
    member this.Post(m, url:StringFormat<'a->'b,'c>, processor:'a->'b) =
        let (url', parameters) = parseUrl url.Value processor
        this.Bind(m, fun _ ->
            this.Bind(getState, fun (nancyModule:NancyModule) ->
                do nancyModule.Post.[url'] <- fun i -> requestWrapper parameters processor i
                putState nancyModule))
let fancy = new FancyBuilder()

type Fancy(pipeline:State<unit,NancyModule>) as this =
    inherit NancyModule()
    do
        exec pipeline this |> ignore

type Alpha = Alpha of string