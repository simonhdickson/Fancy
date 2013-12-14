namespace Fancy.Tests
open Fancy
open Xunit
open Xunit.Extensions

module NancyTests =
    let ExampleUrls : obj[] seq =
        seq {
            yield [|""; ""|]    
            yield [|"/HelloWorld"; "/HelloWorld"|] 
            yield [|"/%s"; "/{%s}"|]
            yield [|"/%i"; "/{%s:int}"|]
        }

    [<Theory; PropertyData("ExampleUrls")>]
    let ``fancy correctly formats nancy url strings`` url expected =
        let result = formatNancyString url
        Assert.Equal<string>(expected, result)