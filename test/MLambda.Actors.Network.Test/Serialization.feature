Feature: Message serialization round-trip
  In order to transport actor messages across the network
  the serializer must correctly serialize and deserialize various message types.

  Scenario: Serialize and deserialize a string message
    Given a JsonMessageSerializer
    When I serialize the string message "Hello World"
    Then the deserialized message should equal "Hello World"

  Scenario: Serialize and deserialize an integer message
    Given a JsonMessageSerializer
    When I serialize the integer message 42
    Then the deserialized integer message should equal 42

  Scenario: Serialize and deserialize a complex object message
    Given a JsonMessageSerializer
    When I serialize a complex message with Name "Alice" and Value 100
    Then the deserialized complex message should have Name "Alice" and Value 100

  Scenario: Envelope codec round-trip
    Given an envelope with type "Tell" and correlation id
    When the envelope is encoded and decoded
    Then the decoded envelope should match the original

  Scenario: Envelope codec preserves ask type and source node
    Given an envelope with type "Ask" and source node "127.0.0.1" port 9001
    When the envelope is encoded and decoded
    Then the decoded envelope should have type "Ask"
    And the decoded envelope source node should be "127.0.0.1" port 9001
