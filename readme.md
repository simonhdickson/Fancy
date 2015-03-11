# Fancy

[![Mono build status](https://travis-ci.org/simonhdickson/Fancy.svg)](https://travis-ci.org/simonhdickson/Fancy)
[![Windows Build status](https://ci.appveyor.com/api/projects/status/j2906bvf4rp6cxgj?svg=true)](https://ci.appveyor.com/project/simonhdickson/fancy)

The idea behind Fancy to provide a statically typed wrapper around Nancy, so that you can leverage both all the features of Nancy and use it in an F# way.

Install
>Install-Package Fanciful -Pre

```fsharp
type ExampleModule() as this = 
    inherit NancyModule()
    do fancy this {
        get "/" (fun () -> fancyAsync { return "Hello World!" } )
    }
```

Check this [blog post](http://simonhdickson.github.io/) or the [documentation](http://simonhdickson.github.io/Fancy/) for more information
