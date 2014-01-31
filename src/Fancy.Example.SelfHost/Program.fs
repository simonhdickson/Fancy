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


let pipeline =
    fancy {
        get "/%s" (fun this s -> fancyAsync {

            let! b = barry "hi"
            return {
                name = "Hallo!"
            }
        })
        //get "/%A" (fun (Alpha name) -> asJson { name=name }) 
        //get "/square/%i" (fun number -> asXml { result=number*number }) 
    }

type Pipeline() = inherit Fancy(pipeline)

let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore