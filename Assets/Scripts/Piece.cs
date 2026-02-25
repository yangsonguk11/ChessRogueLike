using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] RangeInfoSO MoveableButton;

    public List<Vector2> GetMoveableButton() { return MoveableButton.GetAbleRange(); }
}
