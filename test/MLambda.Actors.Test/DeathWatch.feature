Feature: DeathWatch actor monitoring
    Actors can watch other actors to receive a Terminated message
    when the watched actor stops. This enables supervision patterns
    where actors need to react to the lifecycle of their peers.

    Scenario: Watcher receives Terminated when watched actor stops
        Given a watcher actor
        And a target actor to watch
        When the watcher watches the target
        And the target actor is stopped
        And we wait for the terminated notification
        Then the watcher should have received a terminated message

    Scenario: Unwatched actor does not send Terminated
        Given a watcher actor
        And a target actor to watch
        When the watcher watches the target
        And the watcher unwatches the target
        And the target actor is stopped
        And we wait for the terminated notification
        Then the watcher should not have received a terminated message
