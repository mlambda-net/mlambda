Feature: Failure detection
  In order to detect unreachable nodes
  the phi accrual failure detector tracks heartbeat intervals.

  Scenario: Node is available after heartbeats
    Given a phi accrual failure detector with threshold 8.0
    When heartbeats are received from node X at regular intervals
    Then node X should be available

  Scenario: Node becomes unavailable after missed heartbeats
    Given a phi accrual failure detector with threshold 3.0
    When heartbeats are received from node X then stopped
    And we wait for the suspicion to rise
    Then node X should not be available
