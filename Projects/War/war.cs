using System;
using System.Collections.Generic;
using System.Globalization;

List<Card> deck = new List<Card>();// Card pile
List<Card> discardPile = new(); // Discard Pile
Card playerHand = new(); // player hand
Card dealerHand = new(); // computer hand
int playerScore = 0; // player score
int dealerScore = 0; // computer score

try
{
    // creat 52 cards 
    foreach (Suit suit in Enum.GetValues<Suit>())
    {
        foreach (Value value in Enum.GetValues<Value>())
        {
            deck.Add(new()
            {
                Suit = suit,
                Value = value,
            });
        }
    }

    Shuffle(deck); // shuffle

    while (true) // the game main loop
    {
        if (deck.Count == 0) // if the card pile is empty
        {
            Console.Clear();
            Console.WriteLine("the card pile is empty！\n1. shuffle and continue\n2. end the game");
            Console.Write("please choose: ");
            string choice = Console.ReadLine();

            if (choice == "1") // shuffle and continue
            {
                deck = new List<Card>(discardPile);
                discardPile.Clear();
                Shuffle(deck);
            }
            else
            {
                break;
            }
        }

        start:
        Console.Clear();
        Console.WriteLine("press Enter to get a hand（Esc to escept）");
        Console.WriteLine($"deck left: {deck.Count}");
        Console.WriteLine($"current score - player: {playerScore}, computer: {dealerScore}");

        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.Enter:
                playerHand = deck[^1]; // player draw a card
                deck.RemoveAt(deck.Count - 1);
                discardPile.Add(playerHand); // add the card to discard pile

                dealerHand = deck[^1]; // computer draw a card
                deck.RemoveAt(deck.Count - 1);
                discardPile.Add(dealerHand); // add the card to discard pile
                break;

            case ConsoleKey.Escape:
                return;

            default:
                Console.WriteLine("press Enter to continue");
                goto start;
        }

        Console.Clear();
        Console.WriteLine("you draw:");
        DisplayCard(playerHand);
        Console.WriteLine("\n the computer draw:");
        DisplayCard(dealerHand);

        // compare the value of the hand
        if (playerHand.Value > dealerHand.Value)
        {
            Console.WriteLine("\n you win！");
            playerScore++;
        }
        else if (playerHand.Value < dealerHand.Value)
        {
            Console.WriteLine("\n you lose！");
            dealerScore++;
        }
        else
        {
            Console.WriteLine("\n draw！");
        }

        Console.WriteLine($"\n current score - player: {playerScore}, computer: {dealerScore}");
        Console.WriteLine("\n press any key to continue...");
        Console.ReadKey();
    }
}
finally
{
    Console.WriteLine("\n the game end！");
}

// show card
void DisplayCard(Card card)
{
    foreach (var line in card.Render())
    {
        Console.WriteLine(line);
    }
}

// shuffle
void Shuffle(List<Card> cards)
{
    Random rnd = new Random();
    for (int i = 0; i < cards.Count; i++)
    {
        int swap = rnd.Next(cards.Count);
        (cards[i], cards[swap]) = (cards[swap], cards[i]);
    }
}

// class card
class Card
{
    public Suit Suit;
    public Value Value;

    public string[] Render()
    {
        char suit = Suit switch
        {
            Suit.Hearts => '♥',
            Suit.Clubs => '♣',
            Suit.Spades => '♠',
            Suit.Diamonds => '♦',
            _ => ' '
        };

        string value = Value switch
        {
            Value.Ace => "A ",
            Value.Ten => "10",
            Value.Jack => "J ",
            Value.Queen => "Q ",
            Value.King => "K ",
            _ => ((int)Value).ToString(CultureInfo.InvariantCulture) + " "
        };

        return new string[]
        {
           		$"┌───────┐",
			$"│{value}│",
			$"│       │",
			$"│       │",
			$"│       │",
			$"│ {suit}│",
			$"└───────┘",
        };
    }
}

// Enumeration - Suit
enum Suit
{
    Hearts,
    Clubs,
    Spades,
    Diamonds,
}

// Enumeration - value
enum Value
{
    Ace = 14,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
}

