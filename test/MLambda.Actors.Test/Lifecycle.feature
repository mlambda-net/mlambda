Feature: Actor lifecycle hooks
    Actors have lifecycle hooks that are called at specific points:
    PreStart when the actor begins, PostStop when it terminates,
    PreRestart before a restart, and PostRestart after a restart.

    Scenario: PreStart is called when actor is created
        Given a clean lifecycle log
        And a lifecycle actor
        Then the lifecycle log should contain "PreStart"

    Scenario: Actor responds to messages after PreStart
        Given a clean lifecycle log
        And a lifecycle actor
        When a ping message is sent to the lifecycle actor
        Then the ping response should be "pong"
