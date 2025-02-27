namespace Checkers;

public class Game
{
    private const int PiecesPerColor = 12;
    private const int MaxUndoCount = 3;
    private const int MaxSteps = 20;
    public Stack<Move> MoveHistory = new Stack<Move>();
    public Dictionary<PieceColor, int> Steps = new() { { PieceColor.Black, 0 }, { PieceColor.White, 0 } };
    public Dictionary<PieceColor, int> UndoCounts = new() { { PieceColor.Black, 0 }, { PieceColor.White, 0 } };

    public PieceColor Turn { get; private set; }
    public Board Board { get; }
    public PieceColor? Winner { get; private set; }
    public List<Player> Players { get; }

    public Dictionary<PieceColor, int> ExtraTurns = new() { { PieceColor.Black, 1 }, { PieceColor.White, 1 } }; // Add this field to keep track of extra turns


    public Game(int humanPlayerCount)
    {
        if (humanPlayerCount < 0 || 2 < humanPlayerCount) throw new ArgumentOutOfRangeException(nameof(humanPlayerCount));
        Board = new Board();
        Players = new()
        {
            new Player(humanPlayerCount >= 1, Black),
            new Player(humanPlayerCount >= 2, White),
        };
        Turn = Black;
        Winner = null;
    }

    public void PerformMove(Move move)
    {
        MoveHistory.Push(new Move(move.PieceToMove, (move.PieceToMove.X, move.PieceToMove.Y), move.To, move.PieceToCapture));// Modify here to ensure that `MoveHistory` records the `From` position
        Steps[move.PieceToMove.Color]++;

        (move.PieceToMove.X, move.PieceToMove.Y) = move.To;
        if ((move.PieceToMove.Color is Black && move.To.Y is 7) ||
            (move.PieceToMove.Color is White && move.To.Y is 0))
        {
            move.PieceToMove.Promoted = true;
        }
        if (move.PieceToCapture is not null)
        {
            Board.Pieces.Remove(move.PieceToCapture);
        }
        if (move.PieceToCapture is not null &&
            Board.GetPossibleMoves(move.PieceToMove).Any(m => m.PieceToCapture is not null))
        {
            Board.Aggressor = move.PieceToMove;
        }
        else
        {
            Board.Aggressor = null;
            Turn = Turn is Black ? White : Black;
        }
        CheckForWinner();
    }

    public void UndoMove()
    {
        if (MoveHistory.Count > 0 && UndoCounts[Turn] < MaxUndoCount)
        {
            Move lastMove = MoveHistory.Pop();
            Steps[lastMove.PieceToMove.Color]--;
            UndoCounts[lastMove.PieceToMove.Color]++;
            
            (lastMove.PieceToMove.X, lastMove.PieceToMove.Y) = lastMove.From;
            if (lastMove.PieceToCapture is not null)
            {
                Board.Pieces.Add(lastMove.PieceToCapture);
            }
            Turn = Turn is Black ? White : Black;
        }
    }

    public void CheckForWinner()
    {
        if (!Board.Pieces.Any(piece => piece.Color is Black))
        {
            Winner = White;
        }
        if (!Board.Pieces.Any(piece => piece.Color is White))
        {
            Winner = Black;
        }
        if (Winner is null && Board.GetPossibleMoves(Turn).Count is 0)
        {
            Winner = Turn is Black ? White : Black;
        }
        if (Steps[Black] >= MaxSteps && Steps[White] >= MaxSteps)
        {
            int blackTaken = TakenCount(White);
            int whiteTaken = TakenCount(Black);
            Winner = blackTaken > whiteTaken ? Black : (whiteTaken > blackTaken ? White : White);
        }
    }

    public int TakenCount(PieceColor colour) =>
        PiecesPerColor - Board.Pieces.Count(piece => piece.Color == colour);
}



