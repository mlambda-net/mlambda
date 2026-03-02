Feature: Cluster membership via gossip
  In order to form a distributed actor cluster
  nodes must be able to join and leave the cluster via gossip protocol.

  Scenario: Two nodes join a cluster and discover each other
    Given a cluster node A on a random port
    And a cluster node B on a random port with A as seed
    When both cluster nodes are started
    And we wait for gossip convergence
    Then node A should have 2 members
    And node B should have 2 members

  Scenario: Graceful leave removes member cleanly
    Given a cluster node A on a random port
    And a cluster node B on a random port with A as seed
    And both cluster nodes are started
    And we wait for gossip convergence
    When node B gracefully leaves
    And we wait for leave propagation
    Then node A should see node B as Leaving or Down or Removed
