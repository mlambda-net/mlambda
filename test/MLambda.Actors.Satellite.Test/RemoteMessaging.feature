Feature: Remote messaging
  In order to communicate with actors on remote nodes
  messages must be serialized into envelopes and dispatched over the network.

  Scenario: Send a tell message to a remote address
    Given a remote address for actor X on a remote node
    When a tell message is sent to the remote address
    Then the transport should have sent a Tell envelope

  Scenario: Response correlation for ask messages
    Given a pending request with correlation id C
    When a response envelope arrives with correlation id C
    Then the pending request should be completed with the response payload
