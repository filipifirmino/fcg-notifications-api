Feature: PaymentProcessed Notification
  As the system
  When a payment is processed
  The appropriate email notification should be sent based on the payment outcome

  Scenario: Purchase confirmation sent when payment is approved
    Given a payment for game "God of War" to email "player@fcg.com" with amount 199.90 was approved
    When the PaymentProcessed event is consumed
    Then SendPurchaseConfirmationAsync should be called once
    And it should be called with email "player@fcg.com" and game "God of War" and amount 199.90

  Scenario: Purchase rejection sent when payment is rejected
    Given a payment for game "Cyberpunk 2077" to email "gamer@fcg.com" was rejected with reason "Insufficient funds (simulated)"
    When the PaymentProcessed event is consumed
    Then SendPurchaseRejectedAsync should be called once
    And it should be called with email "gamer@fcg.com" and game "Cyberpunk 2077" and reason "Insufficient funds (simulated)"

  Scenario: Confirmation is not sent when payment is rejected
    Given a payment for game "The Witcher 3" to email "fan@fcg.com" was rejected with reason "Insufficient funds (simulated)"
    When the PaymentProcessed event is consumed
    Then SendPurchaseConfirmationAsync should not be called

  Scenario: Rejection is not sent when payment is approved
    Given a payment for game "Elden Ring" to email "soulsplayer@fcg.com" with amount 299.90 was approved
    When the PaymentProcessed event is consumed
    Then SendPurchaseRejectedAsync should not be called
