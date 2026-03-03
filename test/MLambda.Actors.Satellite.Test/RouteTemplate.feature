Feature: Route template
  In order to support parameterized actor routes
  RouteTemplate must parse templates, resolve parameters, and match resolved routes.

  Scenario: Simple route is not parameterized
    Given a route template "greeter"
    Then the template should not be parameterized

  Scenario: Template route is parameterized
    Given a route template "manager/{id}"
    Then the template should be parameterized
    And the parameter names should be "id"

  Scenario: Multi-parameter template is parameterized
    Given a route template "org/{orgId}/user/{userId}"
    Then the template should be parameterized
    And the parameter names should be "orgid,userid"

  Scenario: Resolve substitutes parameters
    Given a route template "manager/{id}"
    When resolved with parameter id equal to 112233
    Then the resolved route should be "manager/112233"

  Scenario: Resolve with multiple parameters
    Given a route template "org/{orgId}/user/{userId}"
    When resolved with parameters orgId equal to "acme" and userId equal to "42"
    Then the resolved route should be "org/acme/user/42"

  Scenario: Resolve with missing parameter throws
    Given a route template "manager/{id}"
    When resolved with empty parameters
    Then an ArgumentException should be thrown

  Scenario: TryMatch extracts parameters
    Given a route template "manager/{id}"
    When matching against "manager/112233"
    Then the match should succeed
    And the extracted parameter id should be "112233"

  Scenario: TryMatch rejects segment count mismatch
    Given a route template "manager/{id}"
    When matching against "manager/1/extra"
    Then the match should fail

  Scenario: TryMatch rejects literal mismatch
    Given a route template "manager/{id}"
    When matching against "worker/112233"
    Then the match should fail

  Scenario: GetBaseRoute returns prefix
    Given a route template "manager/{id}"
    Then the base route should be "manager"

  Scenario: GetBaseRoute for simple route
    Given a route template "greeter"
    Then the base route should be "greeter"
