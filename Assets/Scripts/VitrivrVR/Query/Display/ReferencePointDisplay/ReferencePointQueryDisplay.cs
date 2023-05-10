using Org.Vitrivr.CineastApi.Model; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vitrivr.UnityInterface.CineastApi.Model.Data;
using VitrivrVR.Config;
using VitrivrVR.Logging;
using VitrivrVR.Media.Display;
using VitrivrVR.Notification;
using static VitrivrVR.Logging.Interaction;

namespace VitrivrVR.Query.Display
{
  /// <summary>
  /// Displays queries with Reference Point.
  /// </summary>
  public class ReferencePointQueryDisplay : QueryDisplay
  {

    public override int NumberOfResults => _nResults;
    public MediaItemDisplay mediaItemDisplay;
    public GameObject buttonPrefab;

    private List<ScoredSegment> _results;
    private int _nResults;


    protected override void Initialize()
    {
      var fusionResults = QueryData.GetMeanFusionResults();
      _results = fusionResults;
      if (_results == null)
      {
        NotificationController.Notify("No results returned from query!");
        _results = new List<ScoredSegment>();
      }

      _nResults = _results.Count;

      //Debug: set different result size.
      //_nResults = 50;

      //set initial position
      gameObject.transform.position = new Vector3(0, 0.5f, 1.3f);


      //get Text
      TextMeshProUGUI titleText = gameObject.GetComponentInChildren<TextMeshProUGUI>();

      titleText.text = "Reference Point Display (Displaying: " + _nResults + " Results)";

      Transform gridPanelTransform = gameObject.transform.Find("Canvas").transform.Find("Panel");

      CreateResultObject(gridPanelTransform.gameObject, 0);
    }

    private void CreateResultObject(GameObject panel, int index, int rowShift = 0)
    {

     
      var (position, positionButton) = GetResultLocalPos(index);

      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);

      var transform2 = itemDisplay.transform;
      transform2.SetParent(panel.transform);
      transform2.localPosition = position;

      transform2.localScale = new Vector3(0.4f, 0.4f, 0.1f);

      //Button
      GameObject buttonGameObject = Instantiate(buttonPrefab, panel.transform);

      buttonGameObject.transform.localPosition = positionButton;
      buttonGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
      buttonGameObject.transform.GetChild(0).transform.localScale = new Vector3(1f, 1f, 1f);
      
      Button button = buttonGameObject.GetComponentInChildren<Button>();

      button.transform.localScale = new Vector3(2, 2, 2);
      button.transform.Rotate(new Vector3(-90, 0, 0));

      button.GetComponentInChildren<TextMeshProUGUI>().text = "Hello!";
      button.onClick.AddListener(() => selectItemAsReferencePoint(index));

      // Add to media displays list
      //_mediaDisplays[index] = itemDisplay;

      //_resultIndex[index] = resultIndex;

      itemDisplay.Initialize(_results[index]);

      itemDisplay.gameObject.SetActive(true);
    }

    //Calculates position of mediaItem and metaText in grid based on index
    private (Vector3 position, Vector3 positionButton) GetResultLocalPos(int index)
    {
      return (new Vector3(0.0f, 0.0f, -500.0f), new Vector3(0.0f, -80.0f, -500.0f + 175));
    }

    private void selectItemAsReferencePoint(int index)
    {
      Debug.Log("Pressed the Button! " + index);

      fetchSimilarityScores(index);
      
    }

    private void fetchSimilarityScores(int index)
    {

      List<String> segmentIds = _results.Select(x => x.segment.Id).ToList();


      var itemId = _results[index].segment.Id;

      List<QueryTerm> terms = new List<QueryTerm>();
      QueryConfig config = new QueryConfig(relevantSegmentIds: segmentIds);

      SimilarityQuery similarityQuery = new SimilarityQuery(terms, config);
      Task<QueryResponse> result = QueryController.Instance.CurrentClient.ExecuteQuery(similarityQuery, 200);
    }

  }
}