using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dna
{

    #region Properties
    public List<int> Genes { get; private set; } = new List<int>();
    #endregion

    private int dnaLength = 0;
    private int maxValues = 0;

    #region Constructors
    public Dna (int length, int values)
    {
        dnaLength = length;
        maxValues = values;
        SetRandom();
    }
    #endregion

    #region Public Methods

    public void SetRandom ()
    {
        Genes.Clear();
        for (var i = 0; i < dnaLength; i++)
        {
            Genes.Add(Random.Range(-maxValues, maxValues));
        }
    }

    public void Combine (Dna parent1, Dna parent2)
    {
        for (var i = 0; i < dnaLength; i++)
        {
            // For each gene:
            // Use either parent1 or parent2's gene (50-50 chance).
            Genes[i] = Random.Range(0, 10) < 5 ? parent1.Genes[i] : parent2.Genes[i];
        }
    }

    public void Mutate ()
    {
        Genes[Random.Range(0, dnaLength)] = Random.Range(-maxValues, maxValues);
    }
    #endregion
}
