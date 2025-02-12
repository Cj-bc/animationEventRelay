using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>A relay for Animator's AnimationEvent</summary>
[RequireComponent(typeof(Animator))]
public class AnimationEventRelay : MonoBehaviour
{
    /// <summary>Corresponding animator</summary>
    public Animator animator;

    /// <summary>Association of AnimationEventID and list of callback functions registered for it.</summary>
    /// <remark>Key represents the AnimationEventID attached to each AnimationEvent. Value is a list of registered callback functions.</remark>
    private Dictionary<int, List<Action>> registeredCallbacks = new();
    /// <summary>Association of set of AnimationEvent and AnimationEventID</summary>
    private Dictionary<(AnimationClip, TimeSpan), int> animatoinEventIdMap = new();

    private System.Random randomizer = new();

    public void OnEnable()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning("AnimationEventRelay should be attached to the same gameObject as Animator");
            enabled = false;
            return;
        }
    }

    /// <summary>Inject new AnimationEvent and call given callback</summary>
    /// <param name="callback">A function to call on registered timing</param>
    public void InjectListener(string clipName, TimeSpan time, Action callback)
    {
        if (getClip(clipName) is AnimationClip c)
        {
            Inject(c, time, callback);
        }
    }

    /// <summary>Inject new AnimationEvent at end of the clip and call given callback</summary>
    public void InjectEndedListener(string clipName, Action callback)
    {
        if (getClip(clipName) is AnimationClip c)
        {
            Inject(c, TimeSpan.FromSeconds(c.length), callback);
        }
    }

    public void RemoveEndedListener(string clipName, Action callback)
    {
        if (getClip(clipName) is AnimationClip c)
        {
            RemoveListener(clipName, TimeSpan.FromSeconds(c.length), callback);
        }
    }

    /// <summary>Unregister callback of given time.</summary>
    public void RemoveListener(string clipName, TimeSpan time, Action callback)
    {
        if (getClip(clipName) is AnimationClip c
            && animatoinEventIdMap.TryGetValue(new(c, time), out int id)
            && registeredCallbacks.GetValueOrDefault(id) is List<Action> callbacks)
        {
            callbacks.Remove(callback);
        }
    }

    /// <summary>Register animation event at given <c cref="time">time</c>, and arrange it to call given callback</summary>
    private void Inject(AnimationClip clip, TimeSpan time, Action callback)
    {
        if (animatoinEventIdMap.TryGetValue((clip, time), out int id)
            && registeredCallbacks.TryGetValue(id, out List<Action> callbacks))
        {
            callbacks.Add(callback);
        } else
        {
            id = randomizer.Next();
            // Stores callback function for internal use
            registeredCallbacks.Add(id, new() {callback});
            animatoinEventIdMap.Add((clip, time), id);

            // Adds AnimationEvent to the clip
            AnimationEvent ev = new AnimationEvent();
            ev.time = (float)time.TotalSeconds;
            ev.functionName = "Receiver";
            ev.intParameter = id;
            clip.AddEvent(ev);
        }
    }

    private AnimationClip? getClip(string clipName)
        => animator.runtimeAnimatorController.animationClips
        .Where(clip => clip.name == clipName)
        .FirstOrDefault();

    private void Receiver(int arg)
    {
        if (registeredCallbacks.TryGetValue(arg, out List<Action> callbacks))
        {
            callbacks.ForEach(callback => callback());
        }
    }
}
