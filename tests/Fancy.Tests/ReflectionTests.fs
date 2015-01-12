namespace Fancy.Tests
open System
open System.Threading
open Fancy
open Xunit
open Xunit.Extensions

module ReflectionTests =  
    let ``functions with paramaters`` : obj[] seq =
        seq {
            yield [|(fun (i:int) -> ()); [("i", typeof<int>)]|] 
            yield [|(fun (name:string) -> ()); [("name", typeof<string>)]|]
            yield [|(fun (s:string) (c:CancellationToken) -> ());  [("s", typeof<string>); ("c", typeof<CancellationToken>)] |]
            yield [|(fun () -> ()); [("unitVar0", typeof<unit>)]|]     
            yield [|(fun (``the int``:int) (``a string``:string)  -> failwith ""); [("the int", typeof<int>);"a string", typeof<string>]|]
        }

    [<Theory; PropertyData("functions with paramaters")>]
    let ``parses parameters of function correctly`` (func:obj) (expected:(string*Type) seq) =
        let result = getParameters (func |> Fancy.peel)
        for (expectedName,expectedtype),(name,``type``) in Seq.zip expected result do
            Assert.Equal<string>(expectedName, name)       
            Assert.Equal<Type>(expectedtype, ``type``)

  
    let ``conversions between types`` : obj[] seq =
        seq {
            yield [| typeof<int32>; int64 1; 1|]
        }

    [<Theory; PropertyData("conversions between types")>]
    let ``converting between equivalent types`` (toType:Type) (value:obj) (expected:obj) =
        let result = changeOrConvertType value toType
        Assert.Equal(expected, result)
