using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Wordle
{
    const int wordLen = 5;
    const int maxGuesses = 6;

    static void Score( string solution, string guess, char [] score, bool [] slotsUsed )
    {
        for ( int i = 0; i < wordLen; i++ )
        {
            score[ i ] = ' ';
            slotsUsed[ i ] = false;
        }

        for ( int i = 0; i < wordLen; i++ )
        {
            if ( guess[ i ] == solution[ i ] )
            {
                score[ i ] = 'g';
                slotsUsed[ i ] = true;
            }
        }

        for ( int i = 0; i < wordLen; i++ )
        {
            if ( guess[ i ] != solution[ i ] )
            {
                for ( int j = 0; j < wordLen; j++ )
                {
                    if ( i != j && ! slotsUsed[ j ] && guess[ i ] == solution[ j ] )
                    {
                        slotsUsed[ j ] = true;
                        score[ i ] = 'y';
                    }
                }
            }
        }
    } //Score

    static bool SameScore( char [] chars, string str )
    {
        for ( int i = 0; i < wordLen; i++ )
            if ( chars[ i ] != str[ i ] )
                return false;
        return true;
    } //SameScore

    static void Main( string[] args )
    {
        Random rand = new Random( Environment.TickCount );
        List<string> dictionary = new List<string>();
        foreach ( string line in System.IO.File.ReadLines( @"words.txt" ) )
            if ( wordLen == line.Length  &&
                 -1 == line.IndexOf( '.' ) &&
                 -1 == line.IndexOf( '-' ) &&
                 -1 == line.IndexOf( '\'' ) )
               dictionary.Add( line.ToLower() );

        // randomize the dictionary words

        for ( int r = 0; r < dictionary.Count(); r++ )
        {
            int x = rand.Next( dictionary.Count() );
            int y = rand.Next( dictionary.Count() );
            string t = dictionary[ x ];
            dictionary[ x ] = dictionary[ y ];
            dictionary[ y ] = t;
        }

        // if found, use "glyph" as the opening guess since empirically that works best

        int startingGuess = rand.Next( dictionary.Count() );
        for ( int r = 0; r < dictionary.Count(); r++ )
        {
            if ( 0 == String.Compare( dictionary[ r ], "glyph" ) )
            {
                startingGuess = r;
                break;
            }
        }

        int successes = 0;
        int failures = 0;
        string allgreen = new string( 'g', wordLen );

        //for ( int isolution = 0; isolution < dictionary.Count(); isolution++ )
        Parallel.For ( 0, dictionary.Count(), isolution =>
        {
            string [] scores = new string[ maxGuesses ];
            string [] guesses = new string[ maxGuesses ];

            string solution = dictionary[ isolution ];
            //Console.WriteLine( "solution: {0}", solution );

            bool success = false;
            int nextGuess = startingGuess;
            char [] score = new char[ wordLen ];
            bool [] slotsUsed = new bool[ wordLen ];

            for ( int i = 0; i < maxGuesses; i++ )
            {
                string attempt = null;

                do
                {
                    attempt = dictionary[ nextGuess++ ];
                    if ( nextGuess >= dictionary.Count() )
                        nextGuess = 0;
                    bool newGuessMatchesPriorGuesses = true;
                    //Console.WriteLine( "  guess {0}", attempt );
    
                    for ( int g = 0; g < i; g++ )
                    {
                        Score( attempt, guesses[ g ], score, slotsUsed );
                        //Console.WriteLine( "    match of guess {0}: {1}", g, new string( score ) );
                        if ( !SameScore( score, scores[ g ] ) )
                        {
                            newGuessMatchesPriorGuesses = false;
                            break;
                        }
                    }
    
                    if ( newGuessMatchesPriorGuesses )
                        break;
                } while( true );
    
                Score( solution, attempt, score, slotsUsed );
                guesses[ i ] = attempt;
                scores[ i ] = new string( score );
                //Console.WriteLine( "  attempt {0} {1} has score {2}", i, attempt, scores[ i ] );
                if ( SameScore( score, allgreen ) )
                {
                    success = true;
                    break;
                }
            }

            if ( success )
                Interlocked.Increment( ref successes );
            else
                Interlocked.Increment( ref failures );
        } );

        Console.WriteLine( "total games {0}, successes {1}, failures {2}, rate {3}", dictionary.Count(),
                           successes, failures, (float) successes / (float) dictionary.Count() );
    } //Main
} //Wordle
