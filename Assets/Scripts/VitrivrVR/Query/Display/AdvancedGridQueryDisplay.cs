using System;
using System.Collections.Generic;
using System.Linq;
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

      public override int NumberOfResults => -6;
      
      protected override void Initialize()
      {
        var fusionResults = QueryData.GetMeanFusionResults();
        var _results = fusionResults;
        if (_results == null)
        {
            NotificationController.Notify("No results returned from query!");
            _results = new List<ScoredSegment>();
        }

        gameObject.AddComponent<Canvas>();

        Canvas canvas = GetComponent<Canvas>();
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 200);
        //canvas.GetComponentInChildren<Image>().color = Color.blue;

        // Text
        GameObject myText = new GameObject();
        myText.transform.parent = canvas.transform;
        myText.name = "wibble";

        Text text = myText.AddComponent<Text>();
        text.text = "wobble";
        text.color = Color.red;
        text.fontSize = 100;

        // Text position
        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(0, 0, 0);
        rectTransform.sizeDelta = new Vector2(400, 200);

      Debug.Log(_results.Count);
        Debug.Log(GetComponent<Canvas>());
      }
    }
}