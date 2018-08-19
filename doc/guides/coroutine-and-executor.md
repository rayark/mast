# Coroutine and Executor

## Iterator Block

In C# language, [iterator blocks](http://csharpindepth.com/Articles/Chapter6/IteratorBlockImplementation.aspx) are a particular kind of methods that have [yield](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/yield) keyword inside them, so the methods return an instance of `IEnumerator`, and codes between `yield` statements are  executed once the `MoveNext` method of the `IEnumerator` instance is getting invoked.

Following is a simple example demonstrating how an iterator block getting used.
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

The <xref:Rayark.Mast.Coroutine></xref> class represents a coroutine by emulating stacks for iterator blocks. It allows an iterator block to "invoke" another iterator block by `yield return` them.

The following example demonstrates how an iterator block invokes another iterator block within a coroutine.

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

<xref:Rayark.Mast.Executor></xref> provides a way to execute multiple coroutine concurrently. The basic usage pattern is creating it under a Unity `MonoBehaviour` and resume it every frame, and then we can add an iterator block or an instance of <xref:Rayark.Mast.Coroutine></xref> to it at any time we want to execute an asynchronous operation.

Here is an example of using <xref:Rayark.Mast.Executor></xref> within a `MonoBehaviour`.

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

Instead of resuming <xref:Rayark.Mast.Executor></xref> at frame updating, it is possible to use it as an invoked iterator block within a coroutine.

The following example demonstrate how to use <xref:Rayark.Mast.Coroutine></xref>
and <xref:Rayark.Mast.Executor></xref> to control UI flow and also show a convinient way of using executor within an iterator block.
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

One of the main problems of programming with Unity3D is organizing and maintaining codes of game flow. Because Unity3D doesn't expose the main function, you have to maintain the game states within one or multiple `GameObject`, thus making tracking game state changes from source code very hard.

With iterator blocks and coroutines, we can easily express only low-level UI states but also top-level game states within a single coroutine stack. The following example demonstrates the idea of using a coroutine to control the entire game flow. Note that the real world example is much complex than this. However, it's quite intuitive to extend the following code structure to apply the idea to real-world cases.

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

It's worth to mention that two main benefits brought by coroutine:
1. The coroutine structure presents when Unity scenes are loaded according to the game flow. In this example, you can quickly tell that `trailer` scene is loaded in the beginning, then `menu` scene, then `gameplay`, and then back to `menu` scene.
2. Singleton is no longer need to pass data between Unity scenes. You can see `GameData` instance is passed around to child iterator blocks like pass arguments to a regular function. That is the most natural way of passing data in a computer program and eliminates the need for introducing global [side effects](https://en.wikipedia.org/w/index.php?title=Side_effect_(computer_science)&oldid=855461052).

Although coroutine is a potent tool of modeling game states declaratively, the lacks of the ability to return value lead some inconvenience of passing result from one coroutine to its sibling. For example, you can't just return a value from the iterator block `Menu` to decide which stage is loaded in the iterator block `Gameplay`. That's why *Mast* further introduce *Monad*. Please read the section of [Monad](monad.md) for more information.