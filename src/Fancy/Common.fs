[<AutoOpen>]
module Operators 
    open System
    open System.Threading.Tasks
    let inline (++) (a: 'T) (b: 'T) = 
        System.Delegate.Combine(a, b) :?> 'T
       
    /// https://gist.github.com/theburningmonk/3921623
    let inline awaitPlainTask (task: Task) = 
        // rethrow exception from preceding task if it fauled
        let continuation (t : Task) : unit =
            match t.IsFaulted with
            | true -> raise t.Exception
            | arg -> ()
        task.ContinueWith continuation |> Async.AwaitTask
 
    let inline startAsPlainTask (work : Async<unit>) = Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)