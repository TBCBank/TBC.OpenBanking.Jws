## General instructions
- Do not update `global.json` file.
- There should be no trailing whitespace in any lines.

## C# Instructions
- Always use the latest version C#, currently C# 14 features.
- Write clear and concise comments for each class, method and property.

## General Instructions
- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- For libraries or external dependencies, mention their usage and purpose in comments.

## Naming Conventions
- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).

## Formatting
- Respect and apply code-formatting styles and naming conventions defined in `.editorconfig`.
- Prefer file-scoped namespace declarations and single-line using directives.
- Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.
- Add a blank line before XML documentation comments (`///`) when they follow other code (methods, properties, fields, etc.).

## Project Setup and Structure
- Guide users through creating a new .NET project with the appropriate templates.
- Explain the purpose of each generated file and folder to build understanding of the project structure.

## Nullable Reference Types
- Declare variables non-nullable, and check for `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- In internal and private methods, trust the C# null annotations and don't add null checks when the type system says a value cannot be null. In public methods, check all reference type arguments for null-ness (Use `ArgumentNullException.ThrowIfNull`). Never check struct arguments for null-ness.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).

## Testing
- Always include test cases for critical paths of the application.
- Guide users through creating unit tests.
- Emit "Act", "Arrange" and "Assert" comments.
- Copy existing style in nearby files for test method names and capitalization.
- Explain integration testing approaches for API endpoints.
- Demonstrate how to mock dependencies for effective testing.
- Show how to test authentication and authorization logic.
- Explain test-driven development principles as applied to API development.
- When running tests, if possible use filters and check test run counts, or look at test logs, to ensure they actually ran.
