using Unity.Netcode;
using UnityEngine;

public class InheritNetworkBehavior : NetworkBehaviour
{
    private AnimationManager animationManager;
    private SoundManager soundManager;

    public void Initialize(
        AnimationManager hostAnimationManager,
        SoundManager hostSoundManager)
    {
        animationManager = hostAnimationManager;
        soundManager = hostSoundManager;
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
}
