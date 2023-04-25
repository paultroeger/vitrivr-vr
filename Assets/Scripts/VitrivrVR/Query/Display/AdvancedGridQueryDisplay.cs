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

    private int _nResults;
    private readonly List<MediaItemDisplay> _mediaDisplays = new();


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


      /*Canvas canvas = gameObject.AddComponent<Canvas>();

      gameObject.AddComponent<CanvasScaler>();
      gameObject.AddComponent<GraphicRaycaster>();
      GameObject panel = new GameObject("Panel");
      panel.AddComponent<CanvasRenderer>();
      panel.transform.SetParent(gameObject.transform, false);

      Image image = panel.AddComponent<Image>();
      image.color = Color.gray;
      
      // Text
      GameObject myText = new GameObject();
      myText.transform.parent = panel.transform;
      myText.name = "wibble";

      Text text = myText.AddComponent<Text>();
      text.text = "wobble";
      text.color = Color.red;
      text.fontSize = 100;
      
      // Text position
      RectTransform rectTransform = text.GetComponent<RectTransform>();
      rectTransform.localPosition = new Vector3(0, 0, 0);
      rectTransform.sizeDelta = new Vector2(400, 200);
      */

      gameObject.transform.position = new Vector3(0, 1.2f, 0.5f);

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
        CreateResultObject(_results[0], gridPanel.gameObject);

      }

    }

    private void CreateResultObject(ScoredSegment result, GameObject panel)
    {
      // Determine position
      //var index = _mediaDisplays.Count;
      //var (position, rotation) = GetResultLocalPosRot(index);

      var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);

      

      var transform2 = itemDisplay.transform;

      transform2.SetParent(panel.transform);
      transform2.localPosition = new Vector3(0, 0, 0);

      RectTransform rectTransform = itemDisplay.GetComponent<RectTransform>();
      rectTransform.anchoredPosition = new Vector3(0, 0, 0);
      

      //transform2.localRotation = rotation;
      // Adjust size
      transform2.localScale *= 0.5f;

      // Add to media displays list
      _mediaDisplays.Add(itemDisplay);

      itemDisplay.Initialize(result);

      itemDisplay.gameObject.SetActive(true);
    }
  }
}