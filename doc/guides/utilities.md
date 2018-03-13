# Utilities

## Sleep

[Coroutine.Sleep(float)](xref:Rayark.Mast.Coroutine#Rayark_Mast_Coroutine_Sleep_System_Single_) is a helper function let you sleep a period time in a coroutine.

```csharp
IEnumerator DoSomething(){
    // Sleep for 3.0 seconds
    yield return Coroutine.Sleep(3.0f);
}
```

The period of time during which [Coroutine.Sleep(float)](xref:Rayark.Mast.Coroutine#Rayark_Mast_Coroutine_Sleep_System_Single_) actually sleeps also is affected by the elapsed time passed to [Coroutine.Resume(float)](xref:Rayark.Mast.Coroutine#Rayark_Mast_Coroutine_Resume_System_Single_).

For example,
```csharp
Coroutine co = new Coroutine();
void Update(){
    co.Resume( Time.deltaTime * 0.1f);
}
```
the sleep method executed with in the above coroutine will takes ten times time.

## Resources Management with `Defer`

The `using{}` clause can be used within iterator blocks to help manage resources with respect to the life time of the iterator blocks.

Consider the following iterator block.

```csharp
IEnumerator IAP(){

    using( var transaction = iapMgr.CreateTransaction()){
        // Do some operation
        yield return Coroutine.Sleep();

        if( isError ){
            yield break;
        }
    }

    yield return ShowTransactionSucceedAnimation();
}
```

The variable `transaction` holds a dispoable object. The `using{}` clause makes sure the related disposable object will be disposed when the iterator block finished. Even if `MoveNext` method of the `IEnumerator` instance of the iterator block is stopped being invoked for some unknown reason, we can easily dispose allocated resource via the following code snippet.

```csharp
var etr = IAP();

//call MoveNext() few times

// The transaction object will be disposed if it has been created.
(etr as IDisposable).Dispose();
```

Actually, if you execute iterator blocks with <xref:Rayark.Mast.Coroutine></xref>, all the generated iterator block instances in the stack will be disposed when the instance of <xref:Rayark.Mast.Coroutine></xref> is disposed.

To further utilize `using{}` clause to manage resources, we can use <xref:Rayark.Mast.Defer></xref>. The following example demonstrate how to use <xref:Rayark.Mast.Defer></xref> with `using{}` clause to manage resources, even states (`_running`).

```csharp

IEnumerator IAP(){
    using(var defer = new Defer()){

        _running = true;
        // _running will set to false after leaving using clause
        defer.Add( () => { _running = false;});

        var resource = AllocateResrouce();
        // resource will be deallocated after leaving using clause
        defer.Add( ()=> DeallocateResource(resource));
        // do something
    }
}
```





