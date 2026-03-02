Feature: Stateful counter actor
    Actors maintain internal state across messages.
    The counter actor demonstrates state management
    with increment, decrement, and query operations.

    Scenario: Counter starts at zero
        Given a counter actor
        When the count is queried
        Then the count should be 0

    Scenario: Counter increments
        Given a counter actor
        When the counter is incremented 3 times
        And the count is queried
        Then the count should be 3

    Scenario: Counter decrements
        Given a counter actor
        When the counter is incremented 5 times
        And the counter is decremented 2 times
        And the count is queried
        Then the count should be 3

    Scenario: Request-response pattern returns current value
        Given a counter actor
        When the counter is incremented 1 times
        Then the increment response should be 1
        When the counter is incremented 1 times
        Then the increment response should be 2
