

# [MLambda Actors](https://actors.mlambda.net)

[![Build Status](https://travis-ci.com/RoyGI/MLambda.svg?branch=master)](https://travis-ci.com/RoyGI/MLambda)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=RoyGI_MLambda&metric=alert_status)](https://sonarcloud.io/dashboard?id=RoyGI_MLambda)
[![Maintainability](https://api.codeclimate.com/v1/badges/737d19a0ea28334bf24f/maintainability)](https://codeclimate.com/github/RoyGI/MLambda/maintainability)
[![CodeFactor](https://www.codefactor.io/repository/github/roygi/mlambda/badge)](https://www.codefactor.io/repository/github/roygi/mlambda)
[![codecov](https://codecov.io/gh/RoyGI/MLambda/branch/master/graph/badge.svg)](https://codecov.io/gh/RoyGI/MLambda)
[![GuardRails badge](https://badges.guardrails.io/RoyGI/MLambda.svg?token=92ebac5e2201973fdb72dab039abe5da63bc8427d4dda67f0b33e71a15c6f06f&provider=github)](https://dashboard.guardrails.io/default/gh/RoyGI/MLambda)

MLambda is a Reactive Actor Model framework for .NET. It provides a lightweight, local-only actor system with guardian hierarchy supervision, reactive message passing via System.Reactive, and a clean API built on C# pattern matching.

## Features

- **Guardian Hierarchy** -- Root/System/User/Temp actor tree with parent-child supervision
- **Supervision Strategies** -- OneForOne (only failed child affected) and AllForOne (all siblings affected)
- **Become/Unbecome** -- Dynamic behavior switching at runtime
- **Message Stashing** -- Temporarily buffer messages and replay them later
- **DeathWatch** -- Watch/Unwatch/Terminated pattern for monitoring actor lifecycle
- **Lifecycle Hooks** -- PreStart, PostStop, PreRestart, PostRestart callbacks
- **Reactive Messaging** -- All responses are `IObservable<T>` via System.Reactive
- **DI Integration** -- Register actors with `Microsoft.Extensions.DependencyInjection`

## Quick Start

### 1. Install

Add the MLambda.Actors packages to your project.

### 2. Register Services

```csharp
var services = new ServiceCollection();
services.AddActor();                    // Core actor system
services.AddActor<MyActor>();           // Register your actors
```

### 3. Define an Actor

Inherit from `Actor` and override `Receive` using C# pattern matching:

```csharp
[Route("/counter")]
public class CounterActor : Actor
{
    private int count;

    protected override Behavior Receive(object data) =>
        data switch
        {
            Increment _ => Actor.Behavior<int>(this.HandleIncrement),
            GetCount _  => Actor.Behavior<int>(() => Observable.Return(this.count)),
            _           => Actor.Ignore,
        };

    private IObservable<int> HandleIncrement()
    {
        this.count++;
        return Observable.Return(this.count);
    }
}
```

### 4. Spawn and Send Messages

```csharp
// Inject IUserContext via DI
var address = await userContext.Spawn<CounterActor>();

// Synchronous send (request-response)
int count = await address.Send<Increment, int>(new Increment());

// Fire-and-forget
address.Send(new Increment()).Subscribe();
```

## Concepts

### Behavior and Pattern Matching

The `Receive` method returns a `Behavior` delegate (`IObservable<object> Behavior(IContext context)`). Use `Actor.Behavior(...)` factory methods to create behaviors from your handler methods:

```csharp
protected override Behavior Receive(object data) =>
    data switch
    {
        string message => Actor.Behavior(this.Print, message),   // with parameter
        int value      => Actor.Behavior(this.Process, value),   // typed parameter
        _              => Actor.Ignore,                          // unhandled
    };
```

Handler methods can optionally accept `IContext` as the first parameter to access the actor context (Self, Spawn, Watch, etc.).

### Become / Unbecome

Actors can switch their message handling behavior at runtime:

```csharp
public class BecomeActor : Actor
{
    protected override Behavior Receive(object data) =>
        data switch
        {
            SetMood m when m.Mood == "happy" => Actor.Behavior(this.SwitchToHappy),
            AskMood _ => Actor.Behavior<string>(() => Observable.Return("normal")),
            _ => Actor.Ignore,
        };

    private IObservable<string> SwitchToHappy(IContext ctx)
    {
        this.Become(this.HappyBehavior);  // Switch behavior
        return Observable.Return("now happy");
    }

    private Behavior HappyBehavior(object data) =>
        data switch
        {
            AskMood _ => Actor.Behavior<string>(() => Observable.Return("happy")),
            SetMood m when m.Mood == "normal" => Actor.Behavior(this.RevertToNormal),
            _ => Actor.Ignore,
        };

    private IObservable<string> RevertToNormal(IContext ctx)
    {
        this.Unbecome();  // Revert to default Receive
        return Observable.Return("back to normal");
    }
}
```

### Message Stashing

Actors can stash messages they cannot yet handle and replay them later. This is commonly combined with `Become` for initialization patterns:

```csharp
public class StashActor : Actor
{
    private readonly List<string> processed = new();

    protected override Behavior Receive(object data) =>
        data switch
        {
            Initialize _ => Actor.Behavior(this.HandleInit),
            string _     => Actor.Behavior(this.StashIt),    // Not ready yet
            _ => Actor.Ignore,
        };

    private IObservable<string> HandleInit(IContext ctx)
    {
        this.Become(this.ReadyBehavior);
        this.UnstashAll();  // Replay all stashed messages
        return Observable.Return("initialized");
    }

    private IObservable<string> StashIt(IContext ctx)
    {
        this.Stash?.Stash();  // Buffer current message
        return Observable.Return("stashed");
    }

    private Behavior ReadyBehavior(object data) =>
        data switch
        {
            string msg => Actor.Behavior(this.Process, msg),
            _ => Actor.Ignore,
        };

    private IObservable<string> Process(string msg)
    {
        this.processed.Add(msg);
        return Observable.Return($"processed: {msg}");
    }
}
```

### Supervision Strategies

Define how parent actors handle child failures:

**OneForOne** -- Only the failing child is affected:

```csharp
public class MyActor : Actor
{
    public override ISupervisor Supervisor => Strategy.OneForOne(
        decider => decider
            .When<InvalidOperationException>(Directive.Resume)
            .When<InvalidCastException>(Directive.Restart)
            .Default(Directive.Escalate));
}
```

**AllForOne** -- All sibling children are affected when one fails:

```csharp
public class ParentActor : Actor
{
    private readonly IBucket bucket;

    public ParentActor(IBucket bucket) => this.bucket = bucket;

    public override ISupervisor Supervisor => Strategy.AllForOne(
        decider => decider
            .When<InvalidOperationException>(Directive.Resume)
            .When<InvalidCastException>(Directive.Restart)
            .Default(Directive.Escalate),
        this.bucket);
}
```

Directives: `Resume` (ignore error), `Restart` (recreate actor), `Stop` (terminate), `Escalate` (pass to parent).

### DeathWatch

Monitor another actor's lifecycle:

```csharp
protected override Behavior Receive(object data) =>
    data switch
    {
        WatchTarget t  => Actor.Behavior(this.StartWatching, t),
        Terminated t   => Actor.Behavior(this.OnTerminated, t),
        _ => Actor.Ignore,
    };

private IObservable<string> StartWatching(IContext ctx, WatchTarget target)
{
    ctx.Watch(target.Address);  // Register for Terminated notifications
    return Observable.Return("watching");
}

private IObservable<string> OnTerminated(Terminated t)
{
    // The watched actor has stopped
    return Observable.Return("target terminated");
}
```

### Lifecycle Hooks

Override lifecycle methods to execute code at specific points:

```csharp
public class MyActor : Actor
{
    public override void PreStart()    { /* Before first message */ }
    public override void PostStop()    { /* After actor stops */ }
    public override void PreRestart(Exception reason)  { /* Before restart */ }
    public override void PostRestart(Exception reason) { /* After restart */ }

    protected override Behavior Receive(object data) => Actor.Ignore;
}
```

## Project Structure

```
src/
  MLambda.Actors.Abstraction/   # Interfaces and base classes (IActor, IAddress, IContext, etc.)
  MLambda.Actors/               # Core implementation (Process, MailBox, Scheduler, Supervision)
  MLambda.Actors.Core/          # DI registration (AddActor extensions)
test/
  MLambda.Actors.Test/          # SpecFlow BDD tests
    Actors/                     # Sample actor implementations
    Steps/                      # Step definitions
    *.feature                   # Gherkin scenarios
```

## Testing

Tests use SpecFlow (BDD) with Gherkin feature files:

```
dotnet test
```

Example scenario:

```gherkin
Feature: Message stashing with Become

  Scenario: Messages sent before initialization are stashed and processed after
    Given a stash actor
    When the messages "alpha", "beta", "gamma" are sent
    And the actor is initialized
    And the processed messages are queried
    Then the processed messages should be "alpha", "beta", "gamma"
```

## License

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE) for details.

---

<p align="center">
    <a href="https://www.buymeacoffee.com/yordivad" target="_blank">
        <img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" width="217px" height="51px">
    </a>
</p>
