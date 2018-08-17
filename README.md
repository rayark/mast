# Mast - A Coroutine Extension for Unity

[![pipeline status](https://gitlab.com/rayark/mast/badges/master/pipeline.svg)](https://gitlab.com/rayark/mast/commits/master)

- [Official Repository (Gitlab)](https://gitlab.com/rayark/mast)
- [Documentation](https://rayark.gitlab.io/mast/)

## What is Mast?
Mast is a coroutine library extending **iterator block** feature of C# language. It provides a more convenient and functional way to adopt the concept of **Coroutine**. It is widely used and becomes a fundamental keystone of developing game projects with Unity3D in [Rayark Inc](https://www.rayark.com).

## Why Mast?

Since game programming often involves managing states of the program across a period (multiple frames). It is essential to have an excellent way to manage the side effect of different game logic. That's why **coroutine** comes into sight for Unity3D. However, the coroutine implementation of Unity has some limitations
- Unity coroutine can't return value.
- Unity coroutine lacks tools to help write codes for multiple concurrent tasks.
- Unity coroutine is a black box, and you can't control how it runs.

To overcome the above issues, we develop Mast. It mainly consists of three parts,
- `Executor` and `Coroutine` - provide basic infrastructure to execute coroutines with stacks
- `IMonad` - is a [promise](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise)-like wrapper for coroutines, which enables functional coding paradigm of coroutines
- Utilities such as `Defer` and `Joinable` that helps write more simple code for coroutines



For example, if you want to download and parse a URL asynchronously, the code you write with Mast is like this,

```csharp
IEnumerator Download( string URL, IReturn<string> ret){
    ...
}

T Parse<T>( string input ){
    ...
}


IEnumerator Process(){

    var m = new BlockMonad( r => Download(URL,r))
        .Then( body => new ThreadMoand<PlayerInfo>( Parse<PlayerInfo> ));

    yield return m.Do();

    if( m.Error != null ){
        //Error handling here
    }
    else{

        PlayerInfo info = m.Result;
        Debug.Log(info.Id);
    }
}

void Start(){
    _executor.Add(Process());
}

void Update(){
    if( _executor.Finished)
}
``` 

## Quick Start

### Basic Coroutine

Example,
```csharp
using Rayark.Mast;
using Mast.Coroutine;

IEnumerator A(){
    Debug.Log("1");

    // wait for one frame
    yield return null;

    Debug.Log("Start B")
    // run and wait for B()
    yield return B();

    Debug.Log("4");
    yield return null;
}

IEnumerator B(){
    Debug.Log("2");
    yield return null;
    Debug.Log("3");
    yield return null;
    Debug.Log("B Return");
}

Coroutine coroutine = new Coroutine( A() );

void Update(){
    if( !coroutine.Finished ){
        Debug.Log("---");
        coroutine.Resume(Time.deltaTime);
    }
}
```

Output,
```
---
1
---
Start B
2
---
3
---
B Return
4
```

### Executor

Executor provides a concurrent way of running multiple coroutines.

Example,
```csharp
Executor executor = new Executor()

IEnumerator A(){

    Debug.Log("1");
    yield return null;
    Debug.Log("2");
}

IEnumerator B(){

    Debug.Log("3");
    yield return null;
    Debug.Log("4");
}

void Start(){
    executor.Add(A());
    executor.Add(B());
}

void Update(){
    if( !executor.Finished ){
        executor.Resume(Time.deltaTime);
    }
}
```

### Monad

Monad provides a way for a coroutine to return value or error. Multiple monads can chain together to a single monad for better error handling and concise.

```csharp

IEnumerator SleepAndSqrt( int input, IReturn<int> ret){

    // Sleep for 1 second
    yield return Coroutine.Sleep(1.0f);

    if( input <= 0 ){
        ret.Fail( new System.Exception("input must be greater than 0"));
        yield break;
    }
    ret.Accept(Math.Sqrt(input));
}


IEnumerator  A(){

    // chain two coroutines that perform sleeping 1 sec and sqrt
    var m = new BlockMoand<int>( r => SleepAndSqrt(16, r))
        .Then( v => new BlockMonad<int>( r => SleepAndSqrt(v, r)));
    
    // run monad
    yield return m.Do();

    // no error
    Assert.That( m.Error == null);
    Assert.That( m.Result == 2);

    // chain two coroutines that perform sleeping 1 sec and sqrt
    var m2 = new BlockMoand<int>( r => SleepAndSqrt(-16, r))
        .Then( v => new BlockMonad<int>( r => SleepAndSqrt(v, r)));


    // run monad
    yield return m2.Do();

    // oops, monad has error
    Assert.That( m2.Error != null);
}

void Start(){
    //Add A() to executor
    executor.Add(A());
}
```
## License
Mast is licensed under the MIT license.