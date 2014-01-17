(*** hide ***)
#I "../../bin"
#r "Nancy.dll"
#r "Fancy"   
#r "Nancy.Hosting.Self.dll"

open System   
open Nancy  
open Nancy.Hosting.Self
open Fancy

let pipeline =
    fancy {
        get "/" (fun () -> asPlainText "Hello World!")
    }     

type Pipeline() = inherit Fancy(pipeline)

let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore
