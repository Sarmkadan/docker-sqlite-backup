// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Api;

/// <summary>
/// Builder for constructing request processing pipelines.
/// Enables fluent API for adding middleware and handlers in sequence.
/// </summary>
public class PipelineBuilder
{
    private readonly List<Delegate> _middlewares = [];

    /// <summary>
    /// Adds middleware to the pipeline.
    /// </summary>
    public PipelineBuilder Use(Delegate middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Adds a handler at the end of the pipeline.
    /// </summary>
    public PipelineBuilder Terminal(Func<Task> handler)
    {
        _middlewares.Add(async () => await handler());
        return this;
    }

    /// <summary>
    /// Adds conditional middleware that only executes if predicate is true.
    /// </summary>
    public PipelineBuilder UseWhen(Func<bool> predicate, Delegate middleware)
    {
        if (predicate())
            _middlewares.Add(middleware);

        return this;
    }

    /// <summary>
    /// Builds the pipeline into a single executable delegate.
    /// </summary>
    public Func<Task> Build()
    {
        return async () =>
        {
            var index = 0;

            Func<Task>? next = null;
            next = async () =>
            {
                if (index >= _middlewares.Count)
                    return;

                index++;
                var middleware = _middlewares[index - 1];

                // Support async and sync delegates
                if (middleware is Func<Task> asyncFunc)
                    await asyncFunc();
                else if (middleware is Func<Func<Task>, Task> asyncWith)
                    await asyncWith(() => next());
                else if (middleware is Action syncAction)
                    syncAction();
            };

            await next();
        };
    }

    /// <summary>
    /// Gets the number of middleware components in the pipeline.
    /// </summary>
    public int Count => _middlewares.Count;

    /// <summary>
    /// Clears all middleware from the pipeline.
    /// </summary>
    public void Clear()
    {
        _middlewares.Clear();
    }
}
