namespace Fancy.Tests
open Fancy
open Xunit
open Xunit.Extensions

module NancyTests =
    let ``example urls`` : obj[] seq =
        seq {
            yield [| "/"; [|typeof<obj>|]; "/" |]    
            yield [|"/HelloWorld"; [|typeof<obj>|]; "/HelloWorld"|] 
            yield [|"/%s"; [|typeof<string>|]; "/{%s}"|]
            yield [|"/%i"; [|typeof<int>|]; "/{%s:int}"|]   
            yield [|"/%d"; [|typeof<decimal>|]; "/{%s:decimal}"|]
            yield [|"/%b"; [|typeof<bool>|]; "/{%s:bool}"|]    
            yield [|"/%A"; [|typeof<Alpha>|]; "/{%s:alpha}"|]
        }

    [<Theory; PropertyData("example urls")>]
    let ``fancy correctly formats nancy url strings`` url types expected =
        let result = formatNancyString url types
        Assert.Equal<string>(expected, result)