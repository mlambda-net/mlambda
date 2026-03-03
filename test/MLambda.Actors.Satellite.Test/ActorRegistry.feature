Feature: Actor registry
  In order to track actor locations across the cluster
  the actor registry maps actor identifiers to hosting nodes.

  Scenario: Register and lookup an actor
    Given an actor registry
    When actor X is registered on node A
    Then looking up actor X should return node A

  Scenario: Remove an actor from registry
    Given an actor registry
    And actor X is registered on node A
    When actor X is removed from the registry
    Then looking up actor X should return null

  Scenario: Get all registered actors
    Given an actor registry
    And actor X is registered on node A
    And actor Y is registered on node B
    Then the registry should contain 2 entries
