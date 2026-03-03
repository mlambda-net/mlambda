Feature: Asteroid routing
  In order to send messages from asteroid gateway nodes to cluster-hosted actors
  AsteroidRouteAddress must dispatch work to the local DispatcherActor
  and support parameterized routes.

  Scenario: Tell message dispatches to dispatcher
    Given an asteroid route address for route "greeter"
    When a tell message is sent via the asteroid address
    Then a DispatchWork should have been sent to the dispatcher
    And the DispatchWork target route should be "greeter"
    And the DispatchWork IsAsk should be false

  Scenario: Ask message tracks pending request
    Given an asteroid route address for route "greeter"
    When an ask message is initiated via the asteroid address
    Then a DispatchWork should have been sent to the dispatcher
    And the DispatchWork IsAsk should be true

  Scenario: Parameterized tell resolves route
    Given an asteroid route address for route "manager/{id}"
    When a tell message is sent with parameter id equal to 112233
    Then a DispatchWork should have been sent to the dispatcher
    And the DispatchWork target route should be "manager/112233"

  Scenario: Parameterized ask resolves route
    Given an asteroid route address for route "manager/{id}"
    When an ask message is sent with parameter id equal to 112233
    Then a DispatchWork should have been sent to the dispatcher
    And the DispatchWork target route should be "manager/112233"
    And the DispatchWork IsAsk should be true
