Feature: Actor type registry
  In order to resolve actor types from route names
  the ActorTypeRegistry must support exact and template-based matching.

  Scenario: Exact route matches simple actor
    Given an actor type registry with a simple route actor
    When looking up route "greeter"
    Then the resolved type should be the simple route actor type

  Scenario: Template route matches resolved route
    Given an actor type registry with a parameterized route actor
    When looking up route "manager/112233"
    Then the resolved type should be the parameterized route actor type

  Scenario: Unknown route returns false
    Given an actor type registry with a simple route actor
    When looking up route "unknown"
    Then the type lookup should fail

  Scenario: GetAllRoutes includes both types
    Given an actor type registry with both simple and parameterized actors
    When getting all routes
    Then the routes should include "greeter" and "manager/{id}"
