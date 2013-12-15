module Program
open System
open Nancy        
open Nancy.Bootstrapper       
open Nancy.Hosting.Self
open Fancy

let pipeline =
    fancy {
        get "/" (fun () -> sprintf "Hello World!")
        get "/%s" (fun name -> sprintf "Hello %s!" name) 
        get "/square/%i" (fun number -> sprintf "%i" <| number * number) 
    }

type FSharpNancy() as this =
    inherit NancyModule()
    do
        exec pipeline this |> ignore

let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore