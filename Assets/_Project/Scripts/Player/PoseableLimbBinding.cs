using System;
using UnityEngine;

[Serializable]
public struct PoseableLimbBinding
{
    [SerializeField]
    private PlayerSlot playerSlot;

    [SerializeField]
    private PoseableLimbAction action;

    public PlayerSlot PlayerSlot => playerSlot;

    public PoseableLimbAction Action => action;
}