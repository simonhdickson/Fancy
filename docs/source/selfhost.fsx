(*** hide ***)
#I "../../bin"
#r "Nancy.dll"
#r "Fancy"   
#r "Nancy.Hosting.Self.dll"

open System   
open Nancy  
open Nancy.Hosting.Self
open Fancy

type ExampleModule() as this = 
    inherit Nancy.NancyModule()
    do fancy this {
        get "/" (fun () -> fancyAsync { return "Hello World!" } )
    }


let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore
