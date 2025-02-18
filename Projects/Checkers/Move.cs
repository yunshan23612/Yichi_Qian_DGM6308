﻿namespace Checkers;

public class Move
{
    public Piece PieceToMove { get; set; }

    public (int X, int Y) From { get; set; }  // Add a From variable to store the original position
    public (int X, int Y) To { get; set; }
    public Piece? PieceToCapture { get; set; }

    // Modify the constructor to ensure that the From variable is initialized correctly
    public Move(Piece pieceToMove, (int X, int Y) from, (int X, int Y) to, Piece? pieceToCapture = null)
    {
        PieceToMove = pieceToMove;
        From = from;  // Assignment starting position
        To = to;
        PieceToCapture = pieceToCapture;
    }
}
