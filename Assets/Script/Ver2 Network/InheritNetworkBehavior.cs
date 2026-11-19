using Unity.Netcode;
using System;
using UnityEngine;

public class InheritNetworkBehavior : NetworkBehaviour
{
    private AnimationManager animationManager;
    private SoundManager soundManager;
    private TraceManager traceManager;

    public static event Action<string> TraceHistoryReceived;
    public static event Action<string> TraceDeleted;
    public static event Action<string> TraceDestroyed;

    public void Initialize(
        AnimationManager hostAnimationManager,
        SoundManager hostSoundManager,
        TraceManager hostTraceManager)
    {
        animationManager = hostAnimationManager;
        soundManager = hostSoundManager;
        traceManager = hostTraceManager;
    }

    public void RequestAnimation(AnimationManager.RobotAnimation animation)
    {
        RequestAnimationRpc(animation);
    }

    public void RequestVoiceRole(SoundManager.VoiceRole role)
    {
        RequestVoiceRoleRpc(role);
    }

    public void RequestSound(SoundManager.RobotSound sound)
    {
        RequestSoundRpc(sound);
    }

    public void RequestNewTrace(string traceId)
    {
        RequestNewTraceRpc(traceId);
    }

    public void RequestCompleteTrace(string traceId, string content)
    {
        RequestCompleteTraceRpc(traceId, content);
    }

    public void RequestTraceHistory()
    {
        RequestTraceHistoryRpc();
    }

    public void RequestDeleteTrace(string traceId)
    {
        RequestDeleteTraceRpc(traceId);
    }

    public void RequestDestroyTrace(string traceId)
    {
        RequestDestroyTraceRpc(traceId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestAnimationRpc(AnimationManager.RobotAnimation animation)
    {
        if (animationManager == null)
        {
            Debug.LogWarning("Bridge has no Host AnimationManager.", this);
            return;
        }

        animationManager.Play(animation);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestVoiceRoleRpc(SoundManager.VoiceRole role)
    {
        if (soundManager == null)
        {
            Debug.LogWarning("Bridge has no Host SoundManager.", this);
            return;
        }

        soundManager.SelectVoiceRole(role);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestSoundRpc(SoundManager.RobotSound sound)
    {
        if (soundManager == null)
        {
            Debug.LogWarning("Bridge has no Host SoundManager.", this);
            return;
        }

        soundManager.Play(sound);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestNewTraceRpc(string traceId)
    {
        if (traceManager == null || !traceManager.BeginTrace(traceId))
        {
            Debug.LogWarning("Host could not begin the requested trace.", this);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCompleteTraceRpc(string traceId, string content)
    {
        if (traceManager == null || !traceManager.CompleteTrace(traceId, content))
        {
            Debug.LogWarning("Host could not complete the requested trace.", this);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestTraceHistoryRpc()
    {
        string json = traceManager != null
            ? traceManager.ExportHistoryJson()
            : "{\"traces\":[]}";
        ReceiveTraceHistoryRpc(json);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDeleteTraceRpc(string traceId)
    {
        if (traceManager == null || !traceManager.DeleteTrace(traceId))
        {
            Debug.LogWarning("Host could not delete the requested trace.", this);
            return;
        }

        ReceiveTraceDeletedRpc(traceId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDestroyTraceRpc(string traceId)
    {
        if (traceManager == null || !traceManager.DestroyTrace(traceId))
        {
            Debug.LogWarning("Host could not destroy the requested bubble.", this);
            return;
        }

        ReceiveTraceDestroyedRpc(traceId);
    }

    [Rpc(SendTo.NotServer)]
    private void ReceiveTraceHistoryRpc(string json)
    {
        TraceHistoryReceived?.Invoke(json);
    }

    [Rpc(SendTo.NotServer)]
    private void ReceiveTraceDeletedRpc(string traceId)
    {
        TraceDeleted?.Invoke(traceId);
    }

    [Rpc(SendTo.NotServer)]
    private void ReceiveTraceDestroyedRpc(string traceId)
    {
        TraceDestroyed?.Invoke(traceId);
    }
}
