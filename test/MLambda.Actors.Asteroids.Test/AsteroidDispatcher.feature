Feature: Asteroid message dispatcher
  In order to handle responses and topology updates on asteroid nodes
  the AsteroidMessageDispatcher must correlate responses to pending requests
  and route topology messages to local actors.

  Scenario: Response envelope completes pending request
    Given an asteroid message dispatcher with a pending request
    When a response envelope arrives with the matching correlation id
    Then the pending request should be completed with the response payload

  Scenario: Unknown correlation id is ignored
    Given an asteroid message dispatcher with no pending requests
    When a response envelope arrives with an unknown correlation id
    Then no exception should be thrown

  Scenario: Topology envelope routes to local actor
    Given an asteroid message dispatcher with a topology handler
    When a topology envelope arrives with target route "dispatcher"
    Then the topology payload should be routed to the local actor
