module Program
open System   
open Nancy  
open Nancy.Hosting.Self
open Fancy

type Person = { name:string }
type Square = { result:int }

let pipeline =
    fancy {
        get "/" (fun () -> asPlainText "Hello World!")
        get "/%A" (fun (Alpha name) -> asJson { name=name }) 
        get "/square/%i" (fun number -> asXml { result=number*number }) 
    }

type Pipeline() = inherit Fancy(pipeline)

let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore