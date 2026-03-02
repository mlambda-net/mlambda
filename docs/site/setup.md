# Setup

---

## Prerequisites

- .NET 6.0 SDK or later

## Dependencies

MLambda Actors uses the following key dependencies:

- **System.Reactive** (6.0.1) -- Reactive Extensions for observable-based messaging
- **Microsoft.Extensions.DependencyInjection** -- Service registration

## Project Structure

Add references to these MLambda projects:

```
MLambda.Actors.Abstraction   # Interfaces: IActor, IAddress, IContext, IStash, etc.
MLambda.Actors               # Core implementation: Process, MailBox, Scheduler, Supervision
MLambda.Actors.Core          # DI registration: AddActor() extension methods
```

## Service Registration

In your startup or composition root:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MLambda.Actors.Core;

var services = new ServiceCollection();

// Register the core actor system (guardian hierarchy, supervision, mailbox)
services.AddActor();

// Register each actor type
services.AddActor<MyActor>();
services.AddActor<AnotherActor>();

var provider = services.BuildServiceProvider();

// Resolve the user context to spawn actors
var userContext = provider.GetService<IUserContext>();
```

## Actor Route Annotation

Use the `[Route]` attribute to give actors a named path in the hierarchy:

```csharp
[Route("/myactor")]
public class MyActor : Actor
{
    protected override Behavior Receive(object data) => Actor.Ignore;
}
```

## Running Tests

The test suite uses SpecFlow with Autofac:

```bash
dotnet test
```
