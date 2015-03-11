namespace Fancy.Tests
open System.IO
open Fancy
open Xunit
open Xunit.Extensions
open FsUnit.Xunit


type Alpha = Alpha of string 

module NancyTests =
    open System.Text
    open System

    let ``example urls`` : obj[] seq =
        seq {
            yield [| "/"; [("", typeof<Unit>)]; "/" |]    
            yield [|"/HelloWorld"; [("", typeof<Unit>)]; "/HelloWorld"|] 
            yield [|"/%s"; [("bananas", typeof<string>)]; "/{bananas}"|]
            yield [|"/%i"; [("monkees", typeof<int>)]; "/{monkees:int}"|]   
            yield [|"/%d"; [("pi", typeof<decimal>)]; "/{pi:decimal}"|]
            yield [|"/%b"; [("yes", typeof<bool>)]; "/{yes:bool}"|]

            yield [|"/%s/%s"; [("banana", typeof<string>); ("peel",typeof<string>)]; "/{banana}/{peel}"|]
            yield [|"/%s/HelloWorld/%s"; [("is", typeof<string>); ("cool", typeof<string>)]; "/{is}/HelloWorld/{cool}"|]
            yield [|"/%d/%i"; [("pi", typeof<decimal>); ("atoms", typeof<int>)]; "/{pi:decimal}/{atoms:int}"|]
            yield [|"/%d/HelloWorld/%i"; [("pi", typeof<decimal>); ("atoms", typeof<int>)]; "/{pi:decimal}/HelloWorld/{atoms:int}"|]    
            
            //yield [|"/%A"; [|typeof<Alpha>|]; "/{%s:alpha}"|]
        }

    [<Theory; PropertyData("example urls")>]
    let ``fancy correctly formats nancy url strings`` url types expected =
        let result = formatNancyString url types
        Assert.Equal<string>(expected, result)

    [<Fact>]
    let ``boxed async Zero should return a Async<obj> (Zero)`` () =
        // Given, When
        let a = fancyAsync {
            ()
        }

        // Then
        a |> should be ofExactType<Async<obj>>
        a |> Async.RunSynchronously |> should equal null

    [<Fact>]
    let ``boxed async builder should box everything retured from it (box)`` () =
        // Given
        let a = fancyAsync {
            return 200
        }

        // When 
        let result = a |> Async.RunSynchronously 

        // Then
        a |> should be ofExactType<Async<obj>>
        result |> unbox<int> |> should equal 200

    [<Fact>]
    let ``should be able to use "use" in a boxed async builder (Using)`` () = 
        // Given
        let a = fancyAsync {
            let! a = async {
                return ()
            }
            use ms = new MemoryStream ()
            use sr = new StreamWriter (ms)
            sr.Write("A beautifull poem")
            return ms
        }
        // When
        let result = a |> Async.RunSynchronously
        let stream = result |> unbox<MemoryStream>
        
        // Then
        stream.ToArray () |> should equal (Encoding.UTF8.GetBytes("A beautifull poem"))

    [<Fact>]
    let ``should be able to use "try with" and "try finally" in boxed async builder (TryWith, TryFinally, Delay)`` () =
        // Given
        let test = ref false
        let a = fancyAsync {
            try 
                try 
                    let ex = 2 / 0
                    return ex
                with 
                    | :? DivideByZeroException as e -> 
                        printfn "%A" e
                        raise e
            finally 
                test := true
        }

        // When
        let result = a |> Async.Catch |> Async.RunSynchronously

        // Then
        match result with 
        | Choice1Of2 r -> failwith "Shouldn't happen"
        | Choice2Of2 e -> e |> should be ofExactType<DivideByZeroException>

        !test |> should be True