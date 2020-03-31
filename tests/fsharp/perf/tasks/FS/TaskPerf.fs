(*
csc /optimize /target:library tests\fsharp\perf\tasks\csbenchmark.cs
artifacts\bin\fsc\Debug\net472\fsc.exe tests\fsharp\perf\tasks\TaskBuilder.fs tests\fsharp\perf\tasks\benchmark.fs --optimize -g -r:csbenchmark.dll
*)

namespace TaskPerf

//open FSharp.Control.Tasks
open System.Diagnostics
open System.Threading.Tasks
open System.IO
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
#if TASKBUILDER
open TaskBuilderTasks.ContextSensitive // TaskBuilder.fs extension members
#endif
//open FSharp.Control.ContextSensitiveTasks // the default
open Tests.SyncBuilder

[<AutoOpen>]
module Helpers =
    let bufferSize = 128
    let manyIterations = 10000

    let syncTask() = Task.FromResult 100
    let syncTask_FSharpAsync() = async.Return 100
    let asyncTask() = Task.Yield()

#if TASKBUILDER
    let taskBuilder = TaskBuilderTasks.ContextSensitive.task
#endif

    let tenBindSync_Task() =
        task {
            let! res1 = syncTask()
            let! res2 = syncTask()
            let! res3 = syncTask()
            let! res4 = syncTask()
            let! res5 = syncTask()
            let! res6 = syncTask()
            let! res7 = syncTask()
            let! res8 = syncTask()
            let! res9 = syncTask()
            let! res10 = syncTask()
            return res1 + res2 + res3 + res4 + res5 + res6 + res7 + res8 + res9 + res10
         }

#if TASKBUILDER
    let tenBindSync_TaskBuilder() =
        taskBuilder {
            let! res1 = syncTask()
            let! res2 = syncTask()
            let! res3 = syncTask()
            let! res4 = syncTask()
            let! res5 = syncTask()
            let! res6 = syncTask()
            let! res7 = syncTask()
            let! res8 = syncTask()
            let! res9 = syncTask()
            let! res10 = syncTask()
            return res1 + res2 + res3 + res4 + res5 + res6 + res7 + res8 + res9 + res10
         }
#endif

    let tenBindSync_FSharpAsync() =
        async {
            let! res1 = syncTask_FSharpAsync()
            let! res2 = syncTask_FSharpAsync()
            let! res3 = syncTask_FSharpAsync()
            let! res4 = syncTask_FSharpAsync()
            let! res5 = syncTask_FSharpAsync()
            let! res6 = syncTask_FSharpAsync()
            let! res7 = syncTask_FSharpAsync()
            let! res8 = syncTask_FSharpAsync()
            let! res9 = syncTask_FSharpAsync()
            let! res10 = syncTask_FSharpAsync()
            return res1 + res2 + res3 + res4 + res5 + res6 + res7 + res8 + res9 + res10
         }

    let tenBindAsync_Task() =
        task {
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
         }

#if TASKBUILDER
    let tenBindAsync_TaskBuilder() =
        taskBuilder {
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
         }
#endif

    let singleTask_Task() =
        task { return 1 }

#if TASKBUILDER
    let singleTask_TaskBuilder() =
        taskBuilder { return 1 }
#endif

    let singleTask_FSharpAsync() =
        async { return 1 }

[<MemoryDiagnoser>]
type ManyWriteFile() =
    [<Benchmark(Baseline=true)>]
    member __.ManyWriteFile_CSharpAsync () =
        TaskPerfCSharp.ManyWriteFileAsync().Wait();

    [<Benchmark>]
    member __.ManyWriteFile_Task () =
        let path = Path.GetTempFileName()
        task {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t.Wait()
        File.Delete(path)

#if TASKBUILDER
    [<Benchmark>]
    member __.ManyWriteFile_TaskBuilder () =
        let path = Path.GetTempFileName()
        TaskBuilderTasks.ContextSensitive.task {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t.Wait()
        File.Delete(path)
#endif

    [<Benchmark>]
    member __.ManyWriteFile_FSharpAsync () =
        let path = Path.GetTempFileName()
        async {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            for i = 1 to manyIterations do
                do! Async.AwaitTask(file.WriteAsync(junk, 0, junk.Length))
        }
        |> Async.RunSynchronously
        File.Delete(path)

[<MemoryDiagnoser>]
type NonAsyncBinds() =
    [<Benchmark(Baseline=true)>]
    member __.NonAsyncBinds_CSharpAsync() = 
         for i in 1 .. manyIterations*100 do 
             TaskPerfCSharp.TenBindsSync_CSharp().Wait() 

    [<Benchmark>]
    member __.NonAsyncBinds_Task() = 
        for i in 1 .. manyIterations*100 do 
             tenBindSync_Task().Wait() 

#if TASKBUILDER
    [<Benchmark>]
    member __.NonAsyncBinds_TaskBuilder() = 
        for i in 1 .. manyIterations*100 do 
             tenBindSync_TaskBuilder().Wait() 
#endif

    [<Benchmark>]
    member __.NonAsyncBinds_FSharpAsync() = 
        for i in 1 .. manyIterations*100 do 
             tenBindSync_FSharpAsync() |> Async.RunSynchronously |> ignore

[<MemoryDiagnoser>]
type AsyncBinds() =
    [<Benchmark(Baseline=true)>]
    member __.AsyncBinds_CSharpAsync() = 
         for i in 1 .. manyIterations do 
             TaskPerfCSharp.TenBindsAsync_CSharp().Wait() 

    [<Benchmark>]
    member __.AsyncBinds_Task() = 
         for i in 1 .. manyIterations do 
             tenBindAsync_Task().Wait() 

#if TASKBUILDER
    [<Benchmark>]
    member __.AsyncBinds_TaskBuilder() = 
         for i in 1 .. manyIterations do 
             tenBindAsync_TaskBuilder().Wait() 
#endif

    //[<Benchmark>]
    //member __.AsyncBinds_FSharpAsync() = 
    //     for i in 1 .. manyIterations do 
    //         tenBindAsync_FSharpAsync() |> Async.RunSynchronously 

[<MemoryDiagnoser>]
type SingleSyncTask() =
    [<Benchmark(Baseline=true)>]
    member __.SingleSyncTask_CSharpAsync() = 
         for i in 1 .. manyIterations*500 do 
             TaskPerfCSharp.SingleSyncTask_CSharp().Wait() 

    [<Benchmark>]
    member __.SingleSyncTask_Task() = 
         for i in 1 .. manyIterations*500 do 
             singleTask_Task().Wait() 

#if TASKBUILDER
    [<Benchmark>]
    member __.SingleSyncTask_TaskBuilder() = 
         for i in 1 .. manyIterations*500 do 
             singleTask_TaskBuilder().Wait() 
#endif

    [<Benchmark>]
    member __.SingleSyncTask_FSharpAsync() = 
         for i in 1 .. manyIterations*500 do 
             singleTask_FSharpAsync() |> Async.RunSynchronously |> ignore

[<MemoryDiagnoser>]
type SyncBuilderLoop() =
    [<Benchmark(Baseline=true)>]
    member __.SyncBuilderLoop_NormalCode() = 
        for i in 1 .. manyIterations do 
                    let mutable res = 0
                    for i in Seq.init 1000 id do
                       res <- i + res

#if ALL
    [<Benchmark>]
    member __.SyncBuilderLoop_WorkflowCode() = 
        for i in 1 .. manyIterations do 
             sync { let mutable res = 0
                    for i in Seq.init 1000 id do
                       res <- i + res }
#endif

module Main = 

    [<EntryPoint>]
    let main argv = 
        printfn "Testing that the tests run..."
        ManyWriteFile().ManyWriteFile_CSharpAsync()
        ManyWriteFile().ManyWriteFile_Task ()
#if TASKBUILDER
        ManyWriteFile().ManyWriteFile_TaskBuilder ()
#endif
        ManyWriteFile().ManyWriteFile_FSharpAsync ()
        NonAsyncBinds().NonAsyncBinds_CSharpAsync() 
        NonAsyncBinds().NonAsyncBinds_Task() 
#if TASKBUILDER
        NonAsyncBinds().NonAsyncBinds_TaskBuilder() 
#endif
        NonAsyncBinds().NonAsyncBinds_FSharpAsync() 
        AsyncBinds().AsyncBinds_CSharpAsync() 
        AsyncBinds().AsyncBinds_Task() 
#if TASKBUILDER
        AsyncBinds().AsyncBinds_TaskBuilder() 
#endif
        SingleSyncTask().SingleSyncTask_CSharpAsync()
        SingleSyncTask().SingleSyncTask_Task() 
#if TASKBUILDER
        SingleSyncTask().SingleSyncTask_TaskBuilder() 
#endif
        SingleSyncTask().SingleSyncTask_FSharpAsync()
        printfn "Running benchmarks..."

        //let manyWriteFileResult = BenchmarkRunner.Run<ManyWriteFile>()
        //let syncBindsResult = BenchmarkRunner.Run<NonAsyncBinds>()
        //let asyncBindsResult = BenchmarkRunner.Run<AsyncBinds>()
        //let singleTaskResult = BenchmarkRunner.Run<SingleSyncTask>()

        //printfn "%A" manyWriteFileResult
        //printfn "%A" syncBindsResult
        //printfn "%A" asyncBindsResult
        //printfn "%A" singleTaskResult
        let syncBuilderLoopResult = BenchmarkRunner.Run<SyncBuilderLoop>()
        0  