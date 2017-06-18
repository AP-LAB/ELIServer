Feature: MessageHandlerFeature
			In order to test the workflow of the MessageHandler

			//
@NormalLogin
Scenario: Normal Login
       Given A new client is connected
       And The incoming message has arrived
       When The type of the message is "LogIn"
	   And the correct credentials used
       Then a message should be returned to the user with the user's information