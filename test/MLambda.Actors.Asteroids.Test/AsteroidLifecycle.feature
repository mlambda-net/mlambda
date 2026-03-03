Feature: Asteroid lifecycle
  In order to participate in the cluster as a lightweight gateway
  AsteroidService must register with cluster nodes on startup
  and send disconnect messages on shutdown.

  Scenario: Start without cluster nodes throws
    Given an asteroid service with no cluster nodes configured
    When the asteroid service is started
    Then an InvalidOperationException should be thrown

  Scenario: Start registers with cluster nodes
    Given an asteroid service with 2 cluster nodes configured
    When the asteroid service is started successfully
    Then an AsteroidRegister envelope should be sent to each cluster node

  Scenario: Stop sends disconnect to cluster nodes
    Given an asteroid service with 2 cluster nodes configured
    And the asteroid service is started successfully
    When the asteroid service is stopped
    Then an AsteroidDisconnected envelope should be sent to each cluster node
