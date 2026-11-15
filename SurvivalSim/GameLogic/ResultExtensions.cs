using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class ResultExtensions
{
    public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> transform)
    {
        return result switch
        {
            Result<T>.Success success => new Result<U>.Success(transform(success.Value)),
            Result<T>.Fail fail => new Result<U>.Fail(fail.Code, fail.Message),
            _ => throw new InvalidOperationException("Unknown return type")
        };
    }
    public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> next)
    {
        return result switch
        {
            Result<T>.Success success => next(success.Value),
            Result<T>.Fail fail => new Result<U>.Fail(fail.Code, fail.Message),
            _ => throw new InvalidOperationException("Unknown return type")
        };
    } 
}
