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
            get "/%s/%d/%s" (fun s i s1 -> fancyAsync {
                return this.Negotiate.WithModel({name = this.s})
            })

            get "/tes/%A" (fun (a:Guid) -> fancyAsync {
                return "hond!"
            })
        }

        member this.s = "Bert"
        
    let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
    nancyHost.Start()  
    Console.ReadLine() |> ignore