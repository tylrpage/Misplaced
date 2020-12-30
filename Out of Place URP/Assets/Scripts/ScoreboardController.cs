using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ScoreboardController : MonoBehaviour
{
    [SerializeField] private GameObject EntryPrefab;
    [SerializeField] private Transform EntryScoreboardParent;
    private Dictionary<ushort, Tuple<string, short>> _scores;
    private List<GameObject> _entryObjects = new List<GameObject>();

    public void ResetScores()
    {
        _scores.Clear();
        foreach (var entryObject in _entryObjects)
        {
            Destroy(entryObject);
        }
    }

    public void UpdateEntry(ushort id, string name, short score)
    {
        _scores[id] = new Tuple<string, short>(name, score);
    }

    public void DrawBoard()
    {
        var scoresList = _scores.Values.ToList();
        scoresList.Sort((x, y) =>
        {
            return x.Item2.CompareTo(y.Item2);
        });

        int count = 0;
        GameObject newEntry;
        foreach (var scoreItem in scoresList)
        {
            newEntry = Instantiate(EntryPrefab);
            _entryObjects.Add(newEntry);
            newEntry.transform.parent = EntryScoreboardParent;
            newEntry.transform.localPosition = Vector3.down * count;

            newEntry.GetComponent<TextMeshPro>().text = scoreItem.Item1 + ": " + scoreItem.Item2;
        }
    }
}
