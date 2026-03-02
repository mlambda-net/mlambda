Feature: Become and Unbecome behavior switching
    Actors can dynamically change their message handling behavior
    at runtime using Become and Unbecome. This allows actors to
    implement state machines and react differently based on their
    current state.

    Scenario: Actor starts with default behavior
        Given a become actor
        When the mood is queried
        Then the mood should be "normal"

    Scenario: Actor switches to happy behavior
        Given a become actor
        When the mood is set to "happy"
        And the mood is queried
        Then the mood should be "happy"

    Scenario: Actor switches to angry behavior
        Given a become actor
        When the mood is set to "angry"
        And the mood is queried
        Then the mood should be "angry"

    Scenario: Actor reverts to default behavior with Unbecome
        Given a become actor
        When the mood is set to "happy"
        And the mood is set to "normal"
        And the mood is queried
        Then the mood should be "normal"

    Scenario: Actor transitions between multiple behaviors
        Given a become actor
        When the mood is set to "happy"
        And the mood is queried
        Then the mood should be "happy"
        When the mood is set to "angry"
        And the mood is queried
        Then the mood should be "angry"
        When the mood is set to "normal"
        And the mood is queried
        Then the mood should be "normal"
