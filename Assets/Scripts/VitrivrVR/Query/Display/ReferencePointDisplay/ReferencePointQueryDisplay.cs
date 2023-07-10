using Org.Vitrivr.CineastApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vitrivr.UnityInterface.CineastApi.Model.Data;
using VitrivrVR.Media.Display;
using VitrivrVR.Notification;

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
    public GameObject pillarPrefab;

    private List<ScoredSegment> _results;
    private int _nResults;

    private List<ScoredSegment> _queryResults;

    //for upper reference Point View
    private int referencePoint = -1;
    private MediaItemDisplay referencePointDisplay = null;
    private GameObject referencePointButton = null;

    private int columns = 4;
    private int rowsVisible = 5;
    private int rows;

    private MediaItemDisplay[] _mediaDisplays;
    private GameObject[] _buttons;
    private Transform[] _pillars;

    private Transform controlPanelTransform = null;
    private Transform resultParentTransform = null;

    private Scrollbar ReferencePointScrollbar;
    private float scrollbarStepSize;
    private float prevScrollbarValue;
    private Queue<float> _updateQueue = new ();
    //private float moveScrollbarValue = 0.0f;

    public InputAction moveScrollbar;

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
      //_nResults = 200;

      _mediaDisplays = new MediaItemDisplay[_nResults];
      _buttons = new GameObject[_nResults];
      _pillars = new Transform[columns * rowsVisible];


      //set initial position
      gameObject.transform.position = new Vector3(0, 0.5f, 1.3f);

      if (_nResults == 0)
      {
        return;
      }

      rows = (int)Math.Ceiling((double)_nResults / (double)columns);

      //Setup Scrollbar
      ReferencePointScrollbar = gameObject.GetComponentInChildren<Scrollbar>();
      //ReferencePointScrollbar.numberOfSteps = rows - rowsVisible + 1;
      ReferencePointScrollbar.size = rowsVisible / rows;
      ReferencePointScrollbar.onValueChanged.AddListener((float val) => _updateQueue.Enqueue(val));
      scrollbarStepSize = 1.0f / ((float)(rows - rowsVisible));

      //get Text
      TextMeshProUGUI titleText = gameObject.GetComponentInChildren<TextMeshProUGUI>();

      titleText.text = "Reference Point Display (Displaying: " + _nResults + " Results)";

      resultParentTransform = transform.Find("ResultParent");
      controlPanelTransform = gameObject.transform.Find("Canvas").transform.Find("ControlPanel");

      for (int i = 0; i < columns * rowsVisible; i++)
      {
        Transform pillar = Instantiate(pillarPrefab, resultParentTransform).transform.Find("Canvas").transform.Find("ReferencePointPillar");
        
        var (position, delta) = GetPillarPosScale(i, 0);
        pillar.localPosition = position;
        var rectTransform = pillar.GetComponent<RectTransform>();
        rectTransform.sizeDelta = delta;
        rectTransform.localScale = new Vector3(1, 1, 1);
        _pillars[i] = pillar;
        pillar.GetComponentInChildren<TextMeshProUGUI>().text = "";

      }

      for (int i = 0; i < 16; i++)
      {
        CreateResultObject(i);
        updateResultObjectPos(i);
      }
    }

    private void Update()
    {
      if (_updateQueue.Count > 0)
      {
        var updatePos = (_updateQueue.Dequeue());
        if (_updateQueue.Count > 0)
        {
          updatePos = _updateQueue.Peek();
          _updateQueue.Clear();
        }

        updateResultPosition(updatePos);
      }

      var scrollInput = moveScrollbar.ReadValue<Vector2>().x;
      if (scrollInput < 0)
      {
        ReferencePointScrollbar.value = Math.Max(0, ReferencePointScrollbar.value + 6 * scrollbarStepSize * Time.deltaTime * scrollInput);
      }
      else
      {
        ReferencePointScrollbar.value = Math.Min(1, ReferencePointScrollbar.value + 6 * scrollbarStepSize * Time.deltaTime * scrollInput);
      }

    }

    private void OnEnable()
    {
      moveScrollbar.Enable();
    }

    private void OnDisable()
    {
      moveScrollbar.Disable();
    }

    private void OnDestroy()
    {
      controlPanelTransform.Destroy();
    }

    //Update the Results Shown based on scrollbar value.
    private void updateResultPosition(float val, bool forceUpdate = false)
    {

      //No position update needed if scroll val is the same as the prevScrollValue
      if (val == prevScrollbarValue && !forceUpdate)
      {
        return;
      }

      var visibleWindow = columns * rowsVisible;
      var rowShift = val * (rows - rowsVisible);
      //Debug.Log(val + " " + rowShift);

      var startIndex = columns * (int)Math.Round(rowShift);
      var endIndex = startIndex + visibleWindow;

      //Debug.Log(startIndex + "-" + endIndex + ", " + startLoadIndex + "-" + endLoadIndex + " " + startUnloadIndex + "-" + endUnloadIndex);
      //Destroy objects with index outside of visible window.
      _buttons.ToList().GetRange(0, Math.Min(startIndex, _buttons.Length))
        .Where(x => x != null).ToList().ForEach(x => x.SetActive(false));

      _buttons.ToList().GetRange(Math.Min(endIndex, _buttons.Length), Math.Max(0, _buttons.Length - endIndex))
        .Where(x => x != null).ToList().ForEach(x => x.SetActive(false));

      _mediaDisplays.ToList().GetRange(0, Math.Min(startIndex, _mediaDisplays.Length))
        .Where(x => x != null).ToList().ForEach(x => x.gameObject.SetActive(false));

      _mediaDisplays.ToList().GetRange(Math.Min(endIndex, _mediaDisplays.Length), Math.Max(0, _mediaDisplays.Length - endIndex))
        .Where(x => x != null).ToList().ForEach(x => x.gameObject.SetActive(false));

      //Readd Elements infront or after shown elements. This shifts the already loaded elements. 
      for (int i = startIndex; i < endIndex; i++)
      {
        if (i < _mediaDisplays.Length && _mediaDisplays[i] != null)
        {
          _mediaDisplays[i].gameObject.SetActive(true);
          _buttons[i].SetActive(true);
        }
        else if (i < _nResults)
        {
          //Debug.Log(i);
          CreateResultObject(i, rowShift);
        }
      }

      //reposition in 3d Space and add new Objects if needed.
      for (int i = startIndex; i < endIndex; i++)
      {
        if (i < _nResults && _mediaDisplays[i] != null)
        {
          updateResultObjectPos(i, rowShift);
        }
      }

      ;

      //Debug.Log("Loaded Elements: " + _mediaDisplays.Count(s => s != null));
      gameObject.transform.position = new Vector3(0, 0.5f, 1.3f);
      prevScrollbarValue = val;
    }

    private void CreateResultObject(int index, float rowShift = 0)
    {

      var (position, positionButton, alpha) = GetResultLocalPos(index);

      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, resultParentTransform);

      var transform2 = itemDisplay.transform;
      transform2.SetParent(resultParentTransform);
      transform2.localPosition = position;

      transform2.localScale = new Vector3(0.4f, 0.4f, 0.1f);

      //Button
      GameObject buttonGameObject = Instantiate(buttonPrefab, resultParentTransform);

      buttonGameObject.transform.localPosition = positionButton;
      buttonGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
      buttonGameObject.transform.GetChild(0).transform.localScale = new Vector3(1f, 1f, 1f);
      
      Button button = buttonGameObject.GetComponentInChildren<Button>();

      button.transform.localScale = new Vector3(2, 2, 2);
      button.transform.Rotate(new Vector3(-90, 0, 0));

      button.GetComponentInChildren<TextMeshProUGUI>().text = "+";
      button.onClick.AddListener(() => selectItemAsReferencePoint(index));

      // Add to media displays list
      _mediaDisplays[index] = itemDisplay;
      _buttons[index] = buttonGameObject;

      itemDisplay.Initialize(_results[index]);

      itemDisplay.gameObject.SetActive(true);

      SetAlpha(index, alpha);
    }

    private void DeactivateResultObject(int index)
    {
      _mediaDisplays[index].gameObject.SetActive(false);
      _buttons[index].gameObject.SetActive(false);
    }

    private void updateResultObjectPos(int index, float rowShift = 0)
    {

      float score = 0;

      var pillarIndex = index % (columns * rowsVisible);

      try {

        if (_queryResults.Count > 0)
        {
          score = (float)_queryResults.Where(x => x.segment.Id == _results[index].segment.Id).ToArray()[0].score;
          _pillars[pillarIndex].GetComponent<Image>().color = new Color(0, 0, 255, 0.5f);
        }

        
      } catch //(Exception e)
      {
        //Debug.Log(e);
        //Debug.Log("Error no segment found for Id: " + _results[index].segment.Id + " with index: " + index);
        
        _pillars[pillarIndex].GetComponent<Image>().color = new Color(255, 0, 0, 0.5f);
      }

      var (position, positionButton, alpha) = GetResultLocalPos(index, score, rowShift);
      _mediaDisplays[index].transform.localPosition = position;
      _buttons[index].transform.localPosition = positionButton;

      var (pillarPos, delta) = GetPillarPosScale(index, score, rowShift);
      _pillars[pillarIndex].transform.localPosition = pillarPos;
      _pillars[pillarIndex].GetComponent<RectTransform>().sizeDelta = delta;
      _pillars[pillarIndex].GetComponentInChildren<TextMeshProUGUI>().text = score.ToString("####0.##");
      _pillars[pillarIndex].GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

      SetAlpha(index, alpha);

    }

  

    private void CreateReferencePointObject(GameObject panel, int resultIndex)
    {
      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);

      var transform2 = itemDisplay.transform;
      transform2.SetParent(panel.transform);
      transform2.localPosition = new Vector3(660, -10, -20);

      transform2.localScale = new Vector3(0.4f, 0.4f, 0.1f);

      //Button
      GameObject buttonGameObject = Instantiate(buttonPrefab, panel.transform);

      buttonGameObject.transform.localPosition = new Vector3(660 + 140, -10 + 190, -20 - 20);
      buttonGameObject.transform.localScale = new Vector3(1f, 1f, 1f);
      buttonGameObject.transform.GetChild(0).transform.localScale = new Vector3(1f, 1f, 1f);

      Button button = buttonGameObject.GetComponentInChildren<Button>();

      button.transform.localScale = new Vector3(2, 2, 2);
      button.transform.Rotate(new Vector3(0, 0, 0));

      button.GetComponentInChildren<TextMeshProUGUI>().text = "-";
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
    private (Vector3 position, Vector3 positionButton, float alpha) GetResultLocalPos(int index, float score = 0.0f, float rowShift = 0)
    {

      var posX = -975;
      var posY = 600;

      var column = index % columns;
      var row = index / columns;

      var position = new Vector3(column * 650 + posX, -(row - rowShift) * 400 + posY, -200 - 600 * score - 20);

      var positionButton = new Vector3(position.x + 125, position.y -30, position.z - 200);

      float alpha = 1;
      var lowerBoundry = -(rowsVisible - 1) * 400 + posY + 200;

      if (position.y > posY)
      {
        alpha = 1 - (position.y - posY) / 200;
      }
      else if (position.y < lowerBoundry)
      {
        alpha = Math.Max(0, 1 - (-position.y + lowerBoundry) / 200);
      }

      return (position, positionButton, alpha);
    }

    private (Vector3 position, Vector2 delta) GetPillarPosScale(int index, float score = 0.0f, float rowShift = 0)
    {
      var posX = -975;
      var posY = 600;

      var column = index % columns;
      var row = index / columns;

      var position = new Vector3(column * 650 + posX, -(row - rowShift) * 400 + posY, - 300 * score);

      var delta = new Vector2(200, 600 * score);

      return (position, delta);
    }

    private void SetAlpha(int index, float alpha)
    {
      var pillarIndex = index % (columns * rowsVisible);
      //set transperency
      var rawImage = _mediaDisplays[index].transform.Find("ImageFrame").Find("RawImage").GetComponent<RawImage>();
      rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, alpha);
      var scoreFrame = _mediaDisplays[index].transform.Find("ImageFrame").Find("ScoreFrame").GetComponent<RawImage>();
      scoreFrame.color = new Color(scoreFrame.color.r, scoreFrame.color.g, scoreFrame.color.b, alpha);

      var buttonImage = _buttons[index].transform.Find("Canvas").Find("Button").GetComponent<Image>();
      buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, alpha);

      var buttonText = _buttons[index].transform.Find("Canvas").Find("Button").GetComponentInChildren<TextMeshProUGUI>();
      buttonText.color = new Color(buttonText.color.r, buttonText.color.g, buttonText.color.b, alpha);

      var pillarImage = _pillars[pillarIndex].GetComponent<Image>();
      pillarImage.color = new Color(pillarImage.color.r, pillarImage.color.g, pillarImage.color.b, alpha);

      var pillarText = _pillars[pillarIndex].GetComponentInChildren<TextMeshProUGUI>();
      pillarText.color = new Color(pillarText.color.r, pillarText.color.g, pillarText.color.b, alpha);
    }

    private void selectItemAsReferencePoint(int resultIndex)
    {
      Debug.Log("Pressed the Button! " + resultIndex);

      if(referencePoint != -1)
      {
        DestroyReferenceObject();
      }

      referencePoint = resultIndex;

      CreateReferencePointObject(controlPanelTransform.gameObject, resultIndex);

      fetchSimilarityScores(resultIndex);
    }

    private void removeReferencePoint()
    {
      _queryResults = null;
      DestroyReferenceObject();
      updateResultPosition(ReferencePointScrollbar.value, true);
    }

    private async void fetchSimilarityScores(int resultIndex)
    {

      List<String> segmentIds = _results.Select(x => x.segment.Id).ToList();

      //Debug.Log(segmentIds.Count);

      //List<String> segmentIds = _resultIndex.Select(x => _results[x].segment.Id).ToList();

      var itemId = _results[resultIndex].segment.Id;

      List<QueryTerm> terms = new List<QueryTerm>();
      terms.Add(new QueryTerm(QueryTerm.TypeEnum.ID, itemId, new List<String> { "visualtextcoembedding" }));
      QueryConfig config = new QueryConfig(relevantSegmentIds: segmentIds, maxResults: segmentIds.Count, resultsPerModule: segmentIds.Count);

      
      SimilarityQuery similarityQuery = new SimilarityQuery(terms, config);
      //Debug.Log(similarityQuery.ToJson());
      QueryResponse queryResponse = await QueryController.Instance.CurrentClient.ExecuteQuery(similarityQuery, segmentIds.Count);

      _queryResults = queryResponse.GetMeanFusionResults();

      updateResultPosition(ReferencePointScrollbar.value, true);
    }

  }
}