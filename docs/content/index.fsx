(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
(**
Fancy
==================================

Fancy is designed to act as a type-safe, F# orientated wrapper around the Nancy. 

Documentation

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      oxen can be <a href="https://nuget.org/packages/fanciful">installed from NuGet</a>:
      <pre>PM> Install-Package oxen</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Example
-------

This example demonstrates using creating a route that returns hello world.

*)
#r "Fancy.dll"
#r "Nancy.dll"
#r "Nancy.Hosting.Self"

open Fancy
open Nancy
open Nancy.Hosting.Self
open System

type ExampleModule () as this=
    inherit NancyModule ()

    do fancy this {
        get "/" (fun _ -> fancyAsync {
            return "Hell world"
        })

        get "/%s" (fun string -> fancyAsync {
            return sprintf "This string: \"%s\" was entered" string
        })
    }

let nancyHost = new NancyHost(Uri "http://localhost:8888/nancy/") 
nancyHost.Start()  
Console.ReadLine() |> ignore

(**
Some more info

Samples & documentation
-----------------------

 * [Tutorial](tutorial.html) contains a further explanation of oxen and contains examples of interop with bull.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation.
The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/simonhdickson/Fancy/tree/master/docs/content
  [gh]: https://github.com/simonhdickson/Fancy
  [issues]: https://github.com/simonhdickson/Fancy/issues
  [readme]: https://github.com/simonhdickson/Fancy/README.md
  [license]: https://github.com/simonhdickson/Fancy/blob/master/LICENSE.txt
*)
