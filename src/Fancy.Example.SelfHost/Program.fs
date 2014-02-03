module Program
    open System   
    open System.IO
    open System.Threading 
    open Nancy  
    open Nancy.Responses.Negotiation
    open Nancy.Hosting.Self
    open Fancy
    open Operators
    open Microsoft.FSharp.Control
    open Printf

    type Person = { name:string }
    type Square = { result:int }

    type TestModule() as this = 
        inherit NancyModule()
        do Fancy.fancy this {
            before (fun ctx c -> async {
                printf "Hello from the before thingy"
                return null
            })

            get "/%s/%d/%s" (fun s i s1 (c:CancellationToken) -> fancyAsync {
                let a = "%"
                return this.Negotiate.WithModel({name = this.s})
            })

            get "/tes" (fun () -> fancyAsync {
                return "hond!"
            })

            after (fun ctx c -> async {
                ctx.Response.Contents <- ctx.Response.Contents ++ Action<Stream>(fun s -> s.Write(System.Text.Encoding.UTF8.GetBytes("Ja zeker!"), 0, 8))
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