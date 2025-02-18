// Declare a nullable exception variable to store possible exceptions
Exception? exception = null;

// Store the current console output encoding
Encoding encoding = Console.OutputEncoding;

try
{
	// Set the console output encoding to UTF-8
	Console.OutputEncoding = Encoding.UTF8;
	// Display the game introduction interface and get game options (return the Game object)
	Game game = ShowIntroScreenAndGetOption();
	// Clear the console
	Console.Clear();
	// Run the main loop
	RunGameLoop(game);
	// Rendering the final game state and if press any key,the console will be continued
	RenderGameState(game, promptPressKey: true);
	// Wait for a key press
	Console.ReadKey(true);
}
catch (Exception e)
{
	// Catch all possible exceptions and save them to the exception variable
	exception = e;
	// throw the exceptions
	throw;
}
finally
{
	// Regardless of whether an exception occurs, the following cleanup operations are performed:
	Console.OutputEncoding = encoding;
	Console.CursorVisible = true;
	Console.Clear();
	Console.WriteLine(exception?.ToString() ?? "Checkers was closed.");
}

// initialize the first screen of the game,let the player choose the number of human players
Game ShowIntroScreenAndGetOption()
{
	Console.Clear();
	Console.WriteLine();
	Console.WriteLine("  Checkers");
	Console.WriteLine();
	Console.WriteLine("  Checkers is played on an 8x8 board between two sides commonly known as black");
	Console.WriteLine("  and white. The objective is simple - capture all your opponent's pieces. An");
	Console.WriteLine("  alternative way to win is to trap your opponent so that they have no valid");
	Console.WriteLine("  moves left.");
	Console.WriteLine();
	Console.WriteLine("  Black starts first and players take it in turns to move their pieces forward");
	Console.WriteLine("  across the board diagonally. Should a piece reach the other side of the board");
	Console.WriteLine("  the piece becomes a king and can then move diagonally backwards as well as");
	Console.WriteLine("  forwards.");
	Console.WriteLine();
	Console.WriteLine("  Pieces are captured by jumping over them diagonally. More than one enemy piece");
	Console.WriteLine("  can be captured in the same turn by the same piece. If you can capture a piece");
	Console.WriteLine("  you must capture a piece.");
	Console.WriteLine();
	Console.WriteLine("  Moves are selected with the arrow keys. Use the [enter] button to select the");
	Console.WriteLine("  from and to squares. Invalid moves are ignored.");
	Console.WriteLine();
	Console.WriteLine("  Press a number key to choose number of human players:");
	Console.WriteLine("    [0] Black (computer) vs White (computer)");
	Console.WriteLine("    [1] Black (human) vs White (computer)");
	Console.Write("    [2] Black (human) vs White (human)");

	// Declare a nullable integer variable to store the number of human players
	int? humanPlayerCount = null;

	// Loop until a valid player count is selected
	while (humanPlayerCount is null)
	{
		// Hide the cursor
		Console.CursorVisible = false;

		// Read user key input and set the player count based on the key pressed
		switch (Console.ReadKey(true).Key)
		{
			// When 0 is pressed (either number row or numpad), set to 0 human players (computer vs computer)
			case ConsoleKey.D0 or ConsoleKey.NumPad0: humanPlayerCount = 0; break;
			// PVE
			case ConsoleKey.D1 or ConsoleKey.NumPad1: humanPlayerCount = 1; break;
			// PVP
			case ConsoleKey.D2 or ConsoleKey.NumPad2: humanPlayerCount = 2; break;
		}
	}

	// Create and return a new Game instance with the selected number of human players
	return new Game(humanPlayerCount.Value);
}

// main game loop,run the game until there is a winner
void RunGameLoop(Game game)
{
    while (game.Winner is null)
    {
        // Get the current player
        Player currentPlayer = game.Players.First(player => player.Color == game.Turn);

        // handle human player's turn
        if (currentPlayer.IsHuman)
        {
            while (game.Turn == currentPlayer.Color)
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                // The player presses "U" to undo the move
                if (key == ConsoleKey.U && game.UndoCounts[game.Turn] < 3)
                {
                    game.UndoMove();
                    RenderGameState(game);
                    continue; // Skip the current round and wait for input again
                }

                // wait for the player to make a move
                Move? move = GetPlayerMove(game);
                if (move is not null)
                {
                    game.PerformMove(move);
                    break;
                }
            }
        }
        else // handle computer player's turn
        {
            Move? move = GetComputerMove(game);
            if (move is not null)
            {
                game.PerformMove(move);
            }
        }

        // Check if the maximum number of steps has been reached
        game.CheckForWinner();

        // Rendering the chessboard
        RenderGameState(game);

        // if there is a winner, exit the loop
        if (game.Winner is not null)
            break;
    }

    Console.WriteLine($"{game.Winner} win!");
}

// Let the player select pieces and perform moves
Move? GetPlayerMove(Game game)
{
    (int X, int Y)? selectionStart = null;
    (int X, int Y)? from = game.Board.Aggressor is not null ? 
        (game.Board.Aggressor.X, game.Board.Aggressor.Y) : null;

    List<Move> moves = game.Board.GetPossibleMoves(game.Turn);

    // if there is only one legal piece to move, automatically select it
    if (moves.Select(move => move.PieceToMove).Distinct().Count() == 1)
    {
        Move mustMove = moves.First();
        from = (mustMove.PieceToMove.X, mustMove.PieceToMove.Y);
        selectionStart = mustMove.To;
    }

    // let the player select the piece
    while (from is null)
    {
        from = HumanMoveSelection(game);
        selectionStart = from;
    }

    // let the player select the destination
    (int X, int Y)? to = HumanMoveSelection(game, selectionStart: selectionStart, from: from);
    if (to is null)
        return null; // if the player cancels the move, return null

    Piece? piece = game.Board[from.Value.X, from.Value.Y];

    // make sure the selected piece is the player's own piece
    if (piece is null || piece.Color != game.Turn)
    {
        return null;
    }

    // check if the move is valid
    return game.Board.ValidateMove(game.Turn, from.Value, to.Value);
}

// let the computer select a valid move
Move? GetComputerMove(Game game)
{
    List<Move> moves = game.Board.GetPossibleMoves(game.Turn);
    List<Move> captures = moves.Where(move => move.PieceToCapture is not null).ToList();

    // If there is a capture action, it will be executed first
    if (captures.Count > 0)
    {
        return captures[Random.Shared.Next(captures.Count)];
    }

    // If all pieces are kings, try to get closer to the opponent
    if (!game.Board.Pieces.Any(piece => piece.Color == game.Turn && !piece.Promoted))
    {
        var (a, b) = game.Board.GetClosestRivalPieces(game.Turn);
        Move? priorityMove = moves.FirstOrDefault(move => move.PieceToMove == a && Board.IsTowards(move, b));
        return priorityMove ?? moves[Random.Shared.Next(moves.Count)];
    }

    // Default is to take a random step
    return moves.Count > 0 ? moves[Random.Shared.Next(moves.Count)] : null;
}



// Method to render the current game state on the console
// Parameters:
//   game: The current game instance
//   playerMoved: The player who just made a move (optional)
//   selection: Currently selected position on the board (optional)
//   from: Starting position of a move (optional)
//   promptPressKey: Whether to show "Press any key" prompt (optional)
void RenderGameState(Game game, Player? playerMoved = null, (int X, int Y)? selection = null, (int X, int Y)? from = null, bool promptPressKey = false)
{

	// Define constants for chess piece characters
	const char BlackPiece = '○';
	const char BlackKing  = '☺';
	const char WhitePiece = '◙';
	const char WhiteKing  = '☻';
	const char Vacant     = '·';

	// Hide cursor and set position to top-left
	Console.CursorVisible = false;
	Console.SetCursorPosition(0, 0);

	// Create StringBuilder for efficient string operations
	StringBuilder sb = new();

	// Build the game board display
	sb.AppendLine();
	sb.AppendLine("  Checkers");
	sb.AppendLine();
	// Draw board border and pieces，and draw each row of the board with piece symbols
	sb.AppendLine($"    ╔═══════════════════╗");
	sb.AppendLine($"  8 ║  {B(0, 7)} {B(1, 7)} {B(2, 7)} {B(3, 7)} {B(4, 7)} {B(5, 7)} {B(6, 7)} {B(7, 7)}  ║ {BlackPiece} = Black");
	sb.AppendLine($"  7 ║  {B(0, 6)} {B(1, 6)} {B(2, 6)} {B(3, 6)} {B(4, 6)} {B(5, 6)} {B(6, 6)} {B(7, 6)}  ║ {BlackKing} = Black King");
	sb.AppendLine($"  6 ║  {B(0, 5)} {B(1, 5)} {B(2, 5)} {B(3, 5)} {B(4, 5)} {B(5, 5)} {B(6, 5)} {B(7, 5)}  ║ {WhitePiece} = White");
	sb.AppendLine($"  5 ║  {B(0, 4)} {B(1, 4)} {B(2, 4)} {B(3, 4)} {B(4, 4)} {B(5, 4)} {B(6, 4)} {B(7, 4)}  ║ {WhiteKing} = White King");
	sb.AppendLine($"  4 ║  {B(0, 3)} {B(1, 3)} {B(2, 3)} {B(3, 3)} {B(4, 3)} {B(5, 3)} {B(6, 3)} {B(7, 3)}  ║");
	sb.AppendLine($"  3 ║  {B(0, 2)} {B(1, 2)} {B(2, 2)} {B(3, 2)} {B(4, 2)} {B(5, 2)} {B(6, 2)} {B(7, 2)}  ║ Taken:");
	sb.AppendLine($"  2 ║  {B(0, 1)} {B(1, 1)} {B(2, 1)} {B(3, 1)} {B(4, 1)} {B(5, 1)} {B(6, 1)} {B(7, 1)}  ║ {game.TakenCount(White),2} x {WhitePiece}");
	sb.AppendLine($"  1 ║  {B(0, 0)} {B(1, 0)} {B(2, 0)} {B(3, 0)} {B(4, 0)} {B(5, 0)} {B(6, 0)} {B(7, 0)}  ║ {game.TakenCount(Black),2} x {BlackPiece}");
	sb.AppendLine($"    ╚═══════════════════╝");
	sb.AppendLine($"       A B C D E F G H");
	sb.AppendLine();

	// Replace markers with highlighted pieces for selection and moves
	if (selection is not null)
	{
		sb.Replace(" $ ", $"[{ToChar(game.Board[selection.Value.X, selection.Value.Y])}]");
	}
	if (from is not null)
	{
		char fromChar = ToChar(game.Board[from.Value.X, from.Value.Y]);
		sb.Replace(" @ ", $"<{fromChar}>");
		sb.Replace("@ ",  $"{fromChar}>");
		sb.Replace(" @",  $"<{fromChar}");
	}

	// Get game status information
	PieceColor? wc = game.Winner;
	PieceColor? mc = playerMoved?.Color;
	PieceColor? tc = game.Turn;
	// Note: these strings need to match in length
	// so they overwrite each other.

	// Build the game status message display
	string w = $"  *** {wc} wins ***";
	string m = $"  {mc} moved       ";
	string t = $"  {tc}'s turn      ";
	sb.AppendLine(
		game.Winner is not null ? w :
		playerMoved is not null ? m :
		t);

	// Show or hide "Press any key" prompt
	string p = "  Press any key to continue...";
	string s = "                              ";
	sb.AppendLine(promptPressKey ? p : s);
	Console.Write(sb); //out put the complete game board

	// Local function to get the display character for a board position
	char B(int x, int y) =>
		(x, y) == selection ? '$' :
		(x, y) == from ? '@' :
		ToChar(game.Board[x, y]);

	// Helper function to convert piece to display character
	static char ToChar(Piece? piece) =>
		piece is null ? Vacant :
		(piece.Color, piece.Promoted) switch
		{
			(Black, false) => BlackPiece,
			(Black, true)  => BlackKing,
			(White, false) => WhitePiece,
			(White, true)  => WhiteKing,
			_ => throw new NotImplementedException(),
		};
}

// Method to handle human player move selection using arrow keys
// Parameters:
//   game: Current game instance
//   selectionStart: Initial selected position (optional)
//   from: Starting position of the move (optional)
// Returns: Selected board position or null if cancelled
(int X, int Y)? HumanMoveSelection(Game game, (int X, int y)? selectionStart = null, (int X, int Y)? from = null)
{
	// Initialize selection with start position or default
	(int X, int Y) selection = selectionStart ?? (3, 3);

	// Continue until valid selection is made
	while (true)
	{
		// Display current game state with selection
		RenderGameState(game, selection: selection, from: from);

		// Handle keyboard input
		switch (Console.ReadKey(true).Key)
		{
			case ConsoleKey.DownArrow:  selection.Y = Math.Max(0, selection.Y - 1); break;
			case ConsoleKey.UpArrow:    selection.Y = Math.Min(7, selection.Y + 1); break;
			case ConsoleKey.LeftArrow:  selection.X = Math.Max(0, selection.X - 1); break;
			case ConsoleKey.RightArrow: selection.X = Math.Min(7, selection.X + 1); break;
			case ConsoleKey.Enter:      return selection; //confirm the selection
			case ConsoleKey.Escape:     return null; // cancel the selection
		}
	}
}


/*
I chose two modifications: 1. Player can undo a move (Maybe there are only 3 undos per game) 2. Make it so that the player has a move count (Stamina system).
Because I think these two modifications are related to each other. After all, both undoing and counting moves need to record the player's operation history.
So I first added variables to store the required data. I added the following three variables:
public Dictionary<PieceColor, int> Steps = new() { { PieceColor.Black, 0 }, { PieceColor.White, 0 } }; used to record the number of steps of each player.
public Dictionary<PieceColor, int> UndoCounts = new() { { PieceColor.Black, 0 }, { PieceColor.White, 0 } }; used to record the number of undoes.
public Stack<Move> MoveHistory = new Stack<Move>(); used to record every step taken so as to undo.
The above modifications are all made in the Game class.
Next, because each move needs to store the steps in MoveHistory and increase the number of steps, I modified the PerformMove method to add the record of the number of steps and the number of regrets.
Next, I implemented the regret function in the UndoMove method. When the player presses the U key, he can regret the move, but he can only regret 3 times per game. The specific operation method is: 1. Undo the last move in MoveHistory; 2. Reduce the number of steps; 3. Increase the number of regrets; 4. Retract the captured pieces (if any).
In the fourth step, I modified the victory rules in CheckForWinner(). When the number of steps of one side reaches 20, the number of captured pieces is compared, and the side with more captured pieces wins. If the number of captured pieces is the same, the white piece wins. Because I think the white piece is the second move and has a certain disadvantage, it can also achieve the same number of captured pieces in this case, which means that the white piece is more powerful!
In the fifth step, I modified the RunGameLoop() method and added the regret operation. When the player presses the U key, the UndoMove() method is called to implement the undo function. The PerformMove() method adds a check on the number of undo attempts. When the number of undo attempts reaches 3, the player cannot undo any more moves.
*/