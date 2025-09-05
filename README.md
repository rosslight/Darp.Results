# Darp.Results

A Result type implementation for C# that provides a safe way to handle operations that can succeed or fail, inspired by Rust's `Result<T, E>` type.

## Why another results library?

- Get values through switching
- API heavily inspired by Rust's [Result<T, E>](https://doc.rust-lang.org/std/result/enum.Result.html)
- Ability to attach metadata to results
- Immutability
- Any object should be a possible error type (e.g., enums)
- Analyzers/CodeFixers
- Designed to benefit from the [discriminated unions proposal](https://github.com/dotnet/csharplang/tree/main/meetings/working-groups/discriminated-unions)

Existing projects did not fit all the requirements. However, these projects were an inspiration:

- [FluentResults](https://github.com/altmann/FluentResults)
- [LightResults](https://github.com/jscarle/LightResults)
- [error-or](https://github.com/amantinband/error-or)
- [DotNext Result](https://dotnet.github.io/dotNext/features/core/result.html)
- [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions)
- [language-ext](https://github.com/louthy/language-ext/wiki/How-to-handle-errors-in-a-functional-way)

## Packages

- **Darp.Results** - Core Result type implementation
- **Darp.Results.Analyzers** - Roslyn analyzers for Result usage patterns. For documentation see [here](src/Darp.Results.Analyzers/README.md)
- **Darp.Results.Shouldly** - Shouldly extensions for Result testing

## Quick Start

```csharp
using Darp.Results;

// Create Results
Result<int, string> success = Result.Ok<int, string>(42);
Result<int, string> failure = Result.Error<int, string>("Something went wrong");

// Implicit conversions
Result<int, string> implicitSuccess = 42;
Result<int, string> implicitFailure = "error";

// Extract values safely
if (result.TryGetValue(out int value)) { /* use value */ }
if (result.TryGetError(out string error)) { /* handle error */ }

// Switch on result
return result switch 
{
    Result<int, string>.Ok(var value) => value,
    Result<int, string>.Err(var error) => error,
}
```

## Core API Reference

### Creation

#### Factory Methods
```csharp
// Create success result
Result<TValue, TError> Result.Ok<TValue, TError>(TValue value, IDictionary<string, object>? metadata = null)

// Create error result  
Result<TValue, TError> Result.Error<TValue, TError>(TError error, IDictionary<string, object>? metadata = null)

// Try-catch wrapper
Result<TValue, Exception> Result.Try<TValue>(Func<TValue> func)

// TryParse wrapper
Result<TValue, StandardError> Result.From<T, TValue>(T input, TryParseFunc<T, TValue> tryParse)
```

#### Implicit Conversions
```csharp
// Value to Result
Result<int, string> result = 42; // Creates Ok(42)

// Error to Result  
Result<int, string> result = "error"; // Creates Err("error")
```

### State Checking

```csharp
bool IsOk { get; }  // True if result is success
bool IsErr { get; } // True if result is error
```

### Value Extraction

#### Safe Extraction
```csharp
// Extract value
bool TryGetValue(out TValue value)
bool TryGetValue(out TValue value, out Result<TNewValue, TError>.Err error)
bool TryGetValue<TNewValue>(out TValue value, out Result<TNewValue, TError>.Err error)

// Extract error
bool TryGetError(out TError error)
bool TryGetError(out TError error, out Result<TValue, TNewError>.Ok success)
bool TryGetError<TNewError>(out TError error, out Result<TValue, TNewError>.Ok success)

// Extract with a default value
TValue Unwrap(TValue defaultValue)           // Returns value or default
TValue Unwrap(Func<TValue> valueProvider)    // Returns value or computed default
```

#### Unsafe Extraction (throws on wrong state)
```csharp
TValue Unwrap()                    // Throws if error
TValue Expect(string message)      // Throws with custom message if error
TError UnwrapError()               // Throws if success
TError ExpectError(string message) // Throws with custom message if success
```

### Transformations

#### Map Operations
```csharp
// Transform success value
Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> func)

// Transform error
Result<TValue, TNewError> MapError<TNewError>(Func<TError, TNewError> func)
```

#### Logical Combinators
```csharp
// And operations
Result<TNewValue, TError> And<TNewValue>(Result<TNewValue, TError> result)
Result<TValue, TError> And(Func<TValue, Result<TValue, TError>> resultProvider)
Result<TNewValue, TError> And<TNewValue>(Func<TValue, Result<TNewValue, TError>> resultProvider)

// Or operations  
Result<TValue, TError> Or(Result<TValue, TError> result)
Result<TValue, TError> Or(Func<TError, Result<TValue, TError>> resultProvider)
Result<TValue, TNewError> Or<TNewError>(Func<TError, Result<TValue, TNewError>> resultProvider)
```

### Utility Operations

#### Flattening
```csharp
// Flatten nested Results
Result<TValue, TError> Flatten<TValue, TError>(Result<Result<TValue, TError>, TError> result)
```

#### Iteration
```csharp
// Enumerate success values (yields single value for Ok, empty for Err)
IEnumerable<TValue> AsEnumerable()
IEnumerator<TValue> GetEnumerator() // Enables foreach
```

### Metadata

```csharp
// Add metadata
Result<TValue, TError> WithMetadata(string key, object value)
Result<TValue, TError> WithMetadata(ICollection<KeyValuePair<string, object>> metadata)

// Access metadata
IReadOnlyDictionary<string, object> Metadata { get; }
```

## Testing with Shouldly

```csharp
using Darp.Results.Shouldly;

// Assert success
result.ShouldBeSuccess();
result.ShouldHaveValue(expectedValue);
result.ShouldHaveValue(value => value.ShouldBeGreaterThan(0));

// Assert error
result.ShouldBeError();
result.ShouldHaveError(expectedError);
result.ShouldHaveError(error => error.ShouldNotBeNull());
```

## Examples
### Basic Usage
Divide two numbers, return a result and attempt to get the value
```csharp
public Result<int, string> Divide(int a, int b)
{
    if (b == 0)
        return "Cannot divide by zero";
    return a / b;
}

var result = Divide(10, 2);
if (result.TryGetValue(out int value))
{
    Console.WriteLine($"Result: {value}");
}
```

### Early returns
To achieve early returns, rust provides the `?` operator. Due to shortcomings of c#, we'll use the TryGet pattern to get the error
```csharp
public Result<string, StandardError> WorkWithResult(Result<int, StandardError> result)
{
    if (!result.TryGetValue(out int value, out Result<string, StandardError>.Err? err))
        return err;
    // ...
    return value.ToString();
}
```

### Chaining Operations
```csharp
var result = Result.Ok<int, string>(10)
    .Map(x => x * 2)
    .And(x => x > 15 ? Result.Ok<string, string>($"Big: {x}") : Result.Error<string, string>("Too small"))
    .Map(s => s.ToUpper());

result.ShouldHaveValue("BIG: 20");
```

### Switching
Use switch expressions to easily access value/error
```csharp
public Result<string, StandardError> SwitchOnResult(Result<int, StandardError> result)
{
    return result switch
    {
        Result<int, StandardError>.Ok(var value, var metadata) => value.ToString(),
        Result<int, StandardError>.Err(var error, var metadata) => error,
    };
}
```

### With Metadata
```csharp
var result = Result.Ok<int, string>(42)
    .WithMetadata("operation", "calculation")
    .WithMetadata("timestamp", DateTime.UtcNow);

// Metadata is preserved through transformations
var transformed = result.Map(x => x.ToString());
// transformed.Metadata still contains the original metadata
```

## License

This project is licensed under the Apache License 2.0.
