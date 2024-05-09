// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Utilities;

public class EmbeddingsCalc
{
    private static double DotProduct(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        return Enumerable.Range(0, a.Count).Sum(i => a[i] * b[i]);
    }

    private static double CalcCosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        try
        {
            var dotProduct = DotProduct(a, b);
            var magnitudeA = Math.Sqrt(DotProduct(a, a));
            var magnitudeB = Math.Sqrt(DotProduct(b, b));
            return dotProduct / (magnitudeA * magnitudeB);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public static List<(double cosineSimilarity, Doc doc)> SortByLanguageThenCosine(List<(double, Doc)> docs, string recommendedLanguage)
    {
        // Convert the recommendedLanguage to lowercase for case-insensitive comparison
        recommendedLanguage = recommendedLanguage.ToLower(CultureInfo.InvariantCulture);

        // Clone the list of docs to avoid modifying the original list
        List<(double cosineSimilarity, Doc doc)> similarDocList = docs.ToList();

        // Sort doc list to rank highest any projects with the same language as recommended
        similarDocList.Sort((a, b) =>
        {
            // Sort by recommended language (case-insensitive) first
            var aHasRecommendedLang = a.doc.Language != null ? a.doc.Language.Equals(recommendedLanguage, StringComparison.OrdinalIgnoreCase) : false;
            var bHasRecommendedLang = b.doc.Language != null ? b.doc.Language.Equals(recommendedLanguage, StringComparison.OrdinalIgnoreCase) : false;

            if (aHasRecommendedLang && !bHasRecommendedLang)
            {
                return -1;
            }
            else if (!aHasRecommendedLang && bHasRecommendedLang)
            {
                return 1;
            }

            // If recommended languages are the same or both are different from the recommended language,
            // then sort by cosine similarity in descending order
            return b.cosineSimilarity.CompareTo(a.cosineSimilarity);
        });

        return similarDocList;
    }

    public static List<(double cosineSimilarity, Doc doc)> GetCosineSimilarityDocs(IReadOnlyList<float> questionEmbedding, IReadOnlyList<Doc> docs)
    {
        // For each doc in docs, calculate the cosine similarity between the question embedding and the doc embedding
        // Sort the docs by the cosine similarity value
        var cosineSimilarityDocs = new List<(double cosineSimilarity, Doc doc)>();

        for (var i = 0; i < docs.Count; i++)
        {
            var doc = docs[i] ?? throw new ArgumentOutOfRangeException($"Document {i} is not expected to not be null");
            var embedding = doc.Embedding ?? throw new InvalidOperationException($"Document {i} does not have a valid embedding");
            var cosineSimilarity = CalcCosineSimilarity(questionEmbedding, embedding);
            cosineSimilarityDocs.Add((cosineSimilarity, doc));
        }

        cosineSimilarityDocs.Sort((a, b) => b.cosineSimilarity.CompareTo(a.cosineSimilarity));

        return cosineSimilarityDocs;
    }
}
