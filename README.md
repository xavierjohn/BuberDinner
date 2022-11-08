# Buber Dinner

## The idea
Allows you to turn your home into a restaurant where...
Just like people turning their homes into hotels via AirBNB.

## Concepts & Tech used
* .NET 7, EF Core
* Clean Architecture & Domain-Driven Design principles
* Common patterns such as CQRS, unit of work, repository, mediator
* Open source libraries such as MediatR, FluentValidation, ErroOr, Throw, Mapster
* Authentication: JWT tokens

### Clean Architecture
![](readme-assets/clean-architecture-diagram.png)
![](readme-assets/clean-architecture-diagram-2.png)
![](readme-assets/clean-architecture-detailed.png)
* The **Domain** and **Application** layers are the focus and therefore the core of the system.
* The Domain layer contains **enterprise logic** and **types**. The application layer contains **business logic** and **types**.
* Infrastructure and Presentation depend on Core, but not on one another.