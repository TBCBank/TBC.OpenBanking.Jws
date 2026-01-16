// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET

#nullable enable
#pragma warning disable IDE0005, IDE0290, MA0048

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Provides downlevel polyfills for static methods on Exception-derived types.
    /// </summary>
    internal static class ExceptionPolyfills
    {
        extension(ArgumentNullException)
        {
            public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument is null)
                {
                    ThrowArgumentNullException(paramName);
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowArgumentNullException(string? paramName) =>
            throw new ArgumentNullException(paramName);

        extension(ArgumentOutOfRangeException)
        {
            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.
            /// </summary>
            /// <param name="value">The argument to validate as non-negative.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            {
                if (value < 0)
                    ThrowNegative(value, paramName);
            }
        }

        [DoesNotReturn]
        private static void ThrowNegative<T>(T value, string? paramName) =>
            throw new ArgumentOutOfRangeException(paramName, value,
                string.Format(CultureInfo.InvariantCulture, "{0} ('{1}') must be a non-negative value.", paramName, value));
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Applied to a method that will never return under any circumstance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [ExcludeFromCodeCoverage]
    internal sealed class DoesNotReturnAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifies that the method will not return if the associated Boolean parameter is passed the specified value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    [ExcludeFromCodeCoverage]
    internal sealed class DoesNotReturnIfAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with the specified parameter value.
        /// </summary>
        /// <param name="parameterValue">
        /// The condition parameter value. Code after the method will be considered unreachable by diagnostics if the argument to
        /// the associated parameter matches this value.
        /// </param>
        public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;

        /// <summary>
        /// Gets the condition parameter value.
        /// </summary>
        public bool ParameterValue { get; }
    }

    /// <summary>
    /// Specifies that an output will not be null even if the corresponding type allows it.
    /// Specifies that an input argument was not null when the call returns.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    [ExcludeFromCodeCoverage]
    internal sealed class NotNullAttribute : Attribute
    {
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Tags parameter that should be filled with specific caller name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    [ExcludeFromCodeCoverage]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallerArgumentExpressionAttribute"/> class.
        /// </summary>
        /// <param name="parameterName">Function parameter to take the name from.</param>
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        /// <summary>
        /// Gets name of the function parameter that name should be taken from.
        /// </summary>
        public string ParameterName { get; }
    }
}

#endif
