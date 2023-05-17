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
    public GameObject pillarPrefab;

    private List<ScoredSegment> _results;
    private int _nResults;

    private List<ScoredSegment> _queryResults;

    //for upper reference Point View
    private int referencePoint = -1;
    private MediaItemDisplay referencePointDisplay = null;
    private GameObject referencePointButton = null;

    private int columns = 4;
    private int rowsVisible = 4;
    private int rows;

    private List<MediaItemDisplay> _mediaDisplays = new ();
    private List<GameObject> _buttons = new ();
    private List<Transform> _pillars = new();

    private List<int> _resultIndex = new ();

    private Transform controlPanelTransform = null;
    private Transform resultParentTransform = null;

    private Scrollbar ReferencePointScrollbar;
    private float scrollbarStepSize;
    private float prevScrollbarValue;
    private Queue<float> _updateQueue = new ();
    private float moveScrollbarValue = 0.0f;

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
      //_nResults = 50;

      //set initial position
      gameObject.transform.position = new Vector3(0, 0.5f, 1.3f);

      if (_nResults == 0)
      {
        return;
      }

      rows = (int)Math.Ceiling((double)_nResults / (double)columns);

      //Setup Scrollbar
      ReferencePointScrollbar = gameObject.GetComponentInChildren<Scrollbar>();
      ReferencePointScrollbar.numberOfSteps = rows - rowsVisible + 1;
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
        _mediaDisplays.Add(null);
        _buttons.Add(null);
        _resultIndex.Add(-1);

        Transform pillar = Instantiate(pillarPrefab, resultParentTransform).transform.Find("Canvas").transform.Find("ReferencePointPillar");
        
        var (position, delta) = GetPillarPosScale(i, 0);
        pillar.localPosition = position;
        var rectTransform = pillar.GetComponent<RectTransform>();
        rectTransform.sizeDelta = delta;
        rectTransform.localScale = new Vector3(1, 1, 1);
        _pillars.Add(pillar);

      }

      for (int i = 0; i < 16; i++)
      {
        CreateResultObject(i);
      }

      updateAllObjectPos();
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

      moveScrollbarValue += Time.deltaTime * moveScrollbar.ReadValue<Vector2>().x;

      if (0.5 <= Math.Abs(moveScrollbarValue))
      {
        var value = ReferencePointScrollbar.value;

        if (moveScrollbarValue < 0)
        {
          ReferencePointScrollbar.value = Math.Max(0, (value - scrollbarStepSize));
        }
        else
        {
          ReferencePointScrollbar.value = Math.Min(1, (value + scrollbarStepSize));
        }

        moveScrollbarValue = 0;
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
    private void updateResultPosition(float val)
    {

      //No position update needed if scroll val is the same as the prevScrollValue
      if (val == prevScrollbarValue)
      {
        return;
      }

      var visibleWindow = columns * rowsVisible;
      var rowShift = (int)Math.Round(val * (rows - rowsVisible));
      //Debug.Log(val + " " + rowShift);

      var startIndex = columns * rowShift;
      var endIndex = startIndex + visibleWindow;

      //Debug.Log(startIndex + "-" + endIndex + ", " + startLoadIndex + "-" + endLoadIndex + " " + startUnloadIndex + "-" + endUnloadIndex);

      //Destroy objects with index outside of visible window.
      for (int i = 0; i < visibleWindow; i++)
      {
        if ((_resultIndex[i] < startIndex || endIndex <= _resultIndex[i]) && _resultIndex[i] != -1)
        {
          DestroyResultObject(i);
        }
      }

      var removedAmount = _resultIndex.Count(x => x == -1);
      _resultIndex.RemoveAll(x => x == -1);
      _mediaDisplays.RemoveAll(x => x == null);
      _buttons.RemoveAll(x => x == null);

      //Readd Elements infront or after shown elements. This shifts the already loaded elements. 
      if (val < prevScrollbarValue)
      {

        for (int i = 0; i < removedAmount; i++)
        {
          _mediaDisplays.Insert(0, null);
          _buttons.Insert(0, null);
          _resultIndex.Insert(0, -1);
        }

      }
      else if (val > prevScrollbarValue)
      {
        for (int i = 0; i < removedAmount; i++)
        {
          _mediaDisplays.Add(null);
          _buttons.Add(null);
          _resultIndex.Add(-1);
        }
      }

      //reposition in 3d Space and add new Objects if needed.
      for (int i = 0; i < visibleWindow; i++)
      {
        if (_mediaDisplays[i] != null)
        {
          updateResultObjectPos(i);
        }
        else if (startIndex + i < _nResults)
        {
          CreateResultObject(i, rowShift);
        }
        else
        {
          _mediaDisplays[i] = null;
          _buttons[i] = null;
          _resultIndex[i] = -1;
        }
      }

      updateAllObjectPos();

      //Debug.Log("Loaded Elements: " + _mediaDisplays.Count(s => s != null));
      gameObject.transform.position = new Vector3(0, 0.5f, 1.3f);
      prevScrollbarValue = val;
    }

    private void CreateResultObject(int index, int rowShift = 0)
    {

      var resultIndex = columns * rowShift + index;

      var (position, positionButton) = GetResultLocalPos(index);

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
      button.onClick.AddListener(() => selectItemAsReferencePoint(resultIndex));

      // Add to media displays list
      _mediaDisplays[index] = itemDisplay;
      _buttons[index] = buttonGameObject;

      _resultIndex[index] = resultIndex;

      itemDisplay.Initialize(_results[resultIndex]);

      itemDisplay.gameObject.SetActive(true);
    }

    private void DestroyResultObject(int index)
    {
      _mediaDisplays[index].gameObject.Destroy(true);
      _mediaDisplays[index] = null;
      _buttons[index].gameObject.Destroy(true);
      _buttons[index] = null;

      _resultIndex[index] = -1;
    }

    private void updateResultObjectPos(int index)
    {

      float score = 0;

      try {

        if (_queryResults.Count > 0)
        {
          score = (float)_queryResults.Where(x => x.segment.Id == _results[_resultIndex[index]].segment.Id).ToArray()[0].score;
          _pillars[index].GetComponent<Image>().color = new Color(0, 0, 255, 0.5f);
        }

        
      } catch (Exception e)
      {
        Debug.Log(e);
        Debug.Log("Error no segment found for Id: " + _results[_resultIndex[index]].segment.Id + " with index: " + index);
        _pillars[index].GetComponent<Image>().color = new Color(255, 0, 0, 0.5f);
      }

      var (position, positionButton) = GetResultLocalPos(index, score);
      _mediaDisplays[index].transform.localPosition = position;
      _buttons[index].transform.localPosition = positionButton;

      var (pillarPos, delta) = GetPillarPosScale(index, score);
      _pillars[index].transform.localPosition = pillarPos;
      _pillars[index].GetComponent<RectTransform>().sizeDelta = delta;
      _pillars[index].GetComponentInChildren<TextMeshProUGUI>().text = score.ToString("####0.##");
      _pillars[index].GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    private void updateAllObjectPos()
    {
      for(int i = 0; i < columns * rowsVisible; i++)
      {
        updateResultObjectPos(i);
      }
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
    private (Vector3 position, Vector3 positionButton) GetResultLocalPos(int index, float score = 0.0f)
    {

      var posX = -975;
      var posY = 600;

      var column = index % columns;
      var row = index / columns;

      var position = new Vector3(column * 650 + posX, -row * 400 + posY, -200 - 600 * score - 20);

      var positionButton = new Vector3(position.x + 125, position.y -30, position.z - 200);

      return (position, positionButton);
    }

    private (Vector3 position, Vector2 delta) GetPillarPosScale(int index, float score = 0.0f)
    {
      var posX = -975;
      var posY = 600;

      var column = index % columns;
      var row = index / columns;

      var position = new Vector3(column * 650 + posX, -row * 400 + posY, - 300 * score);

      var delta = new Vector2(200, 600 * score);

      return (position, delta);
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
      updateAllObjectPos();
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

      //Debug.Log(_queryResults.Count);
      updateAllObjectPos();
    }

  }
}