#nullable enable

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="ScheduleException"/> and derived exception types.
/// </summary>
public static class ScheduleExceptionExtensions
{
	/// <summary>
	/// Creates a new <see cref="InvalidCronExpressionException"/> with an enhanced error message that includes the invalid cron expression.
	/// </summary>
	/// <param name="exception">The original exception.</param>
	/// <param name="cronExpression">The invalid cron expression to include in the error message.</param>
	/// <returns>A new <see cref="InvalidCronExpressionException"/> instance.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="cronExpression"/> is null.</exception>
	public static InvalidCronExpressionException WithCronExpression(this ScheduleException exception, string cronExpression)
	{
		if (exception is null)
		{
			throw new ArgumentNullException(nameof(exception));
		}

		if (cronExpression is null)
		{
			throw new ArgumentNullException(nameof(cronExpression));
		}

		return new InvalidCronExpressionException(cronExpression);
	}

	/// <summary>
	/// Creates a new <see cref="InvalidScheduleException"/> with the same schedule ID from the original exception.
	/// </summary>
	/// <param name="exception">The original exception.</param>
	/// <param name="newMessage">The new error message for the exception.</param>
	/// <returns>A new <see cref="InvalidScheduleException"/> instance.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="newMessage"/> is null.</exception>
	public static InvalidScheduleException WithScheduleContext(this ScheduleException exception, string newMessage)
	{
		if (exception is null)
		{
			throw new ArgumentNullException(nameof(exception));
		}

		if (newMessage is null)
		{
			throw new ArgumentNullException(nameof(newMessage));
		}

		if (exception.ScheduleId.HasValue)
		{
			return new InvalidScheduleException(newMessage, exception.ScheduleId.Value);
		}

		return new InvalidScheduleException(newMessage, Guid.Empty);
	}

	/// <summary>
	/// Determines whether the exception is one of the known schedule exception types.
	/// </summary>
	/// <param name="exception">The exception to check.</param>
	/// <returns>True if the exception is a ScheduleException or derived type; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
	public static bool IsScheduleException(this Exception exception)
	{
		if (exception is null)
		{
			throw new ArgumentNullException(nameof(exception));
		}

		return exception is ScheduleException;
	}

	/// <summary>
	/// Determines whether the exception is specifically an <see cref="InvalidCronExpressionException"/>.
	/// </summary>
	/// <param name="exception">The exception to check.</param>
	/// <returns>True if the exception is an InvalidCronExpressionException; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
	public static bool IsInvalidCronExpression(this Exception exception)
	{
		if (exception is null)
		{
			throw new ArgumentNullException(nameof(exception));
		}

		return exception is InvalidCronExpressionException;
	}
}
