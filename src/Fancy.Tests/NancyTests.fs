namespace Fancy.Tests
open Fancy
open Xunit
open Xunit.Extensions

type Alpha = Alpha of string 

module NancyTests =
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