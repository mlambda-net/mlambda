# Actor Architecture

---

## Guardian Hierarchy

MLambda organizes actors into a tree structure with built-in system actors:

```
Root (/)
  ├── System (/system)
  ├── User (/user)
  │     ├── /counter
  │     ├── /stash
  │     └── /watcher
  └── Temp (/temp)
```

- **Root** -- Top-level guardian; final escalation point
- **System** -- Internal framework actors
- **User** -- All user-created actors live here
- **Temp** -- Temporary actors for short-lived work

## Message Processing Pipeline

When a message is sent to an actor:

1. The `IAddress` wraps the message as `Synchronous` (request-response) or `Asynchronous` (fire-and-forget)
2. The message is enqueued in the actor's `MailBox`
3. The `Scheduler` dequeues messages one at a time and calls `Process.Receive`
4. `Process.Receive` sets the current message on the stash, then delegates to the supervisor
5. The supervisor calls `IActor.Receive(payload)` to get a `Behavior` delegate
6. The behavior is executed and its result is sent back via `message.Response(result)`

## Supervision

Each actor has a supervision strategy that determines how child failures are handled.

### OneForOne Strategy

Only the failing child is affected:

```csharp
public override ISupervisor Supervisor => Strategy.OneForOne(
    decider => decider
        .When<InvalidOperationException>(Directive.Resume)
        .When<InvalidCastException>(Directive.Restart)
        .Default(Directive.Escalate));
```

### AllForOne Strategy

All sibling children are affected when one fails:

```csharp
public override ISupervisor Supervisor => Strategy.AllForOne(
    decider => decider
        .When<InvalidOperationException>(Directive.Resume)
        .When<InvalidCastException>(Directive.Restart)
        .Default(Directive.Escalate),
    bucket);
```

### Directives

| Directive   | Effect                                     |
|-------------|-------------------------------------------|
| `Resume`    | Ignore the error, continue processing     |
| `Restart`   | Stop and recreate the actor               |
| `Stop`      | Permanently stop the actor                |
| `Escalate`  | Pass the error to the parent supervisor   |

## Become / Unbecome

Actors can dynamically switch their message handling behavior:

- `Become(handler)` -- Replaces the current receive handler with a new one
- `Unbecome()` -- Reverts to the default `Receive` method

The new handler is a `Func<object, Behavior>` that takes the message payload and returns a behavior.

## Message Stashing

Actors can temporarily buffer messages they cannot yet handle:

- `this.Stash?.Stash()` -- Saves the current message to a stack
- `this.Unstash()` -- Replays the most recent stashed message
- `this.UnstashAll()` -- Replays all stashed messages in original order

Stashed messages are re-enqueued to the mailbox and processed through the normal pipeline.

## DeathWatch

Actors can monitor other actors for termination:

- `context.Watch(address)` -- Register to receive `Terminated` when the target stops
- `context.Unwatch(address)` -- Stop monitoring
- When a watched actor stops, all watchers receive a `Terminated` message

## Lifecycle Hooks

Override these methods to hook into actor lifecycle events:

| Hook           | When Called                          |
|---------------|--------------------------------------|
| `PreStart()`   | Before the actor processes its first message |
| `PostStop()`   | After the actor has been stopped     |
| `PreRestart()` | Before the actor is restarted        |
| `PostRestart()`| After the actor has been restarted   |
