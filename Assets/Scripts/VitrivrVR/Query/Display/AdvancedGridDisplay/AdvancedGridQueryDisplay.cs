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
    private MediaItemDisplay[] _mediaDisplays;
    private GameObject[] _metaTexts;
    
    private Transform gridPanelTransform;

    private readonly Queue<float> _updateQueue = new();

    private int columns = 6;
    private int rows;
    private int rowsVisible = 5;
    private float prevScrollbarValue = 0.0f;

    //private float moveScrollbarValue = 0.0f;
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
      //_nResults = 200;

      _mediaDisplays = new MediaItemDisplay[_nResults];
      _metaTexts = new GameObject[_nResults];

      //+1 because of transperency
      rows = (int)Math.Ceiling((double)_nResults / (double)columns) + 1;

      //Debug.Log("Rows: " + rows);

      //set initial position
      gameObject.transform.position = new Vector3(0, 1.5f, 1.3f);

      //Setup Scrollbar
      AdvancedGridScrollbar = gameObject.GetComponentInChildren<Scrollbar>();
      //AdvancedGridScrollbar.numberOfSteps = rows - rowsVisible + 1;
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

      //var pos = gameObject.transform.position;

      var visibleWindow = columns * rowsVisible;
      var rowShift = val * (rows - rowsVisible);
      //Debug.Log(val + " " + rowShift);

      var startIndex = columns * (int) Math.Round(rowShift);
      var endIndex = startIndex + visibleWindow;

      //Debug.Log(startIndex + "-" + endIndex + ", " + startLoadIndex + "-" + endLoadIndex + " " + startUnloadIndex + "-" + endUnloadIndex);

      //Deactivate objects with index outside of visible window.
      //Debug.Log(val + ":" + startIndex + ":" + endIndex);
      _metaTexts.ToList().GetRange(0, Math.Min(startIndex, _metaTexts.Length))
        .Where(x => x != null).ToList().ForEach(x => x.SetActive(false));

      _metaTexts.ToList().GetRange(Math.Min(endIndex, _metaTexts.Length), Math.Max(0, _metaTexts.Length - endIndex))
        .Where(x => x != null).ToList().ForEach(x => x.SetActive(false));

      _mediaDisplays.ToList().GetRange(0, Math.Min(startIndex, _mediaDisplays.Length))
        .Where(x => x != null).ToList().ForEach(x => x.gameObject.SetActive(false));

      _mediaDisplays.ToList().GetRange(Math.Min(endIndex, _mediaDisplays.Length), Math.Max(0, _mediaDisplays.Length - endIndex))
        .Where(x => x != null).ToList().ForEach(x => x.gameObject.SetActive(false));
      

      //Readd Elements infront or after shown elements. This shifts the already loaded elements. 
      for(int i = startIndex; i < endIndex; i++)
      {
        if(i < _mediaDisplays.Length && _mediaDisplays[i] != null)
        {
          _mediaDisplays[i].gameObject.SetActive(true);
          _metaTexts[i].SetActive(true);
        } else if(i < _nResults)
        {
          //Debug.Log(i);
          CreateResultObject(gridPanelTransform.gameObject, i);
        }
      }

      //reposition in 3d Space and add new Objects if needed.
      for (int i = startIndex; i < endIndex; i++) 
      {
        if (i < _nResults && _mediaDisplays[i] != null)
        {
          var (newPos, newTextPos, alpha) = GetResultLocalPos(i, rowShift);
          //Debug.Log(rowShift+":"+newPos);
          var itemDisplayTransform = _mediaDisplays[i].transform;
          itemDisplayTransform.localPosition = newPos;
          itemDisplayTransform.localRotation = new Quaternion(0, 0, 0, 0);

          //set transperency
          var rawImage = itemDisplayTransform.Find("ImageFrame").Find("RawImage").GetComponent<RawImage>();
          rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, alpha);
          var scoreFrame = itemDisplayTransform.Find("ImageFrame").Find("ScoreFrame").GetComponent<RawImage>();
          scoreFrame.color = new Color(scoreFrame.color.r, scoreFrame.color.g, scoreFrame.color.b, alpha);
          

          var metaTextTransform = _metaTexts[i].transform;
          metaTextTransform.localPosition = newTextPos;
          TextMeshProUGUI metaTextUGUI = metaTextTransform.GetComponentInChildren<TextMeshProUGUI>();
          metaTextUGUI.color = new Color(metaTextUGUI.color.r, metaTextUGUI.color.g, metaTextUGUI.color.b, alpha);

          

        }
      }

      //Debug.Log(_mediaDisplays.Count);

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

      //old move steps code
      /*
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
      */
      var scrollInput = moveScrollbar.ReadValue<Vector2>().x;
      //Math.Abs(scrollInput) > 0.30 && 
      if (scrollInput < 0)
      {
        AdvancedGridScrollbar.value = Math.Max(0, AdvancedGridScrollbar.value + 6 * scrollbarStepSize * Time.deltaTime * scrollInput);
      } else
      {
        AdvancedGridScrollbar.value = Math.Min(1, AdvancedGridScrollbar.value + 6 * scrollbarStepSize * Time.deltaTime * scrollInput);
      }

     

      //Debug.Log(moveScrollbar.ReadValue<Vector2>());
    }

    private void CreateResultObject(GameObject panel, int index, float rowShift = 0)
    {
      // Determine position
      var (position, positionText, alpha) = GetResultLocalPos(index, rowShift);

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
      createMetaDataToDisplay(metaTextUGUI, index, _results[index]);
      metaTextUGUI.fontSize = 30;
      metaTextUGUI.alignment = TextAlignmentOptions.Center;
      metaTextUGUI.color = new Color(metaTextUGUI.color.r, metaTextUGUI.color.g, metaTextUGUI.color.b, alpha);

      _metaTexts[index] = metaText;

      // Add to media displays list
      _mediaDisplays[index] = itemDisplay;

      itemDisplay.Initialize(_results[index]);

      itemDisplay.gameObject.SetActive(true);

      //set transperency
      var rawImage = transform2.Find("ImageFrame").Find("RawImage").GetComponent<RawImage>();
      rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, alpha);
      var scoreFrame = transform2.Find("ImageFrame").Find("ScoreFrame").GetComponent<RawImage>();
      scoreFrame.color = new Color(scoreFrame.color.r, scoreFrame.color.g, scoreFrame.color.b, alpha);

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

    //Calculates position of mediaItem and metaText in grid based on index
    private (Vector3 position, Vector3 positionText, float alpha) GetResultLocalPos(int index, float rowShift)
    {

      var posX = -1250;
      var posY = 700;

      var column = index % columns;
      var row = index / columns;
     
      var position = new Vector3(column * 500 + posX, -(row - rowShift) * 420 + posY, -0.06f);

      var positionText = new Vector3(column * 500 + posX, -(row - rowShift) * 420 - 200 + posY, -0.04f);

      float alpha = 1;
      var lowerBoundry = -(rowsVisible - 1) * 420 + posY + 200;

      if (position.y > posY)
      {
        alpha = 1 - (position.y - posY) / 200;
      } else if (position.y < lowerBoundry)
      {
        alpha = Math.Max(0, 1 - (-position.y + lowerBoundry) / 200);
      }

      return (position, positionText, alpha);
    }

  }
}