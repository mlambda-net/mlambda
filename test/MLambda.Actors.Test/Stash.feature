Feature: Message stashing with Become
    Actors can stash messages for later processing when they are
    not ready to handle them. Combined with Become, this enables
    initialization patterns where messages arrive before the actor
    is fully initialized.

    Scenario: Messages sent before initialization are stashed and processed after
        Given a stash actor
        When the messages "alpha", "beta", "gamma" are sent
        And the actor is initialized
        And the processed messages are queried
        Then the processed messages should be "alpha", "beta", "gamma"

    Scenario: Messages sent after initialization are processed immediately
        Given a stash actor
        When the actor is initialized
        And the messages "one", "two" are sent
        And the processed messages are queried
        Then the processed messages should be "one", "two"

    Scenario: Mixed stash and direct processing
        Given a stash actor
        When the messages "early" are sent
        And the actor is initialized
        And the messages "late" are sent
        And the processed messages are queried
        Then the processed messages should be "early", "late"
