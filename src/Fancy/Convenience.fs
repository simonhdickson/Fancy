[<AutoOpen>]
module Convenience
    open System
    open System.IO
    
    open Nancy
    open Nancy.Cookies
    open Nancy.Responses.Negotiation

    ///Header
    type Header = {
        header: string
        value: string list
    }
    
    type HeaderOrTuple =
    | Header of Header
    | HeaderTuple of (string * string list)

    type ModelOrFactory =
    | Model of obj
    | Factory of Func<obj>

    type StatusCodeOrInt = 
    | StatusCode of HttpStatusCode
    | Number of int

    type ResponseOrNegotiator =
    | Response of Response
    | Negotiator of Negotiator

    type CookieOrTuple =
    | Cookie of INancyCookie
    | CookieTuple of (string * string)
    | TupleWithExpr of (string * string * DateTime)

    /// <summary>
    /// Add a cookie to the response.
    /// </summary>
    /// <param name="negotiator">The <see cref="Negotiator"/> instance.</param>
    /// <param name="cookie">The <see cref="INancyCookie"/> instance that should be added.</param>
    /// <returns>The modified <see cref="Negotiator"/> instance.</returns>
    let addCookie cookie (negotiator:ResponseOrNegotiator) =
        let tcookie =      
            match cookie with
            | Cookie c -> c
            | CookieTuple ct -> 
                let (name, value) = ct
                NancyCookie(name, value) :> _
            | TupleWithExpr te ->
                let (name, value, expr) = te
                let c = NancyCookie(name, value)
                c.Expires <- Nullable(expr)
                c :> _
                
        match negotiator with
        | Negotiator n -> n.NegotiationContext.Cookies.Add(tcookie)
        | Response r -> r.Cookies.Add(tcookie)

        negotiator

    /// <summary>
    /// Add a collection of cookies to the response.
    /// </summary>
    /// <param name="negotiator">The <see cref="Negotiator"/> instance.</param>
    /// <param name="cookies">The <see cref="INancyCookie"/> instances that should be added.</param>
    /// <returns>The modified <see cref="Negotiator"/> instance.</returns>
    let addCookies cookies negotiator = 
        cookies |> Seq.iter (fun c -> negotiator |> addCookie c |> ignore) 
        negotiator

    /// <summary>
    /// Adds headers to the response using anonymous types
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="headers">
    /// Array of headers - each header should be a Tuple with two string elements 
    /// for header name and header value
    /// </param>
    /// <returns>Modified negotiator</returns>
    let addHeaders headers (negotiator:ResponseOrNegotiator) =
        headers 
        |> Seq.map (fun header -> match header with
                                  | Header h -> (h.header, h.value)
                                  | HeaderTuple x -> x)
        |> Seq.iter  (fun (h, v) ->  match negotiator with
                                     | Response x -> v |> Seq.iter (fun e -> x.Headers.Add(h, e))
                                     | Negotiator x -> x.NegotiationContext.Headers.[h] <- v |> String.concat ", " )

        negotiator

    /// <summary>
    /// Add a header to the response
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="header">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>Modified negotiator</returns>
    let addHeader header value negotiator = 
        negotiator |> addHeaders [HeaderTuple(header, value)]

    /// <summary>
    /// Add a content type to the response
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="contentType">Content type value</param>
    /// <returns>Modified negotiator</returns>
    let setContentType contentType negotiator =
        match negotiator with 
        | Response r -> r.ContentType <- contentType
        | Negotiator n -> n.WithContentType(contentType) |> ignore

        negotiator 

    /// <summary>
    /// Allows the response to be negotiated with any processors available for any content type
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Modified negotiator</returns>
    let withFullNegotiation (negotiator:ResponseOrNegotiator) =
        match negotiator with 
        | Negotiator n ->         
            n.NegotiationContext.PermissableMediaRanges.Clear();
            n.NegotiationContext.PermissableMediaRanges.Add(MediaRange "*/*");
        | Response r -> 
            raise (exn "Responses don't support negotiation use the Negotiator")
        negotiator

    /// <summary>
    /// Allows the response to be negotiated with a specific media range
    /// This will remove the wildcard range if it is already specified
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="mediaRange">Media range to add</param>
    /// <returns>Modified negotiator</returns>
    let setAllowedMediaRange (mediaRange:MediaRange) (negotiator:ResponseOrNegotiator) = 
        match negotiator with 
        | Negotiator n ->         
            n.NegotiationContext.PermissableMediaRanges 
            |> Seq.filter (fun mr -> mr.Type.IsWildcard && mr.Subtype.IsWildcard)
            |> Seq.iter (fun wc -> n.NegotiationContext.PermissableMediaRanges.Remove(wc) |> ignore)
            n.NegotiationContext.PermissableMediaRanges.Add(mediaRange);
        | Response r -> 
            raise (exn "Responses don't support negotiation use the Negotiator")
        negotiator

    /// <summary>
    /// Uses the specified model as the default model for negotiation
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="model">Model object</param>
    /// <returns>Modified negotiator</returns>
    let addModel model (negotiator:ResponseOrNegotiator) =
        match negotiator with 
        | Negotiator n ->         
            n.NegotiationContext.DefaultModel <- model;
        | Response r -> 
            raise (exn "Can't set the model of a response you need to tell it what that model looks like if you have serveral representation of the model use ResponseNegotiation.")
        negotiator

    /// <summary>
    /// Uses the specified view for html output
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="viewName">View name</param>
    /// <returns>Modified negotiator</returns>
    let setView viewName (negotiator:ResponseOrNegotiator) =
        match negotiator with
        | Negotiator n -> n.NegotiationContext.ViewName <- viewName;
        | Response r -> raise (exn "You should use this.View[\"viewName\"] if you don't want to negotiate the response")
        negotiator

    /// <summary>
    /// Sets the model to use for a particular media range.
    /// Will also add the MediaRange to the allowed list
    /// </summary>
    /// <param name="range">Range to match against</param>
    /// <param name="modelFactory">Model DU either an object or a model factory</param>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Updated negotiator object</returns>
    let addMediaRangeModel range model (negotiator:ResponseOrNegotiator) =
        let tmodel = match model with
                     | Factory x -> x
                     | Model x -> Func<obj>(fun () -> x)
        
        match negotiator with 
        | Negotiator n ->         
            n.NegotiationContext.PermissableMediaRanges.Add(range);
            n.NegotiationContext.MediaRangeModelMappings.Add(range, tmodel);
        | Response r -> 
            raise (exn "Can't set the model of a response you need to tell it what that model looks like if you have serveral representation of the model use ResponseNegotiation.")
        negotiator
        
    /// <summary>
    /// Sets the <see cref="Response"/> to use for a particular media range.
    /// Will also add the MediaRange to the allowed list
    /// </summary>
    /// <param name="range">Range to match against</param>
    /// <param name="responseFactory">Factory for returning the <see cref="Response"/> object</param>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Updated negotiator object</returns>
    let addMediaRangeResponse range responseFactory negotiator =
        negotiator
        |> addMediaRangeModel range responseFactory

    /// <summary>
    /// Sets the description of the status code that should be assigned to the final response.
    /// </summary>
    /// <param name="reasonPhrase">The status code description that should be used.</param>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Updated negotiator object</returns>
    let setReasonPhrase reasonPhrase (negotiator:ResponseOrNegotiator) = 
        match negotiator with 
        | Negotiator n -> n.NegotiationContext.ReasonPhrase <- reasonPhrase
        | Response r -> r.ReasonPhrase <- reasonPhrase
        negotiator

    /// <summary>
    /// Sets the status code that should be assigned to the final response.
    /// </summary>
    /// <param name="statusCode">The status code that should be used.</param>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Updated negotiator object</returns>
    let statusCode statusCode (negotiator:ResponseOrNegotiator) =
        let code = match statusCode with
                   | Number x-> enum x
                   | StatusCode x-> x

        match negotiator with
        | Negotiator x -> x.NegotiationContext.StatusCode <- Nullable(code)
        | Response x -> x.StatusCode <- code
        
        negotiator

    ///add a delegate at the end of the list of stream editors
    let addToContents editor (response:ResponseOrNegotiator) = 
        match response with 
        | Response r -> r.Contents <- r.Contents ++ Action<Stream>(editor)
        | Negotiator n -> raise (exn "Can't set the contents of a Negotiator because it's content-type is negotiated try setting a model")
        response

    ///contents
    let contents editor (response:ResponseOrNegotiator) = 
        match response with 
        | Response r -> r.Contents <- Action<Stream>(editor)
        | Negotiator n -> raise (exn "Can't set the contents of a Negotiator because it's content-type is negotiated try setting a model")
        response

    ///forces the contents of the response to be downloaded.
    let asAttachment file (response:ResponseOrNegotiator) = 
        match response with 
        | Response r -> Response(r.AsAttachment(file))
        | Negotiator n -> raise (exn "Can't negotiate an attachment beacuse it's content-type can't be negotiated")
        