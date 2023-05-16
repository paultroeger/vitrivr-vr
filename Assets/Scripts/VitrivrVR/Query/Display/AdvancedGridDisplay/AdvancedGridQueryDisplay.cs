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
  /// Displays queries in an advanced grid.
  /// </summary>
  public class AdvancedGridQueryDisplay : QueryDisplay
  {

    public override int NumberOfResults => _nResults;
    public MediaItemDisplay mediaItemDisplay;
    public GameObject textMeshProPrefab;

    public InputAction moveScrollbar;

    private List<ScoredSegment> _results;
    private int _nResults;
    private readonly List<MediaItemDisplay> _mediaDisplays = new();
    private readonly List<GameObject> _metaTexts = new();
    private readonly List<int> _resultIndex = new();
    private Transform gridPanelTransform;

    private readonly Queue<float> _updateQueue = new();

    private int columns = 6;
    private int rows;
    private int rowsVisible = 4;
    private float prevScrollbarValue = 0.0f;

    private float moveScrollbarValue = 0.0f;
    private float scrollbarStepSize = 0.0f;

    private Scrollbar AdvancedGridScrollbar;

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
      

      
      rows = (int)Math.Ceiling((double)_nResults / (double)columns);
     

      for (int i = 0; i < columns * rowsVisible; i++)
      {
        _mediaDisplays.Add(null);
        _metaTexts.Add(null);
        _resultIndex.Add(-1);
      }

      //Debug.Log("Rows: " + rows);

      //set initial position
      gameObject.transform.position = new Vector3(0, 1.5f, 1.3f);

      //Setup Scrollbar
      AdvancedGridScrollbar = gameObject.GetComponentInChildren<Scrollbar>();
      AdvancedGridScrollbar.numberOfSteps = rows - rowsVisible + 1;
      AdvancedGridScrollbar.size = rowsVisible / rows;
      AdvancedGridScrollbar.onValueChanged.AddListener((float val) => _updateQueue.Enqueue(val));
      scrollbarStepSize = 1.0f / ((float) (rows - rowsVisible));
      //Debug.Log("STEP SIZE:" + scrollbarStepSize + ":" + rows);
      //Debug.Log(AdvancedGridScrollbar.size);

      //get Text
      TextMeshProUGUI titleText = gameObject.GetComponentInChildren<TextMeshProUGUI>();

      titleText.text = "Advanced Grid (Displaying: " + _nResults + " Results)";

      //get Panel
      gridPanelTransform = gameObject.transform.Find("Canvas").transform.Find("Panel");
     
      //Setup first page
      if (gridPanelTransform != null)
      {

        for (int i = 0; i < rowsVisible * columns; i++)
        {
          CreateResultObject(gridPanelTransform.gameObject, i);
        }
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

    //Update the Results Shown based on scrollbar value.
    private void updateResultPosition(float val)
    {

      //No position update needed if scroll val is the same as the prevScrollValue
      if (val == prevScrollbarValue)
      {
        return;
      }

      var visibleWindow = columns * rowsVisible;
      var rowShift = (int) Math.Round(val * (rows - rowsVisible));
      //Debug.Log(val + " " + rowShift);

      var startIndex = columns * rowShift;
      var endIndex = startIndex + visibleWindow;

      //Debug.Log(startIndex + "-" + endIndex + ", " + startLoadIndex + "-" + endLoadIndex + " " + startUnloadIndex + "-" + endUnloadIndex);

      //Destroy objects with index outside of visible window.
      for (int i = 0; i < visibleWindow; i++)
      {
        if ((_resultIndex[i] < startIndex || endIndex <= _resultIndex[i]) && _resultIndex[i] != -1)
        {
          destroyResultObject(i);
        }
      }

      var removedAmount = _resultIndex.Count(x => x == -1);
      _resultIndex.RemoveAll(x => x == -1);
      _mediaDisplays.RemoveAll(x => x == null);
      _metaTexts.RemoveAll(x => x == null);

      //Readd Elements infront or after shown elements. This shifts the already loaded elements. 
      if (val < prevScrollbarValue)
      {

        for (int i = 0; i < removedAmount; i++) {
          _mediaDisplays.Insert(0, null);
          _metaTexts.Insert(0, null);
          _resultIndex.Insert(0, -1);
        }
        
      } else if (val > prevScrollbarValue)
      {
        for (int i = 0; i < removedAmount; i++)
        {
          _mediaDisplays.Add(null);
          _metaTexts.Add(null);
          _resultIndex.Add(-1);
        }
      }

      //reposition in 3d Space and add new Objects if needed.
      for (int i = 0; i < visibleWindow; i++) 
      {
        if (_mediaDisplays[i] != null)
        {
          var (newPos, newTextPos) = GetResultLocalPos(i);
          //Debug.Log(newPos);
          var itemDisplayTransform = _mediaDisplays[i].transform;
          itemDisplayTransform.localPosition = newPos;
          var metaTextTransform = _metaTexts[i].transform;
          metaTextTransform.localPosition = newTextPos;
        } else if(startIndex + i < _nResults) {
          CreateResultObject(gridPanelTransform.gameObject, i, rowShift);
        } else
        {
          _mediaDisplays[i] = null;
          _metaTexts[i] = null;
          _resultIndex[i] = -1;
        }
      } 

      //Debug.Log("Loaded Elements: " + _mediaDisplays.Count(s => s != null));
      gameObject.transform.position = new Vector3(0, 1.5f, 1.3f);
      prevScrollbarValue = val;
    }

    //Updates Objects and takes user input for scrolling
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
        var value = AdvancedGridScrollbar.value;

        if(moveScrollbarValue < 0)
        {
          AdvancedGridScrollbar.value = Math.Max(0, (value - scrollbarStepSize));
        } else
        {
          AdvancedGridScrollbar.value = Math.Min(1, (value + scrollbarStepSize));
        }

        moveScrollbarValue = 0;
      }

      //Debug.Log(moveScrollbar.ReadValue<Vector2>());
    }

    private void CreateResultObject(GameObject panel, int index, int rowShift = 0)
    {

      var resultIndex = columns * rowShift + index;

      // Determine position
      var (position, positionText) = GetResultLocalPos(index);

      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);
    
      var transform2 = itemDisplay.transform;
      transform2.SetParent(panel.transform);
      transform2.localPosition = position;

      transform2.localScale = new Vector3(0.4f, 0.4f, 0.1f);

      //Metadata Text
      GameObject metaText = Instantiate(textMeshProPrefab, panel.transform);

      metaText.transform.localPosition = positionText;
      metaText.transform.localScale = new Vector3(1f, 1f, 1f);

      TextMeshProUGUI metaTextUGUI = metaText.GetComponentInChildren<TextMeshProUGUI>();
      //fetch and forget call of async function
      createMetaDataToDisplay(metaTextUGUI, resultIndex, _results[resultIndex]);
      metaTextUGUI.fontSize = 30;
      metaTextUGUI.alignment = TextAlignmentOptions.Center;

      _metaTexts[index] = metaText;

      // Add to media displays list
      _mediaDisplays[index] = itemDisplay;

      _resultIndex[index] = resultIndex;

      itemDisplay.Initialize(_results[resultIndex]);

      itemDisplay.gameObject.SetActive(true);
    }

    //Fetches and sets MetaData Text. Is async since we have to await certain values. 
    private async void createMetaDataToDisplay(TextMeshProUGUI metaTextUGUI, int index, ScoredSegment result)
    {
      var text = "Index " + index;
      text += ", Score: " + result.score.ToString("##0.###");

      var startAbsolute = await result.segment.GetAbsoluteStart();
      var endAbsolute = await result.segment.GetAbsoluteEnd();

      text += "\n" + startAbsolute.ToString("####0.##") + "s - " + endAbsolute.ToString("####0.##");
      text += "s (" + (endAbsolute - startAbsolute).ToString("####0.##") + "s)";
      metaTextUGUI.text = text;
    }

    //destroys unused object
    private void destroyResultObject(int index)
    { 

      _mediaDisplays[index].gameObject.Destroy(true);
      _mediaDisplays[index] = null;
      _metaTexts[index].gameObject.Destroy(true);
      _metaTexts[index] = null;
      
      _resultIndex[index] = -1;
    }

    //Calculates position of mediaItem and metaText in grid based on index
    private (Vector3 position, Vector3 positionText) GetResultLocalPos(int index)
    {

      var posX = -1250;
      var posY = 600;

      var column = index % columns;
      var row = index / columns;
     
      var position = new Vector3(column * 500 + posX, -row  * 420 + posY, -0.06f);

      var positionText = new Vector3(column * 500 + posX, -row * 420 - 200 + posY, -0.04f);

      return (position, positionText);
    }
  }
}