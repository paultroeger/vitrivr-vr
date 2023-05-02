using System;
using System.Collections.Generic;
using System.Linq;
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

    private List<ScoredSegment> _results;
    private int _nResults;
    private readonly List<MediaItemDisplay> _mediaDisplays = new();
    private readonly List<GameObject> _metaTexts = new();
    private Transform gridPanelTransform;

    private int columns;
    private int rows;
    private int rowsVisible;
    private int gridHeight;

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

      //Debug
      _nResults = 72;

      for (int i = 0; i < _nResults; i++)
      {
        _mediaDisplays.Add(null);
        _metaTexts.Add(null);
      }
      

      columns = 6;
      rows = (int)Math.Ceiling((double)(_nResults / columns));
      rowsVisible = 4;

      gridHeight = rows * 400;

      //set initial position
      gameObject.transform.position = new Vector3(0, 1.5f, 1.3f);

      AdvancedGridScrollbar = gameObject.GetComponentInChildren<Scrollbar>();
      AdvancedGridScrollbar.numberOfSteps = rows;
      AdvancedGridScrollbar.size = rowsVisible / rows;
      AdvancedGridScrollbar.onValueChanged.AddListener((float val) => updateResultPosition(val));
      Debug.Log(AdvancedGridScrollbar.size);

      //GameObject scrollbar = GameObject.Find("/AdvancedGridQueryDisplay/Canvas/Panel/Scrollbar");

      //get Text
      TextMeshProUGUI titleText = gameObject.GetComponentInChildren<TextMeshProUGUI>();

      titleText.text = "Advanced Grid (Displaying: " + _nResults + " Results)";

      //get Panel
      gridPanelTransform = gameObject.transform.Find("Canvas").transform.Find("Panel");
     
      if (gridPanelTransform != null)
      {

        for (int i = 0; i < columns*rowsVisible; i++)
        {
          CreateResultObject(_results[i], gridPanelTransform.gameObject, i);
        }
        

      }

    }

    private void updateResultPosition(float val)
    {

      var visibleWindow = columns * rowsVisible;

      Debug.Log(val);

      var startIndex = (int)(Math.Ceiling(val * rows) / rowsVisible) * rowsVisible * columns;
      var endIndex = startIndex + visibleWindow;

      var startLoadIndex = Math.Max(0, startIndex - visibleWindow);
      var endLoadIndex = Math.Min(_nResults - 1, endIndex + visibleWindow);

      var startUnloadIndex = Math.Max(0, startIndex - 2*visibleWindow);
      var endUnloadIndex = Math.Min(_nResults - 1, endIndex + 2*visibleWindow);

      Debug.Log(startIndex + "-" + endIndex + ", " + startLoadIndex + "-" + endLoadIndex + " " + startUnloadIndex + "-" + endUnloadIndex);

      //Load
      for (int i = startLoadIndex; i < startIndex; i++)
      {
        if (_mediaDisplays[i] == null)
        {
          CreateResultObject(_results[i], gridPanelTransform.gameObject, i);
        }
      }

      for (int i = endIndex + 1; i < endLoadIndex + 1; i++)
      {
        if (_mediaDisplays[i] == null)
        {
          CreateResultObject(_results[i], gridPanelTransform.gameObject, i);
        }
      }

      //unload
      for (int i = startUnloadIndex; i < startLoadIndex; i++)
      {
        if (_mediaDisplays[i] != null)
        {
          _mediaDisplays[i].gameObject.SetActive(false);
          //_mediaDisplays[i] = null;
        }
      }

      for (int i = endLoadIndex + 1; i < endUnloadIndex + 1; i++)
      {
        if (_mediaDisplays[i] != null)
        {
          _mediaDisplays[i].gameObject.SetActive(false);
          //_mediaDisplays[i] = null;
        }
      }

      //reposition
      for (int i = startLoadIndex; i < endLoadIndex + 1; i++) 
      {
        if (_mediaDisplays[i] != null)
        { 
          var (newPos, newTextPos) = GetResultLocalPos(i, val);
          //Debug.Log(newPos);
          var itemDisplayTransform = _mediaDisplays[i].transform;
          itemDisplayTransform.localPosition = newPos;
          var metaTextTransform = _metaTexts[i].transform;
          metaTextTransform.localPosition = newTextPos;
          // Set disabled if outside of active range
          _mediaDisplays[i].gameObject.SetActive(startIndex <= i && i < endIndex);
        }
      }

      

      gameObject.transform.position = new Vector3(0, 1.5f, 1.3f);
    }

    private void CreateResultObject(ScoredSegment result, GameObject panel, int index)
    {
      // Determine position
      var (position, positionText) = GetResultLocalPos(index);

      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);

      

      var transform2 = itemDisplay.transform;

      transform2.SetParent(panel.transform);
      transform2.localPosition = position;

      //RectTransform rectTransform = itemDisplay.GetComponent<RectTransform>();
      //rectTransform.anchoredPosition = new Vector3(0, 0, 0);


      //transform2.localRotation = rotation;
      // Adjust size
      Debug.Log(transform2.localScale);

      transform2.localScale = new Vector3(0.4f, 0.4f, 0.1f);

      //Text

      GameObject metaText = Instantiate(textMeshProPrefab, panel.transform);

      metaText.transform.localPosition = positionText;

      TextMeshProUGUI metaTextUGUI = metaText.GetComponentInChildren<TextMeshProUGUI>();
      metaTextUGUI.text = "Index " + index;
      metaTextUGUI.fontSize = 30;

      _metaTexts[index] = metaText;

      //RectTransform newTextTransform = metaText.GetComponentInChildren<RectTransform>();
      //newTextTransform.anchoredPosition = new Vector3 (0,0,0);
      //newTextTransform.sizeDelta = new Vector2(550, 50);

      // Add to media displays list
      _mediaDisplays[index] = itemDisplay;

      itemDisplay.Initialize(result);

      itemDisplay.gameObject.SetActive(true);
    }

    private (Vector3 position, Vector3 positionText) GetResultLocalPos(int index, float scrollbarValue = 0)
    {

      var posX = -1250;
      var posY = 600;

      var column = index % columns;
      var row = index / columns;
      //var multiplier = resultSize + padding;
      var position = new Vector3(column * 500 + posX, -row * 400 + gridHeight * scrollbarValue + posY, -0.05f);

      var positionText = new Vector3(column * 500 - 50 + posX, -row * 400 - 200 + gridHeight * scrollbarValue + posY, -0.05f);

      return (position, positionText);
    }
  }
}