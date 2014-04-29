[<AutoOpen>]
module Convenience
    open Nancy
    open System
    open System.IO

    //Convenience methods for response

    ///add a delegate at the end of the list of stream editors
    let addToContents editor (response:Response) = 
        response.Contents <- response.Contents ++ Action<Stream>(editor)
        response

    ///contents
    let contents editor (response:Response) = 
        response.Contents <- Action<Stream>(editor)
        response

    ///add a header to the reponse
    let addHeader headers (response:Response) = 
         match headers with 
         | (key, value) -> response.Headers.Add(key, value)
         response

    ///add a list of headers to the response
    let addHeaders headers response = 
        headers
        |> Seq.iter (fun h -> addHeader h response |> ignore)
        response

    ///adds a cookie
    let addCookie name value (response:Response) = 
        response.WithCookie(name, value) 

    ///adds a cookie with expiration
    let addCookieWithExpr name value expires (response:Response) = 
        let exp = Nullable expires 
        response.WithCookie(name, value, exp) 

    ///set the statusCode
    let statusCode status (response:Response) = 
        response.StatusCode <- status

    ///forces the contents of the response to be downloaded.
    let asAttachment file (response:Response) = 
        response.AsAttachment(file)