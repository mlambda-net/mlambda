Feature: Router Table
    As a broker system
    I want to maintain a routing table for actor locations
    So that messages can be delivered to the correct nodes

    Scenario: Register and lookup a route
        Given an empty router table
        When route "/user/hello" is registered on node A
        Then looking up route "/user/hello" should return node A

    Scenario: Remove a route
        Given an empty router table
        And route "/user/hello" is registered on node A
        When route "/user/hello" is removed
        Then looking up route "/user/hello" should return null

    Scenario: Remove all routes for a node
        Given an empty router table
        And route "/user/hello" is registered on node A
        And route "/user/world" is registered on node A
        And route "/user/other" is registered on node B
        When all routes for node A are removed
        Then looking up route "/user/hello" should return null
        And looking up route "/user/world" should return null
        And looking up route "/user/other" should return node B

    Scenario: Get all routes
        Given an empty router table
        And route "/user/hello" is registered on node A
        And route "/user/world" is registered on node B
        Then the router table should contain 2 entries
