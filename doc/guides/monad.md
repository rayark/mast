# Monad

Monad is an abstract data type newly added to the Mast library, and is used to encapsulate asynchronized operations. This name comes from the common design pattern in functional programming.

Most people view functional programming as **treating functions as data**, where functions can be stored as variables or passed into other functions. However, functional languages **treat everything as data**, including computation and side effects. You can store many side effects in monad variables, and concatenate them into a new monad to produce a sequencial side effect, just like string concatenation.

## Why Monad?

Yes, we already have a coroutine library, and we can implement asynchronized operations with coroutines. However, there are some drawbacks when using coroutines:

1. In C#, coroutines cannot have return values. You are forced to pass a "return object wrapper" as a parameter so that you can obtain the return value of a coroutine.
    ```csharp
    IEnumerator SomeCoroutine(Return<int> ret)
    {
        // do something
        // ...
        
        ret.Accept(result);
    }
    ```
2. Error handling in coroutine is extremely cumbersome:
    ```csharp
    IEnumerator SomeCoroutine()
    {
        var r1 = new Return<int>();
        yield return Step1(r1);
        if(!r1.Accepted){
            // some error handling
            yield break;
        }
        
        var r2 = new Return<string>();
        yield return Step2(r1.Result, r2);
        if(!r2.Accepted){
            // some error handling
            yield break;
        }
        
        // ...
    }
    ```
3. In C#, lambda functions cannot `yield`. If you wish to combine two coroutines, you are forced to declare a new function which `yield return` these two coroutines.
   ``` csharp
   void DoesNotWork(Executor exec)
   {
       // lambdas cannot yield return
       exec.Add(() => {
           yield return A();
           yield return B();
       });
   }
   ```

## Basic Usage of IMonad

In our Mast library, we provide the `IMonad` interface:

``` csharp
public interface IMonad<T>
{
    T Result { get; }
    Exception Error { get; }
    IEnumerator Do();
}
```

`IMonad<T>` is a abstract interface which encapsulate some computation or side effect which gives `T` as its result. The `Do()` function will returns a enumerator block so that this computation can be executed as a coroutine.In case of any failure, the `Error` field will be set to the exception object.

The typical usage of `IMonad<T>` inside a coroutine is like the following sample:

``` csharp
var req = SomeAsyncOperation();
yield return req.Do();
if(req.Error != null) {
    // error handling
} else {
    var result = req.Result;
}
```

## Chaining the Process

You can chain two monads with the `Then()` function and a lambda which returns another monad.

``` csharp
var req1 = FirstOperation(); 
var req2 = req1.Then(
    result => {
        return SecondOperation(result);
    }
);

yield return req2.Do();
```

The lambda function passed into `Then()` will be called only if `req1` is completed successfully, and the result will be passed as the argument. The return value, which is also a monad, is then executed. That is to say, `Then()` combines two monads and produces a new monad.

This is as same as:

``` csharp
var req1 = FirstOperation();
yield return req1.Do();
if(req1.Error == null){
    var req2 = SecondOperation(req1.Result);
    yield return req2.Do();
}
```

You can combine as many monads as you want:

``` csharp
var req = FirstOperation()
            .Then(r1 => SecondOperation(r1))
            .Then(r2 => ThirdOperation(r2))
            .Then(r3 => FourthOperation(r4));
```

If there is any exception raised, it will be set to `req.Error`. Otherwise, `req.Result` will be the result of `FourthOperation(r4)`.

## Chaining Monads with LINQ

You can combine monads in LINQ syntax:

```csharp
// as same as the previous example
var req = from r1 in FirstOperation()
          from r2 in SecondOperation(r1)
          from r3 in ThirdOperation(r2)
          from r4 in FourthOperation(r3)
          select r4;

yield return req.Do();
```

## Exception Handling

When chaining multiple monads, we may need to add some extra operation when error occurs. We can use the `Catch()` function handle errors. For example, we want to provide a default one when loading an asset:

``` csharp
var req = LoadAsset(assetId)
            .Catch(e => LoadAsset(defaultAssetId));

req.Do();
```

This is as same as:

``` csharp
var req1 = LoadAsset(assetId);
req1.Do();
if(req1.Error != null){
    var req2 = LoadAsset(defaultAssetId);
    req2.Do();
}
```

You can combine both `Then()` and `Catch()` to produce a pipeline:

``` csharp
// Download an asset. If failed, load the cached one.
// And then decrypt it
var req = DownloadAsset(assetId)
            .Catch(e => LoadCachedAsset(assetId))
            .Then(data => DecryptAsset(data, key));
```

## Performing Multiple Task Concurrently

You can also combine multiple monads and execute them concurrently. This is done by `ConcurrentMonad`:

``` csharp
IMonad<string> req1 = SendHttpRequest();
IMonad<int> req2 = DoOperation();

var cm = new ConcurrentMonad<string, int>(req1, req2);
yield return cm.Do();

if(cm.Error == null){
    string result1 = cm.Result.Item1;
    int result2 = cm.Result.Item2;
}
```

Or, you can use the shortcut function

``` csharp
var cm = Moand.WhenAll(SendHttpRequest(), DoOperation());
yield return cm.Do();

if(cm.Error == null){
    string result1 = cm.Result.Item1;
    int result2 = cm.Result.Item2;
}
```

