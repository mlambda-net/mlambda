Feature: TCP transport send and receive
  In order to enable remote actor communication
  the TcpTransport must be able to send and receive envelopes between nodes.

  Scenario: Send and receive a tell envelope between two transports
    Given two TCP transports on different ports
    And both transports are started
    When transport A sends a tell envelope to transport B
    Then transport B should receive the envelope
    And the received envelope type should be "Tell"

  Scenario: Send and receive an ask envelope between two transports
    Given two TCP transports on different ports
    And both transports are started
    When transport A sends an ask envelope to transport B
    Then transport B should receive the envelope
    And the received envelope type should be "Ask"

  Scenario: Send and receive a response envelope
    Given two TCP transports on different ports
    And both transports are started
    When transport A sends a response envelope to transport B
    Then transport B should receive the envelope
    And the received envelope type should be "Response"

  Scenario: Payload data is preserved across transport
    Given two TCP transports on different ports
    And both transports are started
    When transport A sends an envelope with payload "TestPayload" to transport B
    Then transport B should receive the envelope with the correct payload
