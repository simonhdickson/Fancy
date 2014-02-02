module Program
    open System   
    open Nancy  
    open Nancy.Responses.Negotiation
    open Nancy.Hosting.Self
    open Fancy
    open Microsoft.FSharp.Control
    open Printf

    type Person = { name:string }
    type Square = { result:int }


    let barry x = async { return x }

    type TestModule() as this = 
        inherit NancyModule()
      
        do fancy this {
            get "/%s" (fun s -> fancyAsync {
                return this.Negotiate.WithModel({name = this.s})
            })

            get "/test" (fun () -> fancyAsync {
                return System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()
            })
        }

        member this.s = "Bert"
    
    let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
    nancyHost.Start()  
    Console.ReadLine() |> ignore