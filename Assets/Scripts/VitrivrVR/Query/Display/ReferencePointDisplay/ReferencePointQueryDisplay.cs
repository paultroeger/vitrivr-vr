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

    //for upper reference Point View
    private int referencePoint = -1;
    private MediaItemDisplay referencePointDisplay = null;
    private GameObject referencePointButton = null;

    private int columns = 4;
    private int rowsVisible = 4;

    private List<MediaItemDisplay> _mediaDisplays = new ();
    private List<GameObject> _buttons = new ();

    private List<int> _resultIndex = new ();

    private Transform referencePointPanelTransform = null;

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

      for (int i = 0; i < columns * rowsVisible; i++)
      {
        _mediaDisplays.Add(null);
        _buttons.Add(null);
        _resultIndex.Add(-1);
      }

      //get Text
      TextMeshProUGUI titleText = gameObject.GetComponentInChildren<TextMeshProUGUI>();

      titleText.text = "Reference Point Display (Displaying: " + _nResults + " Results)";

      Transform resultPanelTransform = gameObject.transform.Find("Canvas").transform.Find("ResultPanel");

      referencePointPanelTransform = gameObject.transform.Find("Canvas").transform.Find("ReferencePointPanel");

      for (int i = 0; i < 16; i++)
      {
        CreateResultObject(resultPanelTransform.gameObject, i);
      }

      
    }

    private void CreateResultObject(GameObject panel, int index, int rowShift = 0)
    {

      var resultIndex = columns * rowShift + index;

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

      button.GetComponentInChildren<TextMeshProUGUI>().text = "Set as Reference Point";
      button.onClick.AddListener(() => selectItemAsReferencePoint(index));

      // Add to media displays list
      _mediaDisplays[index] = itemDisplay;
      _buttons[index] = buttonGameObject;

      _resultIndex[index] = resultIndex;

      itemDisplay.Initialize(_results[resultIndex]);

      itemDisplay.gameObject.SetActive(true);
    }

    private void CreateReferencePointObject(GameObject panel, int resultIndex)
    {
      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);

      var transform2 = itemDisplay.transform;
      transform2.SetParent(panel.transform);
      transform2.localPosition = new Vector3(0, 0, -300);

      transform2.localScale = new Vector3(0.4f, 0.4f, 0.1f);

      //Button
      GameObject buttonGameObject = Instantiate(buttonPrefab, panel.transform);

      buttonGameObject.transform.localPosition = new Vector3(0, 0, -300 + 175);
      buttonGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
      buttonGameObject.transform.GetChild(0).transform.localScale = new Vector3(1f, 1f, 1f);

      Button button = buttonGameObject.GetComponentInChildren<Button>();

      button.transform.localScale = new Vector3(2, 2, 2);
      button.transform.Rotate(new Vector3(-90, 0, 0));

      button.GetComponentInChildren<TextMeshProUGUI>().text = "Remove Reference Point";
      button.onClick.AddListener(() => removeReferencePoint());

      // Add to media displays list
      referencePointDisplay = itemDisplay;
      referencePointButton = buttonGameObject;

      referencePoint = resultIndex;

      itemDisplay.Initialize(_results[resultIndex]);

      itemDisplay.gameObject.SetActive(true);
    }

    private void DestroyReferenceObject()
    {
      if(referencePoint != -1)
      {
        referencePointDisplay.gameObject.Destroy();
        referencePointDisplay = null;
        referencePointButton.Destroy();
        referencePointButton = null;
        referencePoint = -1;
      }
    }

    //Calculates position of mediaItem and metaText in grid based on index
    private (Vector3 position, Vector3 positionButton) GetResultLocalPos(int index, float score = 0.0f)
    {

      var posX = -750;
      var posY = 630;

      var column = index % columns;
      var row = index / columns;

      var position = new Vector3(column * 500 + posX, -row * 420 + posY, -200 - 1000 * score);

      var positionButton = new Vector3(position.x, position.y, position.z + 175);

      return (position, positionButton);
    }

    private void selectItemAsReferencePoint(int index)
    {
      Debug.Log("Pressed the Button! " + index);

      if(referencePoint != -1)
      {
        DestroyReferenceObject();
      }

      referencePoint = index;

      fetchSimilarityScores(index);

      CreateReferencePointObject(referencePointPanelTransform.gameObject, _resultIndex[index]);

      //updateReferencePoint();

    }

    private void removeReferencePoint()
    {
      DestroyReferenceObject();
    }

    private async void fetchSimilarityScores(int index)
    {

      List<String> segmentIds = _results.Select(x => x.segment.Id).ToList();


      var itemId = _results[index].segment.Id;

      List<QueryTerm> terms = new List<QueryTerm>();
      terms.Add(new QueryTerm(QueryTerm.TypeEnum.ID, itemId, new List<String> { "visualtextcoembedding" }));
      QueryConfig config = new QueryConfig(relevantSegmentIds: segmentIds);

      SimilarityQuery similarityQuery = new SimilarityQuery(terms, config);
      QueryResponse queryResponse = await QueryController.Instance.CurrentClient.ExecuteQuery(similarityQuery, segmentIds.Count);

      var result = queryResponse.GetMeanFusionResults();

     

    }

  }
}