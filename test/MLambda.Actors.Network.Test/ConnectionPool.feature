Feature: Connection pool management
  In order to efficiently manage network connections
  the TcpTransport should maintain a connection pool with connect and disconnect handling.

  Scenario: Connection established event is published
    Given two TCP transports on different ports with event streams
    And both transports are started
    When a tell envelope is sent from transport A to transport B
    Then a ConnectionEstablished event should be published

  Scenario: Connection lost event on transport stop
    Given two TCP transports on different ports with event streams
    And both transports are started
    And a tell envelope is sent from transport A to transport B
    When transport B is stopped
    Then a ConnectionLost event should eventually be published

  Scenario: Reconnection after disconnect
    Given two TCP transports on different ports with event streams
    And both transports are started
    And a tell envelope is sent from transport A to transport B
    When transport B is stopped and restarted
    And another tell envelope is sent from transport A to transport B
    Then transport B should receive the second envelope
