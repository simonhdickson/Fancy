module Program
    open System   
    open System.IO
    open System.Threading 
    open Nancy  
    open Nancy.Responses
    open Nancy.Responses.Negotiation
    open Nancy.Hosting.Self
    open Fancy
    open Microsoft.FSharp.Control
    open Printf

    let barry = async { return "barry" }

    type Person = { name:string }
    type Square = { result:int }

    type TestModule() as this = 
        inherit NancyModule()
        do fancy this {

            before (fun ctx c -> async {
                printf "Hello from the before thingy"   
                return None
            })

            get "/%s/%d/%s" (fun s i s1 (c:CancellationToken) -> fancyAsync {
                let a = "%"
                return this.Negotiate 
                        |> ResponseOrNegotiator.Negotiator
                        |> addModel (Model {name = this.s}) 
                        |> addHeader "vla" ["yoghurt"]
                        |> statusCode (Number 418)
            })

            get "/tes" (fun () -> fancyAsync {
                return "She's a dog"
            })

            after (fun ctx c -> async {
                match (ctx.Response.ContentType, ctx.Response.StatusCode) with
                | ("application/json; charset=utf-8", _) -> ()
                | (_, HttpStatusCode.NotFound) -> 
                                 ctx.Response
                                 |> ResponseOrNegotiator.Response
                                 |> addHeader "test" ["test"]
                                 |> addToContents (fun s -> 
                                     use sw = new StreamWriter(s)
                                     sw.Write "(tm) (r) (c)")
                                 |> ignore
                | (_,_) ->
                    ctx.Response 
                    |> ResponseOrNegotiator.Response
                    |> contents (fun s -> 
                        use sw = new StreamWriter(s)
                        sw.Write("banananas")) 
                    |> ignore
            })
        }

        member this.s = "Bert"
        
    type ExampleModule() as this = 
        inherit Nancy.NancyModule()
        do fancy this {
            get "/" (fun () -> fancyAsync { return "Hello World!" } )
        }
         
    let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
    nancyHost.Start()  
    Console.ReadLine() |> ignore