module Fancy
open System  
open System.ComponentModel   
open System.Dynamic
open System.Text.RegularExpressions
open Printf

open Microsoft.FSharp.Quotations   
open Microsoft.FSharp.Quotations.Patterns 
open Microsoft.FSharp.Quotations.DerivedPatterns

open Nancy            
open Nancy.Bootstrapper             
open Nancy.Hosting.Self

let getParameters (instance:'a->'b) =
    instance.GetType().GetMethods().[0].GetParameters()
    |> Seq.map (fun parameter-> parameter.Name, parameter.ParameterType)
    |> Seq.where (fun (_,parameterType) -> parameterType <> typeof<unit>)
        
let matchParameters parameters (dict:DynamicDictionary) =
    parameters
    |> Seq.map (fun (name:string,targetType) -> ((dict.[name] :?> DynamicDictionaryValue).Value,targetType))
    |> Seq.map (fun (value,targetType) ->
        match value with 
        | x when x.GetType() = targetType -> x
        | x ->
            let converter = TypeDescriptor.GetConverter(targetType)
            match converter.CanConvertFrom(x.GetType()) with
            | true -> converter.ConvertFrom(x)
            | false -> Convert.ChangeType(x, targetType))

let invokeFunction (instance:'a->'b) parameters =
    match Array.length parameters with
    | 0 -> instance.GetType().GetMethods().[0].Invoke(instance, [|()|])
    | _ -> instance.GetType().GetMethods().[0].Invoke(instance, parameters)

let rec printHelper<'a> (fmt:string) (list:string list) : 'a =
    match list with
    | [] -> sprintf <| Printf.StringFormat<_> fmt
    | s :: rest -> printHelper<string -> 'a> fmt rest s
    
let toNancy parameter =
    sprintf "%s" parameter

let formatNancyString inputString =
    Regex.Replace(inputString, "%i", "{%s:int}")

type State<'T, 'State> = 'State -> 'T * 'State
let getState = fun s -> (s,s)
let putState s = fun _ -> ((),s)
let eval m s = m s |> fst
let exec m s = m s |> snd
let empty = fun s -> ((), s)
let bind k m = fun s -> let (a, s') = m s in (k a) s'
                                                            
type NancyBuilder() =
    member this.Return(a) : State<'T,'State> = fun s -> (a,s)
    member this.Bind(m:State<'T,'State>, k:'T -> State<'U,'State>) : State<'U,'State> = bind k m
    member this.Combine(r1, r2) = this.Bind(r1, fun () -> r2)
    [<CustomOperation("get", MaintainsVariableSpaceUsingBind=true)>]
    member this.Get(m, url:StringFormat<'a->'b,'c>, processor:'a->'b) =
        let parameters = getParameters processor
        let url' = printHelper (formatNancyString url.Value) (Seq.map (fun (i,_) -> i) parameters |> Seq.map toNancy |> Seq.toList)
        this.Bind(m, fun _ ->
            this.Bind(getState, fun (nancyModule:NancyModule) ->
                do nancyModule.Get.[url'] <-
                    fun i ->
                        (i :?> DynamicDictionary)
                        |> matchParameters parameters
                        |> Seq.toArray
                        |> invokeFunction processor
                putState nancyModule))
let nancy = new NancyBuilder()

let pipeline =
    nancy {
        get "/" (fun () -> sprintf "Hello World!")
        get "/%s" (fun name -> sprintf "Hello %s!" name) 
        get "/square/%i" (fun number -> sprintf "%i" <| number * number) 
    }

type FSharpNancy() as this =
    inherit NancyModule()
    do exec pipeline this |> ignore

type FSharpBootstrapper() =
    inherit DefaultNancyBootstrapper()
    override m.Modules =
        let modules = base.Modules
        seq {
            yield! modules              
            yield ModuleRegistration(typeof<FSharpNancy>)
        }

let nancyHost = new NancyHost(new FSharpBootstrapper(), Uri "http://localhost:8888/nancy/")
nancyHost.Start()
Console.ReadKey() |> ignore
nancyHost.Dispose()
