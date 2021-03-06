﻿using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class KinectBodyManager : MonoBehaviour
{
    #region Events

    public event Action<KinectBody> EventBodyEnter;
    public event Action<ulong> EventBodyLeave;

    #endregion

    [SerializeField]
    private BodySourceManager bodySourceManager;
    [SerializeField]
    private bool debug = false;
    [SerializeField]
    private Text debugText;
    [SerializeField]
    private GameObject kinectView;

    private IDictionary<ulong, KinectBody> kinectBodies;

    public bool Debug
    {
        get { return debug; }
        set
        {
            debug = value;
            if (debug)
            {
                debugText.enabled = true;
                StartCoroutine(DisplayDebug());
            }
            else
            {
                debugText.enabled = false;
                StopCoroutine(DisplayDebug());
            }
            kinectView.SetActive(debug);
        }
    }

    public ICollection<KinectBody> Bodies
    {
        get { return kinectBodies.Values.Where(p => p.enabled).ToList(); }
    }

    #region MonoBehaviour

    private void Start()
    {
        kinectBodies = new Dictionary<ulong, KinectBody>();
        Debug = debug;

        Debug = ApplicationManager.Instance.UseDebugeMode;
    }

    private void Update()
    {
        var data = bodySourceManager.GetData();

        // this will usually not be initialzed at the start of play
        if (data == null)
        {
            return;
        }

        // figure out if any bodies have been updated
        // also keep track of the current tracked IDs
        var trackedIDs = from body in data where body.IsTracked && body != null select body.TrackingId;
        var trackedBodies = from body in data where body.IsTracked && body != null select body;

        // disable untracked bodies
        foreach (var id in kinectBodies.Keys)
        {
            if (!trackedIDs.Contains(id))
            {
                OnBodyLeave(id);
                kinectBodies[id].enabled = false;
            }
        }

        // add any new bodies
        foreach (var body in trackedBodies)
        {
            if (!kinectBodies.ContainsKey(body.TrackingId))
            {
                var newBody = gameObject.AddComponent<KinectBody>();
                newBody.Body = body;
                kinectBodies[body.TrackingId] = newBody;
                OnBodyEnter(newBody);
            }
            else if (!kinectBodies[body.TrackingId].enabled)
            {
                kinectBodies[body.TrackingId].enabled = true;
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnBodyEnter(KinectBody body)
    {
        if (EventBodyEnter != null)
        {
            EventBodyEnter(body);
        }
    }

    private void OnBodyLeave(ulong id)
    {
        if (EventBodyLeave != null)
        {
            EventBodyLeave(id);
        }
    }

    #endregion

    public KinectBody GetBody(ulong id)
    {
        if (kinectBodies.ContainsKey(id))
        {
            return kinectBodies[id];
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// Gets the average velocity of all bodies for specific joints.
    /// </summary>
    /// <param name="joints">
    /// The joints.
    /// </param>
    /// <returns>
    /// Average velocity as a vector.
    /// </returns>
    public Vector3 GetAverageVelocity(JointType[] joints)
    {
        var average = Vector3.zero;

        foreach (var body in Bodies)
        {
            foreach (var joint in joints)
            {
                average += body.Velocity(joint);
            }
        }

        if (average != Vector3.zero)
        {
            average /= Bodies.Count * (float)joints.Length;
        }

        return average;
    }

    private IEnumerator DisplayDebug()
    {
        while (true)
        {
            debugText.text = "Hand Velocity\n";
            foreach (var body in Bodies)
            {
                debugText.text += "Left: " + body.Velocity(JointType.HandLeft) + "\t";
                debugText.text += "Right: " + body.Velocity(JointType.HandRight) + "\n";
            }

            yield return null;
        }
    }

    public static Vector3 GetJointPosition2D(Windows.Kinect.Joint joint, float z = 0f)
    {
        // TODO: do projection space calculations here instead of 10x values
        return new Vector3(joint.Position.X, joint.Position.Y * 10, z);
    }
}
