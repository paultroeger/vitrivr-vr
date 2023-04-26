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

    private int _nResults;
    private readonly List<MediaItemDisplay> _mediaDisplays = new();
    private readonly List<TextMeshProUGUI> _metaTexts = new();

    private int columns;
    private int rows;

    protected override void Initialize()
    {
      var fusionResults = QueryData.GetMeanFusionResults();
      var _results = fusionResults;
      if (_results == null)
      {
          NotificationController.Notify("No results returned from query!");
          _results = new List<ScoredSegment>();
      }

      _nResults = _results.Count;

      columns = 4;
      rows = (_nResults / columns) + 1;

      //set initial position
      gameObject.transform.position = new Vector3(0, 1.5f, 1.3f);

      Scrollbar scrollbar = gameObject.GetComponentInChildren<Scrollbar>();
      Debug.Log(scrollbar.size);

      //GameObject scrollbar = GameObject.Find("/AdvancedGridQueryDisplay/Canvas/Panel/Scrollbar");

      //get Text
      TextMeshProUGUI titleText = gameObject.GetComponentInChildren<TextMeshProUGUI>();

      titleText.text = "Advanced Grid (Displaying: " + _nResults + " Results)";

      //get Panel
      Transform gridPanel = gameObject.transform.Find("Canvas").transform.Find("Panel");
     
      if (gridPanel != null)
      {

        for (int i = 0; i < 16; i++)
        {
          CreateResultObject(_results[i], gridPanel.gameObject);
        }
        

      }

    }

    private void CreateResultObject(ScoredSegment result, GameObject panel)
    {
      // Determine position
      var index = _mediaDisplays.Count;
      var (position, positionText) = GetResultLocalPosRot(index);

      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);

      

      var transform2 = itemDisplay.transform;

      transform2.SetParent(panel.transform);
      transform2.localPosition = position;

      //RectTransform rectTransform = itemDisplay.GetComponent<RectTransform>();
      //rectTransform.anchoredPosition = new Vector3(0, 0, 0);


      //transform2.localRotation = rotation;
      // Adjust size
      Debug.Log(transform2.localScale);

      transform2.localScale *= 0.1f;

      //Text

      GameObject metaText = Instantiate(textMeshProPrefab, panel.transform);

      metaText.transform.localPosition = positionText;

      TextMeshProUGUI metaTextUGUI = metaText.GetComponentInChildren<TextMeshProUGUI>();
      metaTextUGUI.text = "Index " + index;
      metaTextUGUI.fontSize = 24;

      //RectTransform newTextTransform = metaText.GetComponentInChildren<RectTransform>();
      //newTextTransform.anchoredPosition = new Vector3 (0,0,0);
      //newTextTransform.sizeDelta = new Vector2(550, 50);

      // Add to media displays list
      _mediaDisplays.Add(itemDisplay);

      itemDisplay.Initialize(result);

      itemDisplay.gameObject.SetActive(true);
    }

    private (Vector3 position, Vector3 positionText) GetResultLocalPosRot(int index, float distanceDelta = 0)
    {

      var posX = -800;
      var posY = 500;

      var column = index % columns;
      var row = index / columns;
      //var multiplier = resultSize + padding;
      var position = new Vector3(column * 300 + posX, -row * 300 + posY, -0.1f);

      var positionText = new Vector3(column * 300 + posX, -row * 300 - 120 + posY, -0.1f);

      return (position, positionText);
    }
  }
}