using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
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

        
        Debug.Log(_results.Count);
      }
    }
}