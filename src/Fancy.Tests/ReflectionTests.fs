namespace Fancy.Tests
open System
open Fancy
open Xunit
open Xunit.Extensions

module ReflectionTests =  
    let ``functions with paramaters`` : obj[] seq =
        seq {
            yield [|(fun (i:int) -> ()); [("i", typeof<int>)]|] 
            yield [|(fun (name:string) -> ()); [("name", typeof<string>)]|]
            yield [|(fun () -> ()); [("unitVar0", typeof<unit>)]|]     
            yield [|(fun (``the int``:int) (``a string``:string)  -> failwith ""); [("the int", typeof<int>);"a string", typeof<string>]|]
        }

    [<Theory; PropertyData("functions with paramaters")>]
    let ``parses parameters of function correctly`` (func:obj) (expected:(string*Type) seq) =
        let result = getParametersFromObj func
        for (expectedName,expectedtype),(name,``type``) in Seq.zip expected result do
            Assert.Equal<string>(expectedName, name)       
            Assert.Equal<Type>(expectedtype, ``type``)
