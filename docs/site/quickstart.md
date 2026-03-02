# Quickstart

---

## Defining an Actor

Inherit from `Actor` and override the `Receive` method. Use C# pattern matching to dispatch messages to behaviors:

```csharp
[Route("/hello")]
public class HelloWorld : Actor
{
    protected override Behavior Receive(object data) =>
        data switch
        {
            string message => Actor.Behavior(this.Show, message),
            _ => Actor.Ignore
        };

    private IObservable<Unit> Show(string message)
    {
        Console.WriteLine(message);
        return Actor.Done;
    }
}
```

## Registering Actors

Register the actor system and your actors with dependency injection:

```csharp
var services = new ServiceCollection();
services.AddActor();                  // Core actor system
services.AddActor<HelloWorld>();      // Register your actor
services.AddActor<CounterActor>();    // Register more actors
```

## Spawning and Sending Messages

Inject `IUserContext` and use it to spawn actors and send messages:

```csharp
// Spawn an actor - returns an IAddress (the actor's handle)
var address = await userContext.Spawn<HelloWorld>();

// Fire-and-forget (tell)
address.Send("Hello!").Subscribe();

// Request-response (ask) - waits for actor to respond
int count = await address.Send<Increment, int>(new Increment());
```

## Stateful Actor Example

Actors maintain internal state across messages:

```csharp
[Route("/counter")]
public class CounterActor : Actor
{
    private int count;

    protected override Behavior Receive(object data) =>
        data switch
        {
            Increment _ => Actor.Behavior<int>(this.HandleIncrement),
            Decrement _ => Actor.Behavior<int>(this.HandleDecrement),
            GetCount _  => Actor.Behavior<int>(() => Observable.Return(this.count)),
            _           => Actor.Ignore,
        };

    private IObservable<int> HandleIncrement()
    {
        this.count++;
        return Observable.Return(this.count);
    }

    private IObservable<int> HandleDecrement()
    {
        this.count--;
        return Observable.Return(this.count);
    }
}
```

## Context-Aware Handlers

Handler methods can accept `IContext` as their first parameter to access actor features like spawning children or watching other actors:

```csharp
private IObservable<string> HandleMessage(IContext context, string message)
{
    // Spawn a child actor
    var child = context.Spawn<ChildActor>();

    // Watch another actor for termination
    context.Watch(someAddress);

    return Observable.Return("handled");
}
```

## Behavior Factory Methods

The `Actor.Behavior(...)` factory has overloads for different handler signatures:

```csharp
// No parameters, no context
Actor.Behavior<string>(() => Observable.Return("hello"))

// With context
Actor.Behavior<string>(ctx => Observable.Return("hello"))

// With parameter (no context)
Actor.Behavior(this.Process, value)

// With context and parameter
Actor.Behavior(this.Handle, context, value)

// Multiple parameters
Actor.Behavior(this.Sum, a, b)
```

## Next Steps

- [Actor Model Theory](actormodel.md) -- Understand the fundamentals
- [Architecture](architecture.md) -- How MLambda fits into your design
