module Fancy
open System 
open System.Linq 
open System.ComponentModel   
open System.Dynamic
open Printf

open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations   
open Microsoft.FSharp.Quotations.Patterns 
open Microsoft.FSharp.Quotations.DerivedPatterns
open Nancy             
open Nancy.Bootstrapper
open Nancy.Responses.Negotiation

open Common

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
    FSharpValue.MakeUnion(unionInfo, [| parameter |])

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
    |> Seq.map (fun key -> key, (dict.[key] :?> DynamicDictionaryValue).Value)
    |> Map.ofSeq

let invokeFunctionObj<'b> (instance:obj) parameters =
    let res = 
        match Array.length parameters with
        | 0 -> instance.GetType().GetMethods().[0].Invoke(instance, [|()|]) 
        | _ -> instance.GetType().GetMethods().[0].Invoke(instance, parameters)
    res :?> 'b

let invokeFunction (instance:'a->'b) parameters =
    invokeFunctionObj<'b> instance parameters

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
 //   | "%g", _ -> "{%s:guid}"
    | "%A", sType -> "{%s:" + sType.Name.ToLower() + "}"
    | _ -> failwith "Unsupported"

let formatNancyString inputString (types:Type array) =
    applyReplace inputString "%." (fun (s, i) -> toNancyParameter (s, types.[i]))

let requestWrapper parameters processor (dictionary:obj) =
    (dictionary :?> DynamicDictionary)
    |> dynamicDictionaryToMap
    |> matchParameters parameters
    |> Seq.toArray
    |> invokeFunction processor

let parseUrl (url:string) processor =              
    let formatArgCount = url.Count(fun i -> i = '%')
    let parameters = getParameters processor
    let nancyString = (formatNancyString url (parameters |> Seq.map snd |> Seq.toArray))
    let paramterNames = (Seq.map (fun (i,_) -> i) parameters |> Seq.take formatArgCount |>  Seq.toList)
    let url' = printHelper nancyString paramterNames
    (url', parameters)

/// This tuple is the response summary information we use to let Nancy build it
type response<'b> = int * 'b * (string * string) list
/// This function takes a response and returns a Nancy response negoriator 
let negotiate (src:response<_>) (negotiator:Nancy.Responses.Negotiation.Negotiator) =
    match src with
    | code, content, headers -> 
        box (negotiator
            .WithStatusCode(code)
            .WithHeaders(List.toArray headers)
            .WithModel(content))
    //| _ -> failwith "invalid response %A" src    

        


/// This is derived from the StateBuilder in fsharpx                  
type State<'T, 'State> = 'State -> 'T * 'State
let getState = fun s -> s
let putState s = fun _ -> s
let exec m s = m s |> snd
let bind k m = fun s -> let (a, s') = m s in (k a) s'

// do x.Post.["/{x:Guid}", true] <- fun a c -> Async.StartAsTask ( async { 
//      let id = toGuid a?x 
//        return
//          a |> auth ("claim")
//            |> bind
//            |> validate
//            |> translate
//            |> logic <T> ()
//            |> store
//            |> (fun y -> async {
//                  return (201, (), ("Location", x.Request.Path + y.Id ))                
//            })
//        // auth
//        // binden (serialize and validate)
//        // vertalen
//        // opslaan
//        // notify pubsub of queue
//        Console.WriteLine(a?x.ToString())
//        return box (x.Negotiate.WithHeader("Location", "id").WithStatusCode(201))
//    })

type build = NancyModule -> unit
                                      
type FancyBuilder() =

    member this.Return(nancyModule:build) = fun x -> nancyModule x
    member this.Bind(nancyModule, expr) = expr nancyModule
    member this.Combine(r1, r2) = this.Bind(r1, fun () -> r2)

    [<CustomOperation("get", MaintainsVariableSpaceUsingBind=true)>]
    member this.Get(m, url:StringFormat<'a->'b,'c>, processor:'a-> Negotiator) =
        let (url', parameters) = parseUrl url.Value processor
        this.Bind(m, fun (nancyModule:NancyModule) ->
            do nancyModule.Get.[url', true] <- fun i cancelationToken ->  Async.StartAsTask ( async { 
                let result = (requestWrapper parameters processor i) 
                return negotiate result nancyModule.Negotiate })
            nancyModule)  

//    [<CustomOperation("post", MaintainsVariableSpaceUsingBind=true)>]
//    member this.Post(m, url:StringFormat<'a->'b,'c>, processor:'a->(IResponseFormatter->'b)) =
//        let (url', parameters) = parseUrl url.Value processor
//        this.Bind(m, fun _ ->
//            this.Bind(getState, fun (nancyModule:NancyModule) ->
//                do nancyModule.Post.[url'] <- fun i -> let result = requestWrapper nancyModule parameters processor i
//                                                       invokeFunctionObj result [|nancyModule.Response|]
//                putState nancyModule))


let fancy = new FancyBuilder()


[<AbstractClass>]
type Fancy(build) as this =
    inherit NancyModule()
    do
        build this |> ignore

//type Alpha = Alpha of string
//
//let asPlainText output (this:IResponseFormatter) =
//    this.AsText(output)
//
//let asJson output (this:IResponseFormatter) =
//    this.AsJson(output)
//
//let asXml output (this:IResponseFormatter) =
//    this.AsXml(output)
