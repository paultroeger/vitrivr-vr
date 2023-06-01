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
  public class StudyDisplay : QueryDisplay
  {

    public override int NumberOfResults => _nResults;
    public MediaItemDisplay mediaItemDisplay;

    private List<ScoredSegment> _results;
    private int _nResults;
    
    private Transform gridPanelTransform;

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


      //Debug.Log("Rows: " + rows);

      //set initial position
      gameObject.transform.position = new Vector3(0.8f, 1.5f, 0f);
     

      //get Panel
      gridPanelTransform = gameObject.transform.Find("Canvas").transform.Find("Panel");

      gridPanelTransform.localRotation = Quaternion.Euler(0, 90, 0);
      gridPanelTransform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

      //Study Code
      //Debug.Log(_results[43].segment.Id);
      //Debug.Log(_results[51].segment.Id);
      //Debug.Log(_results[66].segment.Id);
      //Debug.Log(_results[96].segment.Id);

      var list = _results.Where(x => x.segment.Id == "v_10441_60" || x.segment.Id == "v_07249_60" || x.segment.Id == "v_00127_72" || x.segment.Id == "v_01942_38").ToList();

      if (list.Count > 0)
      {
        
        
        var itemDisplay = Instantiate(mediaItemDisplay, Vector3.zero, Quaternion.identity, transform);

        var transform2 = itemDisplay.transform;
        transform2.SetParent(gridPanelTransform);
        transform2.localPosition = new Vector3(0, 0, 0.0f);
        transform2.localRotation = Quaternion.Euler(0, 0, 0);

        transform2.localScale = new Vector3(0.4f, 0.4f, 0.1f);

        itemDisplay.Initialize(list[0]);

        itemDisplay.gameObject.SetActive(true);

        var scoreFrame = itemDisplay.transform.Find("ImageFrame").Find("ScoreFrame").GetComponent<RawImage>();
        scoreFrame.color = new Color(0, 255, 255, 1);

      }
      //Study Code
    }

  }
}