# Coroutine and Executor

## Iterator Block

In C# languauge, *iterator blocks* are a special kind of methods that has [yield](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/yield) keyword inside them so that method will return an instance of `IEnumerator` and a section of codes between `yield` statement will be executor once the `MoveNext` method of the `IEnumerator` instance is getting invoked.

Follwing is a simple example demonstrating how an iterator block getting used.
```csharp
IEnumerator Example(){
    Debug.Log("Step 1");
    yield return null;
    Debug.Log("Step 2");
    yield return null;
    Debug.Log("Step 3");
}

static void Main(){
    var e = Example();
    while(e.MoveNext()){
        Debug.Log("=== Sleep ===");
        System.Threading.Thread.Sleep(30);
    }
}
```

Output,
```
Step 1
=== Sleep ===
Step 2
=== Sleep ===
Step 3
```

For more detials about iterator blocks, please see [this article](http://csharpindepth.com/Articles/Chapter6/IteratorBlockImplementation.aspx).


## Coroutine

The <xref:Rayark.Mast.Coroutine></xref> class represets a coroutine by emulating stacks for iterator blocks. It allows an iterator blocks invoke executor another iterator blocks by `yield return` them.

The following example demonstrate how to the invocation of iterator blocks within an iterator blocks.

Example,
```csharp
using Rark.Mast;
using Mast.Coroutine;

IEnumerator A(){
    Debug.Log("1");

    # wait for one frame
    yield return null;

    Debug.Log("Start B")
    # run and wait for B()
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

## Executor

<xref:Rayark.Mast.Executor></xref> provides a way to execute multiple coroutine concurrenly. The basic usage pattern is creating it under a Unity `MonoBehaviour` and resume it every frame, then we can add an iterator block or a instance of <xref:Rayark.Mast.Coroutine></xref> to it at anytime we want to execute an asynchrous operation.

This is an example of using executor within a `MonoBehaviour`.

```csharp
using Rayark.Mast;
using Coroutine = Rayark.Mast.Coroutine;

class Player : MonoBehaviour{
    // create an executor when constructing
    Executor _executor = new Executor();

    // update executor every frame.
    void Update(){
        _executor.Resume(Time.deltaTime);
    }

    public void Hurt( float damage ){
        // add an iterator to an executor
        _executor.Add(_ShowDamageEffect());
    }

    // an iterator block
    IEnumerator _ShowDamageEffect(){
        // Do effects
    }
}
```

Instead of resuming <xref:Rayark.Mast.Executor></xref> on frame updating, it is possible to use it like a child coroutine within a iterator blocks.


The following example demonstrate how to use <xref:Rayark.Mast.Coroutine></xref>
and <xref:Rayark.Mast.Executor></xref> to control UI flow and also show a convinient way of using executor within an *iterator block*.
```csharp

IEnumerator Menu(){
    var executor = new Executor();

    // show the entering animation and updates the profile 
    // from server concurrently
    executor.Add( _ShowEnteringAnimation());
    executor.Add( _UpdateProfileFromServer());

    // Join() is an extension methods coverts IResumable to an iterator block
    yield return executor.Join();

    _leave = false;
    while( !_leave )
        yield return null;

    yield return _ShowLeavingAnimation();
}
```

## Control Entire Game Flow with Coroutine

One of the main problems of programming with Unity3D is organizing and maintaining codes of game flow. Because Unity3D dosen't expose main function, you have to encodes the game states within multiple `GameObject`, thus making tracking game state changes from source code very hard.

With iterator blocks and coroutines, we can easily express only low level UI states but also top level game states within a single coroutine stack. The following example desmonstrate the idea of using coroutine to control the entire game flow. Note that the real world example will be much complex than this. However, it's quite intuitive to extend the following code structure to apply the idea to real-world case.

```csharp
class Game : MonoBehaviour{

    static IEnumerator Main(){

        var gameData = new GameData();
        var executor = new Executor();
        executor.Add( UpdateGameData( gameData ) );
        executor.Add( ShowTrailer() );

        yield return executor.Join();

        while(true){
            yield return Menu(gameData);
            yield return Gameplay(gameData);

        }
    }

    static IEnumerator ShowTrailer(){
        SceneManager.LoadScene("trailer");
        // do something
    }

    static IEnumerator UpdateGameData( gameData ){
        // perform REST API Request
    }

    static IEnumerator Menu(GameData gameData){
        SceneManager.LoadScene("menu");
        // execute menu logic here
    }

    static IEnumerator Gameplay(GameData gameData){
        SceneManager.LoadScene("gameplay");
        // execute gameplay logic here
    }
}
```

It's worth to mention that there are two main benefits that brought by coroutine:
1. The coroutine structure cleary presents when the new Unity scene will be loaded. In this example, you can easily tell that `trailer` scene will be loaed in the begining, then `menu` scene, then `gameplay`, and then back to `menu` scene.
2. Singleton is no longer need to pass data between Unity scenes. You can see `GameData` instance is passed around to child iterator blocks like pass arguments to normal function. This is the most nature way of passing data in a computer program.

Despite coroutine is a very powerful tool of modeling game states declaratively, the lacks of the ability of returning value leads some inconvinience of passing result from one coroutine to its sibling. For example, you can't just return a value from the iterator block `Menu` to decide which stage will be loaded in the iterator block `Gameplay`. That's why *Mast* does further steps by introcing *Moand*. Please read the section of [Monad](monad.md) for more information.