namespace Common;

public static class ProfileUtil
{
    // this still doesnt work (public uid), oh well
    public static char RandomLetter() => (char)Random.Shared.Next('A', 'Z' + 1);

    public static int GetOrderId(int personalityId)
    {
        return (personalityId / 100) switch
        {
            >= 101 and <= 112 => personalityId / 100 - 100,
            _ => 0,
        };
    }
}
