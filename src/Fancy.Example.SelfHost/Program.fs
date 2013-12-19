module Program
open System
open Nancy        
open Nancy.Bootstrapper       
open Nancy.Hosting.Self
open Fancy

let pipeline =
    fancy {
        get "/" (fun () -> sprintf "Hello World!")
        get "/%A" (fun (Alpha name) -> sprintf "Hello %s!" name) 
        get "/square/%i" (fun number -> sprintf "%i" <| number * number) 
    }

type Pipeline() =
    inherit Fancy(pipeline)

let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore