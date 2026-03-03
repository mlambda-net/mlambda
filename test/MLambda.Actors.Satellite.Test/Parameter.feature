Feature: Parameter
  In order to pass route parameters to parameterized actor routes
  the Parameter class must support case-insensitive key access
  and dictionary conversion.

  Scenario: Indexer sets and gets value
    Given a parameter with key "id" set to 123
    Then getting key "id" should return 123

  Scenario: Keys are case insensitive
    Given a parameter with key "Id" set to 456
    Then getting key "id" should return 456
    And getting key "ID" should return 456

  Scenario: ToDictionary returns copy
    Given a parameter with key "name" set to "alice"
    When converted to dictionary
    Then the dictionary should contain key "name" with value "alice"

  Scenario: IsEmpty for new parameter
    Given a new empty parameter
    Then the parameter should be empty

  Scenario: IsEmpty after set
    Given a parameter with key "id" set to 1
    Then the parameter should not be empty

  Scenario: Constructor from dictionary lowercases keys
    Given a parameter constructed from a dictionary with key "MyKey" and value "hello"
    Then getting key "mykey" should return "hello"
