# Fancy

The idea behind Fancy to provide a statically typed wrapper around Nancy, so that you can leverage both all the features of Nancy and use it in an F# way.

Install
>Install-Package Fanciful -Pre

```fsharp
let pipeline =
    fancy {
        get "/" (fun () -> asPlainText "Hello World!")
    }
```

Check this [blog post](http://simonhdickson.github.io/) for more information