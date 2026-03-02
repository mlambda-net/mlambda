Feature: Address resolution
  In order to transparently send messages to local or remote actors
  the address resolver must return the appropriate address type.

  Scenario: Resolve returns null for unknown actor
    Given an address resolver with an empty registry
    When resolving an unknown actor id
    Then the resolved address should be null

  Scenario: Resolve returns remote address for registered remote actor
    Given an address resolver with actor X registered on a remote node
    When resolving actor X
    Then the resolved address should be a remote address
