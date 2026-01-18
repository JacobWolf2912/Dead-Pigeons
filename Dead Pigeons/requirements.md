
Tech stack requirements
Tech stack requirements for your project. It is mandatory to follow these tech stack requirements in order to pass the exam.

For Systems Development:
You are required to make use of development practices covered during this course. The requirements are: 

Have one or more GitHub Actions workflows to automate relevant processes (like building and testing).

You must produce tests.

You must test test all service methods (both "happy path" and "unhappy path" tests). 
You must use XUnit.DependencyInjection for your test setup.
You must use TestContainers for isolating test persistence.
For Programming
The solution must be a distributed application consisting of 2 independent applications.

You must produce a React client application.

You must use React Router.
You must use Typescript (no vanilla Javascript in the source code)
You must produce a .NET Web API server application.

You must communicate with a relational database using Entity Framework.

You must perform server side data validation.

You must use some variant of OpenAPI / Swagger to provide living documentation for your API. (NSwag or other equivalent library)
You must use GUIDs (and not numeric ID's like 1,2,3 ...)

For CDS Security
Deployment

The whole system must be deployed to the cloud.

Authentication & Authorization

The system must have users.

They need to be able to authenticate.

Passwords are handled safely.

The system must have appropriate authorization policies determining who can access what.

Secrets 

No secrets for the deployed system in git repository.

You must document policies for who can access what.

 
README.md requirements
When you submit your source code, make a README.md file in the root of the git repo (don't give me some pdf report - you're a developer now).

In your README you should at least:

Document security policies for who can access what (authorization)
Document / explain environment, configuration and linting.
Summarize the current state of the project (what works, known bugs, etc)
There are no upper or lower character limit to the README.md. Just make it worth reading. You may add any chapter to the README.md that you think is valuable to have (either to yourself or to the assessors looking through your code).





 
Case details
Dead Pigeons 🐦 
 

Introduction 

The local sports club, Jerne IF, receives financial support using a game called “Dead Pigeons” played by supporters of the club. There are issues regarding managing and scaling the current non-digital variant of the game, so a web solution is proposed. 

 

Rules 

“Dead pigeons” is a lottery-style game in which a number of people guess winning numbers from a board. 

  

The administrator will draw 3 random numbers by the end of each week.

These numbers are the winning sequence. They are drawn out of a physical hat and can be entered by the admin on the web app (no random number generator – just a number selection menu). 

Each player places 5-8 numbers on their boards in an attempt to guess the winning sequence. 

The total sum used for prizes is 70% of the revenue from selling the boards to the players. The remaining 30% is kept by the sports facility. 

Pretty simple, right? Here are the caveats... 

There can be any number of winners, since any number of boards can include the winning sequence. 

A singular player can purchase any number of boards. 

They may play the same sequence any number of times or multiple different sequences. 

If they get multiple boards with the winning sequence, they will get a share of the winning sum multiple times. 

The purchase price for each board is dependent on the number of numbers you select in that sequence. Below is the table of prices: 

5 fields 

20 DKK 

6 fields 

40 DKK 

7 fields 

80 DKK 

8 fields 

160 DKK 

 

The numbers on the board will always be 1-16.
Once the board administrator has drawn the winning numbers, no more guesses can be made / players can join that game. 

Additionally, players may only join the game until 5 o’clock Saturday (PM, afternoon) Danish local time. 

The next game automatically starts once the admin has entered the winning numbers for an existing. 

Players can choose to repeat boards for X number of games. (For example, play the same board for 10 weeks in a row). It should also be possible to stop the repeating board (opt-out of the game). 

You must use a “balance” system where payments made to Jerne IF increase the account balance and purchased boards decrease account balance. 

The balance cannot be negative. The player will have to start off by submitting money words the balance, and then they can play for X number of weeks. 

When players deposit money towards the “balance”, they should be able to attach the MobilePay transaction number  

It is always possible for the administrator to check transactions by transaction number in MobilePay.  

By default, a transaction is “pending” until it is approved by an admin. Once it has been approved it will proceed towards the balance and can be used to purchase boards. 

Winning sums do not go towards the balance: Prize money is handled by the admins separately. 

Some players will play digitally (your app) while others play physically. They participate in the same “round”, meaning your app simply must keep track of the digital participants.  

When the administrators figure out how much prize money to pay each winner, they calculate this manually by adding in the physical participants. Your application should therefore not calculate total prize for each winner. 

The administrator should be able to get a clear overview of data in the application with everything being timestamped: 

The admin requires full CRUD on players  

The player is registered by the admin with full name, phone number and email address. 

The player can be marked as active or inactive by the Admin. Only active players may buy boards / participate in games. By default a player is inactive. 

Games (the weekly occurrence)
Overview should show the history for all games. This includes: 

What boards each player has played + indicate whether they are winning boards. 

The total number of winning boards to quickly add together with the physical boards. 

Sequence / number ordering of guesses and winner numbers don’t matter. 

Example: someone guessing 4-1-7-2-5 while the winning numbers are 2-5-1 is still a valid win. 

 

By the end of the project, the group which most gracefully and production-ready fulfills the requirements (given you accept to) will put the system into production for Jerne IF.

 
 
Example starter app
Here's my boilerplate setup which you may use for inspiration for your setups. It includes authentication and all of the stuff we've worked with in programming + testing: https://github.com/uldahlalex/starter-25





 
Q&A & general advice
During the project announcement you may have some questions. If I think the questions and answers may be very valuable to others at a later point in time, I'll upload them here. I'll also add in tips for solving the exam case.

Tip 1: State-less API. It might be tempting to make some automation that creates a new game every X days / using schedulers / etc. I will strongly advise against this. I recommend simply seeding "inactive" games into the database for each week the next 20 years using a loop, and then the function that "ends" a game (by publishing the winning numbers) simply makes the next week "active" as a single database transaction. This is the least failure-prone implementation i can think of.

Tip 2: Soft-delete everything. You want full history of all activities, so no hard deletes that completely erase data.

Tip 3: "Balance" is calculated as a sum of approved transactions minus all "board purchases" so it's always verifiable by history if a person has positive balance. That means there is no "balance column"; It is simply a product of past history! 