using Ardalis.GuardClauses;

namespace ProductIntelligence.Core.ValueObjects;

/// <summary>
/// Value object for OpenAI embedding vectors (1536 dimensions for text-embedding-3-small)
/// </summary>
public record EmbeddingVector
{
    public float[] Values { get; init; }
    public int Dimensions => Values.Length;

    public EmbeddingVector(float[] values)
    {
        Guard.Against.NullOrEmpty(values, nameof(values));
        
        if (values.Length != 1536 && values.Length != 3072)
        {
            throw new ArgumentException("Embedding must be 1536 or 3072 dimensions");
        }
        
        Values = values;
    }

    public static EmbeddingVector From(float[] values) => new(values);
    
    public double CosineSimilarity(EmbeddingVector other)
    {
        if (Dimensions != other.Dimensions)
        {
            throw new ArgumentException("Vectors must have same dimensions");
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < Dimensions; i++)
        {
            dotProduct += Values[i] * other.Values[i];
            magnitudeA += Values[i] * Values[i];
            magnitudeB += other.Values[i] * other.Values[i];
        }

        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
