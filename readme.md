# Fancy

The idea behind Fancy to provide a statically typed wrapper around Nancy, so that you can leverage both all the features of Nancy and use it in an F# way.

```fsharp Hello World from Fancy
let pipeline =
    fancy {
        get "/" (fun () -> "Hello World!")
    }
```

Check this [blog post](http://simonhdickson.github.io/) for more information