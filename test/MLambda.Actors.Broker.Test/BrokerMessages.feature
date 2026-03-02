Feature: Broker Message Handling
    As a broker actor
    I want to handle route registration and lookup messages
    So that actors can be discovered across the cluster

    Scenario: Register route via broker actor
        Given a broker actor with an empty router table
        When a RegisterRoute message for "/user/hello" is sent
        Then the router table should contain route "/user/hello"

    Scenario: Lookup route via broker actor
        Given a broker actor with route "/user/hello" on node A
        When a LookupRoute message for "/user/hello" is sent
        Then the lookup result should contain route "/user/hello"

    Scenario: Announce routes from remote node
        Given a broker actor with an empty router table
        When an AnnounceRoutes message arrives from node B with routes "/user/alpha" and "/user/beta"
        Then the router table should contain route "/user/alpha"
        And the router table should contain route "/user/beta"

    Scenario: Node left removes its routes
        Given a broker actor with route "/user/hello" on node A
        When a NodeLeft message for node A is sent
        Then looking up route "/user/hello" in the table should return null
