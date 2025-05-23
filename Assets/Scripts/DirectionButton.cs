using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DirectionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    
    public enum Direction
    {
        Left,
        Right
    }

    [SerializeField] Direction direction;
    

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!VoterCharacter.LocalInstance) return;

        if (direction == Direction.Left)
        {
            VoterCharacter.LocalInstance.leftButtonPressed = true;
        }
        else if (direction == Direction.Right)
        {
            VoterCharacter.LocalInstance.rightButtonPressed = true;
        }
        
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (direction == Direction.Left)
        {
            VoterCharacter.LocalInstance.leftButtonPressed = false;
        }
        else if (direction == Direction.Right)
        {
            VoterCharacter.LocalInstance.rightButtonPressed = false;
        }
    }

    public void OnDisable()
    {
        if (VoterCharacter.LocalInstance != null)
        {
            if (direction == Direction.Left && VoterCharacter.LocalInstance.leftButtonPressed)
            {
                VoterCharacter.LocalInstance.leftButtonPressed = false;
            }
            else if (direction == Direction.Right && VoterCharacter.LocalInstance.rightButtonPressed)
            {
                VoterCharacter.LocalInstance.rightButtonPressed = false;
            }
        }
    }
}
