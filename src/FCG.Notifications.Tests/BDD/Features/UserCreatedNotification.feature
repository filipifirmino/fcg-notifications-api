Feature: UserCreated Notification
  As the system
  When a new user registers on the platform
  A welcome email notification should be sent

  Scenario: Welcome email is sent when user is created
    Given a new user with name "João Silva" and email "joao@fcg.com" was created
    When the UserCreated event is consumed
    Then SendWelcomeEmailAsync should be called once
    And it should be called with name "João Silva" and email "joao@fcg.com"

  Scenario: Welcome email uses the correct user name
    Given a new user with name "Maria Souza" and email "maria@fcg.com" was created
    When the UserCreated event is consumed
    Then SendWelcomeEmailAsync should be called with name "Maria Souza"

  Scenario: Welcome email uses the correct user email
    Given a new user with name "Carlos Lima" and email "carlos@fcg.com" was created
    When the UserCreated event is consumed
    Then SendWelcomeEmailAsync should be called with email "carlos@fcg.com"
