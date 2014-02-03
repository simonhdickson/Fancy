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

    let urlVarRegex = Regex(@"%[\w-\._~]+", RegexOptions.Compiled)

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
    let getParameters (instance:obj) =
        instance.GetType().GetMethods().[0].GetParameters()
        |> Seq.map (fun parameter -> parameter.Name, parameter.ParameterType)
        |> Seq.where (fun (_,parameterType) -> parameterType <> typeof<unit>)  

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
            | 0 -> instance.GetType().GetMethods().[0].Invoke(instance, [|()|])
            | _ -> instance.GetType().GetMethods().[0].Invoke(instance, parameters) 
            |> unbox
    }

    let toNancyParameter input =
        match input with
        | name, sType when sType = typeof<int> -> "{" + name + ":int}"
        | name, sType when sType = typeof<bool> -> "{" + name + ":bool}"
        | name, sType when sType = typeof<decimal> -> "{" + name + ":decimal}"
        | name, sType when sType = typeof<Guid> -> "{" + name + ":guid}"
        | name, sType when sType = typeof<DateTime> -> "{" + name + ":datetime}"
        | name, sType -> "{" + name + "}"

    let rec formatNancyString inputString (types: (string * Type) list) =
        match types with
        | [] -> inputString
        | head::tail -> formatNancyString (urlVarRegex.Replace (inputString, (toNancyParameter head), 1)) (tail)
        
    let requestWrapper parameters (processor) (dictionary:obj) = async {
        return!
            (dictionary :?> DynamicDictionary)
            |> dynamicDictionaryToMap
            |> matchParameters parameters
            |> Seq.toArray
            |> invokeFunction processor
    }

    let parseUrl url processor =              
        let parameters = getParameters processor
        let nancyString = (formatNancyString url (parameters |> Seq.toList))
        (nancyString, parameters)
                                                         
    type FancyBuilder(nancyModule: NancyModule) =
        member this.Yield(a) = a

        member private this.routeDelegateBuilder (processor, parameters) = 
            fun dictionary cancelationToken -> 
                Async.StartAsTask (requestWrapper parameters processor dictionary)

        [<CustomOperation("get")>]
        member this.Get (source, url:StringFormat<'a, 'z>, processor:'a) =
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Get.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)
            
        [<CustomOperation("post")>]
        member this.Post (source, url:StringFormat<'a, 'z>, processor:'a) =
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Post.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)
        
        [<CustomOperation("put")>]
        member this.Put (source, url:StringFormat<'a, 'z>, processor:'a) =
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Put.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)        

        [<CustomOperation("delete")>]
        member this.Delete (source, url:StringFormat<'a, 'z>, processor:'a) =
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Delete.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)

        [<CustomOperation("options")>]
        member this.Options (source, url:StringFormat<'a, 'z>, processor:'a) =
            let (parsedUrl, parameters) = parseUrl url.Value processor
            do nancyModule.Options.[parsedUrl, true] <- this.routeDelegateBuilder (processor, parameters)
    
    let fancy m = new FancyBuilder(m)
