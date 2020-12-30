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
    private Dictionary<ushort, Tuple<string, short>> _scores = new Dictionary<ushort, Tuple<string, short>>();
    private List<GameObject> _entryObjects = new List<GameObject>();

    public void ResetScores()
    {
        _scores.Clear();
    }

    public void UpdateEntry(ushort id, string name, short score)
    {
        _scores[id] = new Tuple<string, short>(name, score);
    }

    public void RemoveEntry(ushort id)
    {
        _scores.Remove(id);
    }

    public void DrawBoard()
    {
        // Delete old entry objects
        foreach (var entryObject in _entryObjects)
        {
            Destroy(entryObject);
        }
        _entryObjects.Clear();
        
        var scoresList = _scores.Values.ToList();
        scoresList.Sort((x, y) =>
        {
            return y.Item2.CompareTo(x.Item2);
        });

        int count = 0;
        GameObject newEntry;
        foreach (var scoreItem in scoresList)
        {
            newEntry = Instantiate(EntryPrefab);
            _entryObjects.Add(newEntry);
            newEntry.transform.SetParent(EntryScoreboardParent, false);
            newEntry.transform.localPosition = Vector3.down * count;

            newEntry.GetComponent<TextMeshPro>().text = scoreItem.Item1 + ": " + scoreItem.Item2;

            count++;
        }
    }
}
