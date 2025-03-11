// Introduces the basic system namespace to provide basic console operations and data types
using System;

// Introduce collection-related namespaces to provide support for collection types such as List
using System.Collections.Generic;

// Introduced the LINQ namespace to provide query and data manipulation functions
using System.Linq;

// Define the main game class
class PhantomDuel
{

    // Create a random number generator for random operations such as shuffling cards
	static Random rng = new Random();

	// Define a deck list to store all game cards
	static List<Card> deck = new List<Card>();

	// Define the player's hand list to store the cards currently held by the player
	static List<Card> playerHand = new List<Card>();

	// Define the computer's hand list to store the cards currently held by the computer
	static List<Card> computerHand = new List<Card>();

	// Defines the victory points of players and computers, used to determine the outcome of the game
    static int playerVP = 0, computerVP = 0;


	// Main function, program entry
    static void Main()
    {
        Console.WriteLine("Welcome to Phantom Duel！");

		// Initialize the game
        InitializeGame();
        
		// Enter the game loop until one side reaches 10 victory points
        while (playerVP < 10 && computerVP < 10)
        {
            PlayRound();
        }
        
		// Determine the game winner and display
        Console.WriteLine(playerVP >= 10 ? "Player wins！" : "Computer wins！");
    }

    // Initialize the game, create the deck and distribute the cards
    static void InitializeGame()
    {
        deck = CreateDeck(); // Create a complete deck
        Shuffle(deck); // Shuffle the deck
        playerHand = deck.Take(6).ToList(); // Player's starting hand
        computerHand = deck.Skip(6).Take(6).ToList(); // Computer starting hand
    }

    // Create a standard 54-card deck, including A-K in 4 suits and 2 Jokers
    static List<Card> CreateDeck()
    {
        List<Card> newDeck = new List<Card>();
        string[] suits = { "Spades", "Hearts", "Clubs", "Diamonds" };  // Color
        string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" }; // Point
        
		// Traverse the suits and points to generate a complete deck
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                newDeck.Add(new Card(suit, rank));
            }
        }
        
        newDeck.Add(new Card("Joker", "Red")); // Add red Joker
        newDeck.Add(new Card("Joker", "Black")); // Add black Joker
        
        return newDeck; // Returns the generated deck
    }

    // Shuffle the deck
    static void Shuffle(List<Card> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    // Start a round of the game
    static void PlayRound()
    {
        Console.WriteLine("Please select 3 cards for battle (enter index, e.g. 0 2 4)(Need to be separated by spaces):");
        DisplayHand(playerHand); // Displays the player's current hand
        string? input = Console.ReadLine();

// If the input string is empty or contains only spaces, the system will prompt that the input is invalid and return
if (string.IsNullOrWhiteSpace(input))
{
    Console.WriteLine("Invalid input, please try again.");
    return;
}

// Split the input string by spaces, filter out the parts that can be converted to integers and convert them into an integer array
int[] selectedIndexes = input.Split(' ').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToArray();
        List<Card> playerBattle = selectedIndexes.Select(i => playerHand[i]).ToList();
        List<Card> computerBattle = computerHand.Take(3).ToList();
        
        Console.WriteLine("The battle begins!");
        for (int i = 0; i < 3; i++)
        {
            ResolveBattle(playerBattle[i], computerBattle[i]);
        }
        
        playerHand = deck.Take(6).ToList(); // Redraw player's hand
        computerHand = deck.Skip(6).Take(6).ToList(); // Redraw the computer's hand
    }

    // Analyze combat logic, compare combat effectiveness and apply special effects
    static void ResolveBattle(Card playerCard, Card computerCard)
    {
        int playerValue = playerCard.GetValue(); // Get the player's card combat power
        int computerValue = computerCard.GetValue(); // Get computer card combat power
        
        Console.WriteLine($"Player plays:{playerCard} VS Computer plays cards:{computerCard}");
        
        // Handling special card effects
        if (playerCard.IsSpecial())
            playerCard.ApplyEffect(ref playerVP, ref computerVP, true);
        if (computerCard.IsSpecial())
            computerCard.ApplyEffect(ref computerVP, ref playerVP, false);
        
        // Comparison of combat effectiveness
        if (playerValue > computerValue)
        {
            playerVP++;
            Console.WriteLine("The player wins the round!");
        }
        else if (playerValue < computerValue)
        {
            computerVP++;
            Console.WriteLine("The computer wins this round!");
        }
        else
        {
            Console.WriteLine("Draw, no one scores.");
        }
    }

    // Show current hand
    static void DisplayHand(List<Card> hand)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            Console.WriteLine($"[{i}] {hand[i]}");
        }
    }
}

// Cards class, used to represent a card in the game
class Card
{
    public string Suit { get; }
    public string Rank { get; }
    
    public Card(string suit, string rank)
    {
        Suit = suit;
        Rank = rank;
    }
    
    // Get the card's combat power value
    public int GetValue()
    {
        if (int.TryParse(Rank, out int num)) return num;
        return Rank switch
        {
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            "A" => 14,
            "Joker" => 0, // Joker has no combat power.Special handling required
            _ => 0
        };
    }
    
    // Determine whether it is a special card (Ace or Joker)
    public bool IsSpecial()
    {
        return Rank == "A" || Suit == "Joker";
    }

    // Dealing with special card effects
    public void ApplyEffect(ref int currentPlayerVP, ref int opponentVP, bool isPlayer)
    {
        if (Rank == "A")
        {
            switch (Suit)
            {
                case "Spades":
                    Console.WriteLine("SpadesA: If you win, fight again!");
                    break;
                case "Hearts":
                    Console.WriteLine("HeartsA: If you win, you get an extra +1 VP!");
                    currentPlayerVP++;
                    break;
                case "Clubs":
                    Console.WriteLine("ClubsA: No VP deduction if you fail!");
                    opponentVP--;
                    break;
                case "Diamonds":
                    Console.WriteLine("DiamondsA: If you win, draw an extra card!");
                    break;
            }
        }
        else if (Suit == "Joker")
        {
            if (Rank == "Red")
            {
                Console.WriteLine("Red Joker: Exchange a card on the field!");
            }
            else if (Rank == "Black")
            {
                Console.WriteLine("Black Joker: Clear the battlefield and both sides discard their cards!");
            }
        }
    }
    
    public override string ToString()
    {
        return Suit == "Joker" ? $"{Rank} Joker" : $"{Suit}{Rank}";
    }
}
