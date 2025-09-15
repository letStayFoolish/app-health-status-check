namespace DeepCheck.Helpers;

// Example when not to use DI
public static class RandomNumberGenerator
{
    public static int GetRandomNumber() => Random.Shared.Next();
}
