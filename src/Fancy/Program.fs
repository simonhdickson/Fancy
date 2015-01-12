module Fancy
    open System 
    open System.IO
    open System.Threading
    open System.Reflection
    open System.Linq 
    open System.ComponentModel   
    open Printf
    open Microsoft.FSharp.Reflection
    open Nancy             
    open System.Text.RegularExpressions

    let urlVarRegex = Regex(@"%[\w-\._~]+", RegexOptions.Compiled)

    let peel p = (p, p.GetType().GetMethods().[0])
    
    /// Because nancy expects an object to do with as she may see fit
    /// and because we want to handle our routes in an async context
    /// we added the box function to the return function of the AsyncBuilder.
    /// A fancy function now has the signature 'a -> Async<obj>,
    /// but you can choose to return a string, a Nancy Negotiator, .net object, JSON
    /// or whatever Nancy can serialize to the requested content type. 
    type BoxedAsyncBuilder () =
        member this.Return x = 
            match box(x) with 
            | :? ResponseOrNegotiator as rn ->
                match rn with 
                | Response r -> r |> box |> async.Return
                | Negotiator n -> n |> box |> async.Return
            | _  as x -> x |> async.Return         

        member this.Bind (comp, binder) = async.Bind (comp, binder)
        member this.ReturnFrom comp = async.ReturnFrom comp
        member this.Using (resource, binder) = async.Using (resource, binder)
        member this.TryFinally (comp, compensation) = async.TryFinally (comp, compensation)
        member this.TryWith (comp, handler) = async.TryWith (comp, handler)
        member this.Delay gen = async.Delay gen

        member this.Zero () = async {
            return box null
        }

    /// Fancy specific async builder.
    /// <see cref="Fancy.BoxedAsyncBuilder">This to ensure we benefit from all Nancy's goodness</see>
    let fancyAsync = new BoxedAsyncBuilder ()

    /// Takes an object that represents a function of 'a->'b and returns a list of all parameters
    /// required to invoke it
    let getParameters (instance:(obj * MethodInfo)) =
        match instance with
        | (_, meth) -> meth.GetParameters()
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
    
    let matchParameters expectedParameters cancellationToken (dict:Map<_,_>) =
        expectedParameters
        |> Seq.map (fun (name:string,targetType) -> 
            match (Map.containsKey name dict) with
            | true -> dict.[name], targetType
            | false -> match targetType with 
                       | t when t = typeof<CancellationToken> -> cancellationToken, targetType
                       | _ -> failwith (sprintf "Unmapped Parameter! type: %O name: %s" targetType name)
                
        )
        |> Seq.map (fun (value,targetType) ->
            match value with 
            | x when x.GetType() = targetType -> x  
            | x when FSharpType.IsUnion(targetType) -> makeSingleUnion targetType x 
            | x -> changeOrConvertType x targetType)

    let dynamicDictionaryToMap (dict:DynamicDictionary) =
        dict
        |> Seq.map (fun key -> key, (dict.[key] :?> DynamicDictionaryValue).Value)
        |> Map.ofSeq
  
    let invokeFunction (instance:(obj * MethodInfo)) parameters  : Async<obj> = async {
        return!
            match instance with 
            | (obj1, instance) -> 
                match Array.length parameters with
                | 0 -> instance.Invoke(obj1, [|()|])
                | _ -> instance.Invoke(obj1, parameters) 
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
        
    let requestWrapper parameters processor (dictionary:obj) cancellationToken = async {
        return!
            (dictionary :?> DynamicDictionary)
            |> dynamicDictionaryToMap
            |> matchParameters parameters cancellationToken
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
            fun dictionary cancellationToken -> 
                Async.StartAsTask (requestWrapper parameters processor dictionary cancellationToken)

        [<CustomOperation("before")>]
        member this.Before(source, processor) =
            do nancyModule.Before.AddItemToEndOfPipeline(
                fun ctx c -> Async.StartAsTask(async {
                    let! res = processor ctx c
                    return match res with 
                                | Some x -> x
                                | None -> null
                }))
                
        [<CustomOperation("after")>]
        member this.After(source, processor) =
            do nancyModule.After.AddItemToEndOfPipeline(fun ctx c -> startAsPlainTask(processor ctx c))
            
        [<CustomOperation("get")>]
        member this.Get (source, url:StringFormat<'a, 'z>, processor:'a) =
            let peeledProcessor = processor |> peel
            let (parsedUrl, parameters) = parseUrl url.Value peeledProcessor
            do nancyModule.Get.[parsedUrl, true] <- this.routeDelegateBuilder (peeledProcessor, parameters)
            
        [<CustomOperation("post")>]
        member this.Post (source, url:StringFormat<'a, 'z>, processor:'a) =
            let peeledProcessor = processor |> peel
            let (parsedUrl, parameters) = parseUrl url.Value peeledProcessor
            do nancyModule.Post.[parsedUrl, true] <- this.routeDelegateBuilder (peeledProcessor, parameters)
        
        [<CustomOperation("put")>]
        member this.Put (source, url:StringFormat<'a, 'z>, processor:'a) =
            let peeledProcessor = processor |> peel
            let (parsedUrl, parameters) = parseUrl url.Value peeledProcessor
            do nancyModule.Put.[parsedUrl, true] <- this.routeDelegateBuilder (peeledProcessor, parameters)        

        [<CustomOperation("delete")>]
        member this.Delete (source, url:StringFormat<'a, 'z>, processor:'a) =
            let peeledProcessor = processor |> peel
            let (parsedUrl, parameters) = parseUrl url.Value peeledProcessor
            do nancyModule.Delete.[parsedUrl, true] <- this.routeDelegateBuilder (peeledProcessor, parameters)

        [<CustomOperation("options")>]
        member this.Options (source, url:StringFormat<'a, 'z>, processor:'a) =
            let peeledProcessor = processor |> peel
            let (parsedUrl, parameters) = parseUrl url.Value peeledProcessor
            do nancyModule.Options.[parsedUrl, true] <- this.routeDelegateBuilder (peeledProcessor, parameters)
    

    /// The fancy compution builder use this to write your modules for nancy in f#
    /// example: 
    /// <c>
    /// type ExampleModule() as this = 
    ///     inherit Nancy.NancyModule()
    ///     do fancy this {
    ///         get "/"  (fun () -> fancyAsync { return "Hello World!" } )
    ///     }
    /// </c>
    /// <param name="m">The Nancy Module</param>
    /// <returns>Unit</returns>
    let fancy m = new FancyBuilder(m)