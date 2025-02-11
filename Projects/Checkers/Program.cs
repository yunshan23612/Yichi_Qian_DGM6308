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
		// Get the current player based on whose turn it is
		Player currentPlayer = game.Players.First(player => player.Color == game.Turn);
		
		// handle human player's turn
		if (currentPlayer.IsHuman)
		{
			while (game.Turn == currentPlayer.Color)
			{
				// Variables to track piece selection and movement
				(int X, int Y)? selectionStart = null;
				// If there's an aggressor piece (must move), use its position as starting point
				(int X, int Y)? from = game.Board.Aggressor is not null ? (game.Board.Aggressor.X, game.Board.Aggressor.Y) : null;
				// Get all possible moves for current player
				List<Move> moves = game.Board.GetPossibleMoves(game.Turn);
				
				// If only one piece can move, automatically select it
				if (moves.Select(move => move.PieceToMove).Distinct().Count() is 1)
				{
					Move must = moves.First();
					from = (must.PieceToMove.X, must.PieceToMove.Y);
					selectionStart = must.To;
				}

				// Keep asking for piece selection until valid piece is chosen
				while (from is null)
				{
					from = HumanMoveSelection(game);
					selectionStart = from;
				}

				// Get the destination square for the selected piec
				(int X, int Y)? to = HumanMoveSelection(game, selectionStart: selectionStart, from: from);
				Piece? piece = null;
				piece = game.Board[from.Value.X, from.Value.Y];

				// Validate piece selection
				if (piece is null || piece.Color != game.Turn)
				{
					from = null;
					to = null;
				}

				// Handle move execution
				if (from is not null && to is not null)
				{
					Move? move = game.Board.ValidateMove(game.Turn, from.Value, to.Value);
					if (move is not null &&
						(game.Board.Aggressor is null || move.PieceToMove == game.Board.Aggressor))
					{
						game.PerformMove(move);
					}
				}
			}
		}

		// Handle computer player's turn
		else
		{

			// Get all possible moves and capturing moves
			List<Move> moves = game.Board.GetPossibleMoves(game.Turn);
			List<Move> captures = moves.Where(move => move.PieceToCapture is not null).ToList();
			
			// Prioritize capturing moves if available
			if (captures.Count > 0)
			{
				game.PerformMove(captures[Random.Shared.Next(captures.Count)]);
			}

			// If all pieces are kings, try to move towards closest rival
			else if(!game.Board.Pieces.Any(piece => piece.Color == game.Turn && !piece.Promoted))
			{
				var (a, b) = game.Board.GetClosestRivalPieces(game.Turn);
				Move? priorityMove = moves.FirstOrDefault(move => move.PieceToMove == a && Board.IsTowards(move, b));
				game.PerformMove(priorityMove ?? moves[Random.Shared.Next(moves.Count)]);
			}

			// Make a random move if no special conditions
			else
			{
				game.PerformMove(moves[Random.Shared.Next(moves.Count)]);
			}
		}

		// Display the game state after move and wait for key press
		RenderGameState(game, playerMoved: currentPlayer, promptPressKey: true);
		Console.ReadKey(true);
	}
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
