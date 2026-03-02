Feature: Discovery Tracking
    As a discovery actor
    I want to track routes announced by remote nodes
    So that I can provide a complete view of the cluster topology

    Scenario: Track routes from announced node
        Given a discovery actor
        When the discovery actor receives an AnnounceRoutes from node C with routes "/user/svc1" and "/user/svc2"
        Then discovering routes should return 2 entries

    Scenario: Node left clears discovered routes
        Given a discovery actor
        And the discovery actor receives an AnnounceRoutes from node C with routes "/user/svc1" and "/user/svc2"
        When a NodeLeft message for node C is sent to discovery
        Then discovering routes should return 0 entries
