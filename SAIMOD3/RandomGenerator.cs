
namespace SAIMOD3;

public static class RandomGenerator
{
    private static readonly Random Random = new();
    
    public static int GenerateDetailType(float probability)
    {
        return Random.NextDouble() < probability ? 1 : 2;
    }

    public static float GetExponentialRandom(float mean)
    {
        double u = Random.NextDouble();
        return (float)(-mean * Math.Log(u));
    }
    
    public static float GetNormalRandom(float a, float s2)
    {
        float usum = -6;
        int i = 0;

        while (i++ < 12)
        {
            usum += GenerateUniformRandom(0, 1);
        }

        return a + usum * (float)Math.Sqrt(s2);
    }
    
    public static float GenerateUniformRandom(float min, float max)
    {
        if (min >= max)
        {
            throw new ArgumentException("Minimum value must be less than maximum value.");
        }

        return min + (float)(Random.NextDouble() * (max - min));
    }
}