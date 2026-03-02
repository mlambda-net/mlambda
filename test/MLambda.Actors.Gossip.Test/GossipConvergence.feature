Feature: Gossip state convergence
  In order to maintain consistent cluster views
  gossip state must converge across multiple nodes.

  Scenario: Gossip state merges correctly
    Given a gossip state with member A as Up
    And another gossip state with member B as Up
    When the states are merged
    Then the merged state should contain both members

  Scenario: Higher heartbeat wins during merge
    Given a gossip state with member A heartbeat 5 status Up
    And another gossip state with member A heartbeat 10 status Suspect
    When the states are merged
    Then member A should have status Suspect and heartbeat 10
