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
    open System.Text.RegularExpressions

    /// Because nancy expects an object to do with as she may see fit
    /// and because we want to handle our routes in an async context
    /// we added the box function to the return function of the AsyncBuilder.
    /// A fancy function now has the signature NancyModule -> 'a -> Async<obj>,
    /// but you can choose to return a string, a Nancy Negotiator, .net object, JSON
    /// or whatever Nancy can serialize to the requested content type. 
    type BoxedAsyncBuilder () =
        member this.Bind (computation,binder) = async {
            let! arg = computation
            return! binder arg
        }
        member this.ReturnFrom = async.ReturnFrom
        member this.Return x = async.Return (box x)

    let fancyAsync = new BoxedAsyncBuilder ()

    /// Takes an object that represents a function of a'->'b and returns a list of all parameters
    /// required to invoke it
    let getParametersFromObj (instance:obj) =
        instance.GetType().GetMethods().[0].GetParameters()
        |> Seq.map (fun parameter -> parameter.Name, parameter.ParameterType)
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
  
    let invokeFunction (instance) parameters : Async<obj> = async {
        return! 
            match Array.length parameters with
            | 0 -> unbox (instance.GetType().GetMethods().[0].Invoke(instance, [|()|])) 
            | _ -> unbox (instance.GetType().GetMethods().[0].Invoke(instance, parameters))
    }

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

    let applyReplace inputString regex f =
        let index = ref -1
        Regex.Replace(inputString, regex, fun (m:Match) -> index:=!index+1; f (m.Value,!index))

    let formatNancyString inputString (types:Type array) =
        applyReplace inputString "%." (fun (s, i) -> toNancyParameter (s, types.[i]))

    let requestWrapper parameters (processor) (dictionary:obj) = async {
        return!
            (dictionary :?> DynamicDictionary)
            |> dynamicDictionaryToMap
            |> matchParameters parameters
            |> Seq.toArray
            |> invokeFunction processor
    }

    let parseUrl (url:string) processor =              
        let formatArgCount = url.Count(fun i -> i = '%')
        let parameters = getParameters processor
        let nancyString = (formatNancyString url (parameters |> Seq.map snd |> Seq.toArray))
        let paramterNames = (Seq.map (fun (i,_) -> i) parameters |> Seq.take formatArgCount |>  Seq.toList)
        let url' = printHelper nancyString paramterNames
        (url', parameters)
                                      
    type FancyBuilder(nancyModule: NancyModule) =
        member this.Return(a) = a
        
        member private this.routeDelegateBuilder (processor, parameters) = 
            fun dictionary cancelationToken -> 
                Async.StartAsTask (requestWrapper parameters processor dictionary)

        [<CustomOperation("get", MaintainsVariableSpaceUsingBind=true)>]
        member this.Get(state, url:StringFormat<'a -> 'b,'z>, processor: ('a) -> Async<obj>) =
            // parsing is required for typesafe processor
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Get.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)
        

        [<CustomOperation("post", MaintainsVariableSpaceUsingBind=true)>]
        member this.Post(state, url:StringFormat<'a->'b,'c>, processor: 'a -> 'b -> Async<obj>) =
            // parsing is required for typesafe processor
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Post.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)
        
        [<CustomOperation("put", MaintainsVariableSpaceUsingBind=true)>]
        member this.Put(state, url:StringFormat<'a->'b,'c>, processor: 'a -> 'b -> Async<obj>) =
            // parsing is required for typesafe processor
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Put.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)        

        [<CustomOperation("delete", MaintainsVariableSpaceUsingBind=true)>]
        member this.Delete(state, url:StringFormat<'a->'b,'c>, processor: 'a -> 'b -> Async<obj>) =
            // parsing is required for typesafe processor
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Delete.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)

        [<CustomOperation("options", MaintainsVariableSpaceUsingBind=true)>]
        member this.Options(state, url:StringFormat<'a->'b,'c>, processor: 'a -> 'b -> Async<obj>) =
            // parsing is required for typesafe processor
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Options.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)
    
    let fancy m = new FancyBuilder(m)
