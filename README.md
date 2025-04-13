# Async Logger
## OK code test
### .NET 8 Console Application


#### Considerations:
+ SOLID
	- Single Responsibility Principle
		1. Separating logger class from Application.
		1. DI helps ensure that classes have a single responsibility by delegating object creation and management to a DI container.
		1. AsyncLogger class is focused on one responsibility.
	- Open/Closed Principle
		1. By depending on abstractions (interfaces) rather than concrete implementations.
		1. We can introduce new implementations without changing the existing code.
	- Liskov Substitution Principle
		1. Using interfaces ensure that derived classes or implementations can be used interchangeably without altering the correctness of the program.
	- Interface Segregation
		1. Defining separate, small interfaces for different responsibilities so that clients won't have to implement the interfaces which they don't use.
	- Dependency Inversion Principle
		1. High-level modules are not depend on low-level modules but on abstractions.
		1. DI promotes this principle by injecting dependencies through interfaces rather than instantiating concrete classes directly.
+ Clean Code
	1. Use descriptive names.
	1. Avoid hardcoding strings, values, ….
	1. Exception handling when dealing with files.
+ Testability
	1. AsyncLogger class dependencies can be injected.
	1. We must also add a wrapper around StreamWriter in inject it to the AsyncLogger so that tests can mock it (Although in this case, I didn't do it because unit tests actually write to files).
+ Performance
	1. Using `Channel` to pass data between producers and consumers asynchronously.
	1. Using buffer to minimize the file operations.
+ Optimization
	1. Use `StreamWrite` to process line by line instead of `File.WriteText`.

### Further improvements:

+ Add LogLevel, fallback output, different log sources like AWS CloudWatch.