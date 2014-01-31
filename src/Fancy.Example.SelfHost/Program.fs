module Program
open System   
open Nancy  
open Nancy.Responses.Negotiation
open Nancy.Hosting.Self
open Fancy
open Microsoft.FSharp.Control

type Person = { name:string }
type Square = { result:int }


let barry x = async { return x }

//
//let pipeline =
//
//        //get "/%A" (fun (Alpha name) -> asJson { name=name }) 
//        //get "/square/%i" (fun number -> asXml { result=number*number }) 
//    }

type Pipeline() as this = 
    inherit NancyModule()
   
    let fancy = new FancyBuilder<Pipeline>(this)
   
    let routes = fancy {
        get "/%s" (fun s -> fancyAsync {
            return this.s
        })
    }
    do Fancy.exec routes this |> ignore

    member this.s = "Bert"



let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore