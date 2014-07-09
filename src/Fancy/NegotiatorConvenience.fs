[<AutoOpen>]
module NegotiatorConvenience
    open System

    open Nancy
    open Nancy.Responses.Negotiation

    /// <summary>
    /// Add a cookie to the response.
    /// </summary>
    /// <param name="negotiator">The <see cref="Negotiator"/> instance.</param>
    /// <param name="cookie">The <see cref="INancyCookie"/> instance that should be added.</param>
    /// <returns>The modified <see cref="Negotiator"/> instance.</returns>
    let addCookie cookie (negotiator:Negotiator) =
        negotiator.NegotiationContext.Cookies.Add (cookie)
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

    ///Header
    type Header = {
        header: string
        value: string
    }
    
    type HeaderOrTuple =
    | Header of Header
    | Tuple of (string * string)

    type ModelOrFactory =
    | Model of obj
    | Factory of Func<obj>

    type StatusCodeOrInt = 
    | StatusCode of HttpStatusCode
    | Number of int

    /// <summary>
    /// Adds headers to the response using anonymous types
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="headers">
    /// Array of headers - each header should be a Tuple with two string elements 
    /// for header name and header value
    /// </param>
    /// <returns>Modified negotiator</returns>
    let addHeaders headers (negotiator:Negotiator) =
        headers 
        |> Seq.map (fun header -> match header with
                                    | Header h -> (h.header, h.value)
                                    | Tuple x -> x)
        |> Seq.iter  (fun (h, v) -> negotiator.NegotiationContext.Headers.[h] <- v )

        negotiator

    /// <summary>
    /// Add a header to the response
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="header">Header name</param>
    /// <param name="value">Header value</param>
    /// <returns>Modified negotiator</returns>
    let addHeader header value negotiator = 
        negotiator |> addHeaders [Tuple(header, value)]

    /// <summary>
    /// Add a content type to the response
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="contentType">Content type value</param>
    /// <returns>Modified negotiator</returns>
    let setContentType contentType negotiator=
        negotiator |> addHeader "Content-Type" contentType

    /// <summary>
    /// Allows the response to be negotiated with any processors available for any content type
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Modified negotiator</returns>
    let withFullNegotiation (negotiator:Negotiator) =
        negotiator.NegotiationContext.PermissableMediaRanges.Clear();
        negotiator.NegotiationContext.PermissableMediaRanges.Add(MediaRange.FromString("*/*"));
        negotiator

    /// <summary>
    /// Allows the response to be negotiated with a specific media range
    /// This will remove the wildcard range if it is already specified
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="mediaRange">Media range to add</param>
    /// <returns>Modified negotiator</returns>
    let setAllowedMediaRange (mediaRange:MediaRange) (negotiator:Negotiator) = 
        let wildcards = 
            negotiator.NegotiationContext.PermissableMediaRanges 
            |> Seq.filter (fun mr -> mr.Type.IsWildcard && mr.Subtype.IsWildcard)
            |> Seq.iter (fun wc -> negotiator.NegotiationContext.PermissableMediaRanges.Remove(wc) |> ignore)

        negotiator.NegotiationContext.PermissableMediaRanges.Add(mediaRange);
        negotiator

    /// <summary>
    /// Uses the specified model as the default model for negotiation
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="model">Model object</param>
    /// <returns>Modified negotiator</returns>
    let addModel model (negotiator:Negotiator) =
        negotiator.NegotiationContext.DefaultModel <- model
        negotiator;

    /// <summary>
    /// Uses the specified view for html output
    /// </summary>
    /// <param name="negotiator">Negotiator object</param>
    /// <param name="viewName">View name</param>
    /// <returns>Modified negotiator</returns>
    let setView viewName (negotiator:Negotiator) =
        negotiator.NegotiationContext.ViewName <- viewName;
        negotiator

    /// <summary>
    /// Sets the model to use for a particular media range.
    /// Will also add the MediaRange to the allowed list
    /// </summary>
    /// <param name="range">Range to match against</param>
    /// <param name="modelFactory">Model DU either an object or a model factory</param>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Updated negotiator object</returns>
    let addMediaRangeModel range model (negotiator:Negotiator) =
        let tmodel = match model with
                     | Factory x -> x
                     | Model x -> Func<obj>(fun () -> x)

        negotiator.NegotiationContext.PermissableMediaRanges.Add(range);
        negotiator.NegotiationContext.MediaRangeModelMappings.Add(range, tmodel);
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
    let setReasonPhrase reasonPhrase (negotiator:Negotiator) = 
        negotiator.NegotiationContext.ReasonPhrase <- reasonPhrase
        negotiator

    /// <summary>
    /// Sets the status code that should be assigned to the final response.
    /// </summary>
    /// <param name="statusCode">The status code that should be used.</param>
    /// <param name="negotiator">Negotiator object</param>
    /// <returns>Updated negotiator object</returns>
    let setStatusCode statusCode (negotiator:Negotiator) =
        let code = match statusCode with
                   | Number x-> enum x
                   | StatusCode x-> x

        negotiator.NegotiationContext.StatusCode <- Nullable(code)
        negotiator